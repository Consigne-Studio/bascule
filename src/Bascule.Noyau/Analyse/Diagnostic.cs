using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Bascule.Chargement;
using Bascule.Rapport;
using Bascule.Selenium;

namespace Bascule.Analyse;

/// <summary>Diagnostic de l'édition gratuite : classe chaque usage de Selenium selon ce que la migration en fera
/// (automatique, à relire, à reprendre), sans contenir le réécrivain. Les tables suivent les règles de l'édition Pro ;
/// leur cohérence est vérifiée sur les suites d'exemple (tests DiagnosticTests).</summary>
public static class Diagnostic
{
    // Membres convertis automatiquement, par nom (le récepteur est vérifié quand le nom est ambigu).
    private static readonly HashSet<string> Automatiques = new(StringComparer.Ordinal)
    {
        // éléments
        "Click", "Clear", "SendKeys", "Submit", "Text", "Displayed", "Enabled", "Selected", "TagName",
        "GetDomAttribute", "GetDomProperty", "GetProperty", "GetCssValue", "GetScreenshot", "GetShadowRoot",
        "FindElement", "FindElements",
        // pilote et navigation
        "Title", "Url", "PageSource", "Quit", "Close", "Dispose", "GoToUrl", "Back", "Forward", "Refresh",
        "Navigate", "Manage", "SwitchTo", "Timeouts", "Window", "Maximize", "Minimize", "FullScreen",
        "Frame", "DefaultContent", "ParentFrame", "Alert", "Accept", "Dismiss", "ActiveElement",
        "ExecuteScript", "ExecuteJavaScript", "Cookies", "AllCookies", "GetCookieNamed", "DeleteAllCookies", "DeleteCookieNamed",
        "AsByteArray", "AsBase64EncodedString", "SaveAsFile",
        // listes
        "SelectByText", "SelectByValue", "SelectByIndex", "DeselectAll", "SelectedOption", "AllSelectedOptions", "Options", "IsMultiple", "WrappedElement",
        // attentes
        "Until", "IgnoreExceptionTypes", "ImplicitWait", "PageLoad", "Timeout", "PollingInterval", "Message",
        // cookies Selenium lus après conversion
        "Name", "Value", "Domain", "Path", "Secure",
    };

    private static readonly HashSet<string> SelecteursBy = new(StringComparer.Ordinal)
    {
        "Id", "CssSelector", "Css", "XPath", "Name", "ClassName", "TagName", "LinkText", "PartialLinkText",
    };

    private static readonly HashSet<string> ConditionsConverties = new(StringComparer.Ordinal)
    {
        "ElementIsVisible", "ElementToBeClickable", "VisibilityOfAllElementsLocatedBy", "PresenceOfAllElementsLocatedBy", "ElementExists",
        "InvisibilityOfElementLocated", "TextToBePresentInElementLocated", "TextToBePresentInElement", "TextToBePresentInElementValue",
        "TitleIs", "TitleContains", "UrlToBe", "UrlContains", "UrlMatches", "ElementToBeSelected", "ElementSelectionStateToBe",
        "FrameToBeAvailableAndSwitchToIt", "AlertIsPresent",
    };

    private static readonly HashSet<string> EtapesActions = new(StringComparer.Ordinal)
    {
        "MoveToElement", "Click", "DoubleClick", "ContextClick", "ClickAndHold", "Release", "DragAndDrop", "KeyDown", "KeyUp",
        "SendKeys", "ScrollToElement", "ScrollByAmount", "Pause", "Perform", "Build",
    };

    // Membres sans conversion automatique, avec l'indication donnée dans le rapport.
    private static readonly Dictionary<string, (string Categorie, string Message)> Manuels = new(StringComparer.Ordinal)
    {
        ["WindowHandles"] = (Categories.Fenetres, "Identifiants de fenêtre : avec Playwright, chaque onglet est un IPage (Context.Pages)."),
        ["CurrentWindowHandle"] = (Categories.Fenetres, "Identifiants de fenêtre : avec Playwright, chaque onglet est un IPage (Context.Pages)."),
        ["NewWindow"] = (Categories.Fenetres, "Nouvel onglet ou fenêtre : récupérez la nouvelle page avec Context.RunAndWaitForPageAsync."),
        ["ExecuteAsyncScript"] = (Categories.JavaScript, "ExecuteAsyncScript : à réécrire avec EvaluateAsync et une promesse."),
        ["AddCookie"] = (Categories.Cookies, "AddCookie : construisez un cookie Playwright (Name, Value, Url ou Domain/Path)."),
        ["Location"] = (Categories.Lecture, "Position d'un élément : BoundingBoxAsync()."),
        ["Size"] = (Categories.Lecture, "Taille d'un élément : BoundingBoxAsync()."),
        ["Coordinates"] = (Categories.Lecture, "Coordonnées d'un élément : BoundingBoxAsync()."),
        ["LocationOnScreenOnceScrolledIntoView"] = (Categories.Lecture, "Position après défilement : ScrollIntoViewIfNeededAsync puis BoundingBoxAsync."),
        ["SessionId"] = (Categories.Cycle, "SessionId n'a pas d'équivalent Playwright."),
        ["Capabilities"] = (Categories.Cycle, "Capabilities n'a pas d'équivalent Playwright."),
    };

    public static RapportBascule Analyser(Espace espace, string version, string edition)
    {
        var rapport = new RapportBascule { Mode = "diagnostic", Edition = edition, Version = version, Date = DateTimeOffset.Now, Source = espace.Racine };
        var arbres = new Dictionary<string, SyntaxTree>(StringComparer.Ordinal);
        foreach (var f in espace.Projets.SelectMany(p => p.Fichiers).Distinct())
            arbres[f] = Compilateur.Analyser(File.ReadAllText(f), f);
        var parChemin = espace.Projets.ToDictionary(p => p.CheminCsproj, StringComparer.Ordinal);
        var vus = new HashSet<string>(StringComparer.Ordinal);

        foreach (var projet in espace.Projets)
        {
            rapport.Projets.Add(Inventaire.ResumeProjet(projet, espace.Racine, arbres));
            var propres = projet.Fichiers.Where(vus.Add).ToList();
            if (!projet.ReferenceSelenium && !propres.Any(f => arbres[f].ToString().Contains("OpenQA.Selenium", StringComparison.Ordinal))) continue;
            var rel = Relatif(espace.Racine, projet.CheminCsproj);
            if (!projet.StyleSdk)
                rapport.Constats.Add(new Constat(rel, 1, Categories.Projet, Niveau.Manuel,
                    "Projet au format ancien (non SDK, packages.config) : à convertir au format SDK avant de remplacer les paquets. Le code C# est migré."));
            rapport.Constats.Add(new Constat(rel, 1, Categories.Projet, Niveau.Automatique, "Paquets Selenium remplacés par Playwright (et versions minimales relevées)."));

            var compilation = Compilateur.Compiler(projet, arbres, c => parChemin.GetValueOrDefault(c));
            foreach (var f in propres)
            {
                var m = compilation.GetSemanticModel(arbres[f]);
                new Classeur(m, Relatif(espace.Racine, f), rapport.Constats).Classer(arbres[f].GetRoot());
            }
        }
        return rapport;
    }

    private static string Relatif(string racine, string chemin) => Path.GetRelativePath(racine, chemin).Replace('\\', '/');

    /// <summary>Parcourt un fichier et range chaque usage de Selenium (l'expression la plus externe d'une chaîne d'appels).</summary>
    private sealed class Classeur(SemanticModel m, string fichier, List<Constat> constats)
    {
        private readonly HashSet<int> _lignesManuelles = [];

        private void Ajouter(SyntaxNode n, string categorie, Niveau niveau, string message)
        {
            var ligne = n.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            if (niveau == Niveau.Manuel && !_lignesManuelles.Add(ligne)) return;   // un point manuel par ligne, comme la migration
            constats.Add(new Constat(fichier, ligne, categorie, niveau, message, Extrait(n)));
        }

        private static string Extrait(SyntaxNode n)
        {
            var t = string.Join(" ", n.ToString().Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
            return t.Length <= 160 ? t : t[..159] + "…";
        }

        private ISymbol? Symbole(SyntaxNode n) => m.GetSymbolInfo(n).Symbol ?? m.GetSymbolInfo(n).CandidateSymbols.FirstOrDefault();

        private NatureSelenium Nature(ExpressionSyntax e) => ApiSelenium.Nature(m.GetTypeInfo(e).Type);

        public void Classer(SyntaxNode racine)
        {
            var cu = (CompilationUnitSyntax)racine;
            if (cu.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(u => u.NamespaceOrType.ToString().StartsWith("OpenQA.Selenium.Appium", StringComparison.Ordinal)))
                Ajouter(cu.Usings.FirstOrDefault() ?? (SyntaxNode)cu, Categories.Projet, Niveau.Manuel,
                    "Code mobile (Appium) : Playwright ne pilote pas les applications mobiles natives ; les parties web sont migrées, les pilotes Appium restent.");

            foreach (var n in racine.DescendantNodes())
            {
                switch (n)
                {
                    case InvocationExpressionSyntax or MemberAccessExpressionSyntax when EstUsageExterne((ExpressionSyntax)n):
                        ClasserUsage((ExpressionSyntax)n);
                        break;
                    case BaseObjectCreationExpressionSyntax c:
                        ClasserCreation(c);
                        break;
                    case AttributeSyntax a:
                        ClasserAttribut(a);
                        break;
                    case CatchDeclarationSyntax cd when ApiSelenium.EstSelenium(m.GetTypeInfo(cd.Type).Type):
                        var nomEx = m.GetTypeInfo(cd.Type).Type!.Name;
                        if (nomEx is "NoSuchElementException" or "NotFoundException" or "WebDriverTimeoutException")
                            Ajouter(cd, Categories.Exceptions, Niveau.Automatique, $"{nomEx} remplacée par son équivalent.");
                        else
                            Ajouter(cd, Categories.Exceptions, Niveau.ARelire, $"{nomEx} n'existe pas dans Playwright : remplacée par PlaywrightException.");
                        break;
                    case InvocationExpressionSyntax sleep when sleep.Expression.ToString() is "Thread.Sleep" or "System.Threading.Thread.Sleep":
                        Ajouter(sleep, Categories.Pauses, Niveau.ARelire, "Pause fixe conservée (await Task.Delay) : probablement à supprimer, Playwright attend automatiquement.");
                        break;
                    case MethodDeclarationSyntax md:
                        ClasserCadres(md);
                        break;
                    case ArgumentSyntax arg when ApiSelenium.Nature(m.GetTypeInfo(arg.Expression).Type) == NatureSelenium.OptionsNavigateur
                                                 && !EstCreationDePilote(arg):
                        // Une fois par variable d'options, comme la migration (qui conserve sa déclaration).
                        if (Symbole(arg.Expression) is not { } so || _optionsSignalees.Add(so))
                            Ajouter(arg, Categories.Cycle, Niveau.Manuel, "Options de navigateur passées à une autre méthode : à remplacer par le nom du navigateur Playwright ou par BrowserTypeLaunchOptions.");
                        break;
                    case ParameterSyntax { Type: { } tp } when TypeSansEquivalent(tp) is { } nt:
                        Ajouter(tp, Categories.Cycle, Niveau.Manuel, $"Paramètre de type {nt} (Selenium) : sans équivalent direct, à remplacer.");
                        break;
                    case PropertyDeclarationSyntax pd when TypeSansEquivalent(pd.Type) is { } nt:
                        Ajouter(pd.Type, Categories.Cycle, Niveau.Manuel, $"Propriété de type {nt} (Selenium) : sans équivalent direct, à remplacer.");
                        break;
                }
            }
        }

        /// <summary>Expression Selenium qui n'est ni le récepteur ni un argument d'une autre expression Selenium.</summary>
        private bool EstUsageExterne(ExpressionSyntax e)
        {
            if (!EstSelenium(e)) return false;
            for (SyntaxNode? p = e.Parent; p is not null and not StatementSyntax and not MemberDeclarationSyntax; p = p.Parent)
                if (p is InvocationExpressionSyntax or MemberAccessExpressionSyntax && EstSelenium((ExpressionSyntax)p)) return false;
            return true;
        }

        private bool EstSelenium(ExpressionSyntax e)
        {
            var s = Symbole(e);
            if (s is IMethodSymbol or IPropertySymbol or IFieldSymbol { ContainingType.TypeKind: not TypeKind.Enum })
                return ApiSelenium.EstSelenium(s) && s.ContainingType?.Name is not "Keys" and not "How";
            // Selenium 3 sans symbole (ExpectedConditions résolues par leur nom).
            return false;
        }

        private void ClasserUsage(ExpressionSyntax e)
        {
            var s = Symbole(e)!;
            var nom = s.Name;
            var type = s.ContainingType?.Name ?? "";
            var ns = s.ContainingNamespace?.ToDisplayString() ?? "";

            // Réglages d'options de navigateur et PageFactory : supprimés par la migration (simple information).
            if (s.ContainingType is not null && ApiSelenium.Nature(s.ContainingType) == NatureSelenium.OptionsNavigateur)
            {
                Ajouter(e, Categories.Cycle, Niveau.Information, "Option de navigateur : à reporter si besoin dans le .runsettings de Playwright.");
                return;
            }
            if (type == "PageFactory")
            {
                Ajouter(e, Categories.PageObjects, Niveau.Information, "PageFactory.InitElements supprimé : les propriétés ILocator sont résolues à l'usage.");
                return;
            }

            if (ns.Contains("DevTools", StringComparison.Ordinal) || ns.Contains("BiDi", StringComparison.Ordinal) || ns.Contains("VirtualAuth", StringComparison.Ordinal)
                || type is "INetwork" or "IJavaScriptEngine" or "IDevTools" or "ICommandExecutor" or "IHasCommandExecutor")
            {
                Ajouter(e, Categories.Cycle, Niveau.Manuel, $"{type}.{nom} (DevTools, BiDi ou commande propre au pilote) : pas d'équivalent automatique ; Playwright propose Page.RouteAsync, Context.NewCDPSessionAsync…");
                return;
            }
            if (type == "RelativeBy" || Descendants(e).Any(d => Symbole(d)?.ContainingType?.Name == "RelativeBy"))
            {
                Ajouter(e, Categories.Selecteurs, Niveau.Manuel, "Localisateur relatif (RelativeBy) : à réécrire avec Locator(…).Filter.");
                return;
            }
            if (nom == "AddCookie" && e is InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: BaseObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: >= 2 and var n } }] })
            {
                Ajouter(e, Categories.Cookies, n == 2 ? Niveau.Automatique : Niveau.ARelire,
                    n == 2 ? "AddCookie : Context.AddCookiesAsync sur l'URL courante." : "AddCookie : nom et valeur reportés ; domaine, chemin et expiration à vérifier.");
                return;
            }
            if (Manuels.TryGetValue(nom, out var man) && !(nom == "Size" && type == "IWindow"))
            {
                Ajouter(e, man.Categorie, Niveau.Manuel, man.Message);
                return;
            }
            if (nom == "Size" && type == "IWindow")
            {
                Ajouter(e, Categories.Navigation, Niveau.Automatique, "Taille de fenêtre : SetViewportSizeAsync.");
                return;
            }
            if (nom == "Window" && type == "ITargetLocator")
            {
                Ajouter(e, Categories.Fenetres, Niveau.Manuel, "Changement de fenêtre : avec Playwright, chaque onglet est un IPage (Context.Pages).");
                return;
            }
            if (type == "By")
            {
                if (SelecteursBy.Contains(nom)) Ajouter(e, Categories.Selecteurs, Niveau.Automatique, $"By.{nom} devient un sélecteur Playwright.");
                else Ajouter(e, Categories.Selecteurs, Niveau.Manuel, $"By.{nom} : à écrire en sélecteur CSS ou XPath.");
                return;
            }
            if (nom.StartsWith("FindElementBy", StringComparison.Ordinal) || nom.StartsWith("FindElementsBy", StringComparison.Ordinal))
            {
                Ajouter(e, Categories.Selecteurs, Niveau.Automatique, "Recherche Selenium 3 convertie en localisateur.");
                return;
            }
            if (type == "Actions" || nom == "Perform" && Descendants(e).Any(d => Symbole(d)?.ContainingType?.Name == "Actions"))
            {
                var etapes = Descendants(e).Append(e).OfType<InvocationExpressionSyntax>().Select(i => Symbole(i)).Where(x => x?.ContainingType?.Name == "Actions").Select(x => x!.Name).ToList();
                var inconnue = etapes.FirstOrDefault(x => !EtapesActions.Contains(x));
                if (inconnue is null) Ajouter(e, Categories.Avance, Niveau.Automatique, "Chaîne Actions convertie en appels Playwright (Hover, Mouse, Keyboard, DragTo).");
                else Ajouter(e, Categories.Avance, Niveau.Manuel, $"Chaîne Actions ({inconnue}) : à convertir avec Page.Mouse et Page.Keyboard.");
                return;
            }
            if (nom == "Until")
            {
                var inv = e as InvocationExpressionSyntax;
                var arg = inv?.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                if (arg is LambdaExpressionSyntax)
                {
                    Ajouter(e, Categories.Attentes, Niveau.Automatique, "Attente par condition : BasculeOutils.AttendreJusquaAsync avec le même délai.");
                    return;
                }
                var cond = (arg as InvocationExpressionSyntax)?.Expression switch
                {
                    MemberAccessExpressionSyntax ma => ma.Name.Identifier.ValueText,
                    IdentifierNameSyntax id => id.Identifier.ValueText,
                    _ => null,
                };
                if (cond == "AlertIsPresent")
                    Ajouter(e, Categories.Dialogues, Niveau.ARelire, "Attente d'un dialogue : CapturerDialogues répond au dialogue dès son ouverture.");
                else if (cond is not null && ConditionsConverties.Contains(cond))
                    Ajouter(e, Categories.Attentes, Niveau.Automatique, $"ExpectedConditions.{cond} : WaitForAsync ou assertion Expect.");
                else
                    Ajouter(e, Categories.Attentes, Niveau.Manuel, $"Attente {(cond is null ? "non reconnue" : "ExpectedConditions." + cond)} : à remplacer par Expect(…) ou WaitForAsync.");
                return;
            }
            if (nom is "GetAttribute")
            {
                var a0 = (e as InvocationExpressionSyntax)?.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                bool litteral = a0 is not null && m.GetConstantValue(a0).HasValue;
                Ajouter(e, Categories.Lecture, litteral ? Niveau.Automatique : Niveau.ARelire,
                    litteral ? "GetAttribute : GetAttributeAsync (ou valeur DOM pour value, checked…)." : "GetAttribute avec un nom calculé : vérifiez la valeur lue.");
                return;
            }
            if (nom == "SendKeys" && e is InvocationExpressionSyntax sk && sk.ArgumentList.Arguments.Count == 1
                && sk.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax idf
                && idf.Identifier.ValueText.ToLowerInvariant() is var v && (v.Contains("path") || v.Contains("chemin") || v.Contains("fichier") || v.Contains("file") || v.Contains("upload")))
            {
                Ajouter(e, Categories.Actions, Niveau.ARelire, "SendKeys d'un chemin de fichier : SetInputFilesAsync (champ type=file).");
                return;
            }
            if (Automatiques.Contains(nom))
            {
                if (type == "SelectElement" && nom.StartsWith("Deselect", StringComparison.Ordinal) && nom != "DeselectAll")
                {
                    Ajouter(e, Categories.Listes, Niveau.Manuel, $"SelectElement.{nom} : à convertir avec SelectOptionAsync.");
                    return;
                }
                Ajouter(e, CategoriePour(nom, type), Niveau.Automatique, $"{nom} : converti en équivalent Playwright.");
                return;
            }
            Ajouter(e, Categories.Cycle, Niveau.Manuel, $"{type}.{nom} : pas de conversion automatique.");
        }

        private readonly HashSet<ISymbol> _optionsSignalees = new(SymbolEqualityComparer.Default);

        // Types Selenium que la migration remplace (pilote, élément, sélecteur, attente, capture, alerte, exceptions courantes).
        private static readonly HashSet<string> TypesRemplaces = new(StringComparer.Ordinal)
        {
            "IWebDriver", "WebDriver", "RemoteWebDriver", "ChromeDriver", "FirefoxDriver", "EdgeDriver", "SafariDriver", "InternetExplorerDriver",
            "ChromiumDriver", "IJavaScriptExecutor", "ITakesScreenshot", "Actions", "ISearchContext", "IWebElement", "WebElement", "SelectElement",
            "By", "Screenshot", "WebDriverWait", "DefaultWait", "IWait", "IAlert",
        };

        private string? TypeSansEquivalent(TypeSyntax t)
        {
            var type = m.GetTypeInfo(t).Type;
            if (type is INamedTypeSymbol { IsGenericType: true } g && g.TypeArguments.Length == 1) type = g.TypeArguments[0] is INamedTypeSymbol a && ApiSelenium.EstSelenium(a) ? a : type;
            return type is not null && ApiSelenium.EstSelenium(type) && !TypesRemplaces.Contains(type.Name) ? type.Name : null;
        }

        private bool EstCreationDePilote(ArgumentSyntax arg) =>
            arg.Parent?.Parent is BaseObjectCreationExpressionSyntax c && ApiSelenium.Nature(m.GetTypeInfo(c).Type) == NatureSelenium.Pilote
            || arg.Parent?.Parent is InvocationExpressionSyntax i && ApiSelenium.Nature(m.GetTypeInfo(i).Type) == NatureSelenium.Pilote;

        private static IEnumerable<ExpressionSyntax> Descendants(ExpressionSyntax e) => e.DescendantNodes().OfType<ExpressionSyntax>();

        private static string CategoriePour(string nom, string type) => (nom, type) switch
        {
            (_, "SelectElement") => Categories.Listes,
            (_, "IAlert") or ("Alert" or "Accept" or "Dismiss", _) => Categories.Dialogues,
            (_, "ICookieJar" or "Cookie") => Categories.Cookies,
            (_, "INavigation") or ("Url" or "Title" or "PageSource", _) => Categories.Navigation,
            (_, "Screenshot" or "ITakesScreenshot") or ("GetScreenshot", _) => Categories.Captures,
            ("ExecuteScript" or "ExecuteJavaScript", _) => Categories.JavaScript,
            ("Frame" or "DefaultContent" or "ParentFrame", _) => Categories.Cadres,
            ("Until" or "ImplicitWait" or "PageLoad" or "Timeout" or "PollingInterval" or "IgnoreExceptionTypes", _) => Categories.Attentes,
            ("Text" or "Displayed" or "Enabled" or "Selected" or "TagName" or "GetCssValue" or "GetDomAttribute" or "GetDomProperty" or "GetProperty", _) => Categories.Lecture,
            ("FindElement" or "FindElements", _) => Categories.Selecteurs,
            ("Quit" or "Close" or "Dispose", _) => Categories.Cycle,
            ("Maximize" or "Minimize" or "FullScreen" or "Window", _) => Categories.Cookies,
            _ => Categories.Actions,
        };

        private void ClasserCreation(BaseObjectCreationExpressionSyntax c)
        {
            var t = m.GetTypeInfo(c).Type;
            if (!ApiSelenium.EstSelenium(t)) return;
            switch (ApiSelenium.Nature(t))
            {
                case NatureSelenium.Pilote when t!.Name == "RemoteWebDriver":
                    Ajouter(c, Categories.Cycle, Niveau.Manuel, "RemoteWebDriver (grille Selenium) : remplacé par un navigateur local ; pour un navigateur distant, utilisez BrowserType.ConnectAsync.");
                    break;
                case NatureSelenium.Pilote:
                    Ajouter(c, Categories.Cycle, Niveau.Automatique, $"{t!.Name} : le navigateur est fourni par PageTest (ou BasculeOutils).");
                    break;
                case NatureSelenium.Liste or NatureSelenium.Actions or NatureSelenium.Attente or NatureSelenium.OptionsNavigateur:
                    break;   // classés avec leurs appels
                default:
                    if (t!.Name is not "Cookie")
                        Ajouter(c, Categories.Cycle, Niveau.Manuel, $"new {t.Name} : pas d'équivalent automatique.");
                    else if (c.ArgumentList is not { Arguments.Count: >= 2 } || c.Parent is not ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddCookie" } } })
                        Ajouter(c, Categories.Cookies, Niveau.Manuel, "Cookie Selenium : construisez un cookie Playwright.");   // sinon classé avec AddCookie
                    break;
            }
        }

        private void ClasserAttribut(AttributeSyntax a)
        {
            var nom = a.Name.ToString().Split('.').Last();
            if (nom.EndsWith("Attribute", StringComparison.Ordinal)) nom = nom[..^"Attribute".Length];
            if (nom == "FindsBy") Ajouter(a, Categories.PageObjects, Niveau.Automatique, "[FindsBy] devient une propriété ILocator.");
            else if (nom is "FindsBySequence" or "FindsByAll") Ajouter(a, Categories.PageObjects, Niveau.Manuel, $"[{nom}] : à réécrire en localisateur chaîné.");
        }

        /// <summary>Dans une méthode, après SwitchTo().Frame et avant DefaultContent : les appels aux méthodes du projet qui manipulent
        /// le navigateur chercheront dans la page (Playwright n'a pas de « cadre courant »).</summary>
        private void ClasserCadres(MethodDeclarationSyntax md)
        {
            bool dansCadre = false;
            foreach (var inv in md.DescendantNodes().OfType<InvocationExpressionSyntax>().OrderBy(i => i.SpanStart))
            {
                var s = Symbole(inv) as IMethodSymbol;
                if (s is null) continue;
                if (ApiSelenium.EstSelenium(s))
                {
                    if (s.Name == "Frame") dansCadre = true;
                    else if (s.Name is "DefaultContent") dansCadre = false;
                    continue;
                }
                if (!dansCadre || s.MethodKind != MethodKind.Ordinary || !s.Locations.Any(l => l.IsInSource)) continue;
                bool navigateur = s.Parameters.Any(p => ApiSelenium.Nature(p.Type) is NatureSelenium.Selecteur or NatureSelenium.Pilote)
                                  || s.ContainingType.GetMembers().Any(x => x is IFieldSymbol f && ApiSelenium.Nature(f.Type) == NatureSelenium.Pilote
                                                                         || x is IPropertySymbol p && ApiSelenium.Nature(p.Type) == NatureSelenium.Pilote);
                if (navigateur)
                    Ajouter(inv, Categories.Cadres, Niveau.Manuel, $"Appel de {s.Name}() dans un cadre : cette méthode cherchera dans la page ; faites la recherche sur le FrameLocator ou passez-le-lui.");
            }
        }
    }
}
