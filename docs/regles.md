# Règles de conversion

Ce que Bascule fait de chaque usage de Selenium. Le diagnostic (édition gratuite) classe chaque usage selon ces règles ; la migration (édition Pro) les applique. Trois niveaux :

* **Automatique** : réécrit sans changement de comportement attendu.
* **À relire** : réécrit, mais le comportement peut différer un peu (texte visible, ordre d'attente, envoi de fichier). Le rapport dit quoi vérifier.
* **À reprendre** : laissé tel quel (Selenium reste alors référencé) ; le rapport donne le fichier, la ligne et l'équivalent Playwright à écrire.

**Compilation vérifiée.** Après la réécriture, l'édition Pro recompile en mémoire la suite d'origine et la suite migrée, avec les métadonnées de Playwright et des cadres de test. Chaque erreur que la migration a fait apparaître devient un point **à reprendre** (catégorie « Compilation du résultat »), avec le fichier et la ligne du fichier migré ; les erreurs déjà présentes avant ne comptent pas. Avec `--compiler`, `dotnet build` complète la vérification avec les vrais paquets du projet (Appium, bibliothèques maison) et ses erreurs rejoignent aussi le rapport.

Le rapport ajoute des constats **Information** (option de navigateur, `PageFactory`…) qui ne demandent rien.

## Projet

| Avant | Après |
|---|---|
| Paquets `Selenium.*`, pilotes `*Driver`, `DotNetSeleniumExtras.*`, `WebDriverManager` | retirés ; `Microsoft.Playwright.NUnit`, `.MSTest` ou `.Xunit` 1.64.1 ajouté |
| `Microsoft.NET.Test.Sdk`, `NUnit`, `MSTest.*`, `xunit` trop anciens pour Playwright | relevés à la version minimale (sinon NU1605) |
| Options de navigateur (`AddArgument("--headless")`, taille de fenêtre) | supprimées, signalées en information : Playwright tourne sans interface par défaut, avec une fenêtre de 1280 × 720 |
| Pilote autre que Chrome (`FirefoxDriver`, `EdgeDriver`…) | `.runsettings` Playwright qui choisit le même navigateur, référencé par `RunSettingsFilePath` |
| Projet au format ancien (`packages.config`) | **à reprendre** : convertir au format SDK ; le code C# est migré |

Si des points restent à reprendre, Selenium est **conservé** à côté de Playwright pour que le projet compile pendant la transition.

## Cycle de vie du navigateur

Deux stratégies, choisies par classe :

* **Classe de base Playwright** (`PageTest` de NUnit, MSTest ou xUnit) quand la classe crée et ferme elle-même son pilote : la création du pilote, `Quit`, `Dispose` et les options disparaissent, `driver` devient la propriété `Page`. Une classe de base maison qui porte le pilote devient elle-même un `PageTest`.
* **Cycle de vie conservé** quand le pilote est créé ailleurs (fabrique, singleton, injection, ou champ initialisé depuis l'un d'eux) : un `IPage` est ouvert au même endroit (`BasculeOutils.OuvrirNavigateurAsync`), et fermé là où le pilote l'était.

`new RemoteWebDriver(...)` (grille Selenium distante, BrowserStack, Sauce Labs) est remplacé par un navigateur **local** et signalé **à reprendre** : pour un navigateur distant, utilisez `BrowserType.ConnectAsync` vers le point d'accès Playwright de votre fournisseur. Vérifiez votre intégration continue avant de fusionner.

## Sélecteurs

| Selenium | Playwright |
|---|---|
| `By.Id("panier")` | `Locator("#panier")` (ou `[id="…"]` si l'identifiant n'est pas un identifiant CSS simple) |
| `By.CssSelector(s)` | `Locator(s)` |
| `By.XPath(x)` | `Locator("xpath=" + x)` |
| `By.Name(n)` | `Locator("[name=\"n\"]")` |
| `By.ClassName(c)` | `Locator(".c")` |
| `By.TagName(t)` | `Locator(t)` |
| `By.LinkText(t)`, `By.PartialLinkText(t)` | `Locator("a:text-is(\"t\")")`, `Locator("a:has-text(\"t\")")` |
| `FindElement(by)` | `Locator(sel).First` (premier élément, comme Selenium ; pas de `.First` pour un `#id`) |
| `FindElements(by)` | `Locator(sel).AllAsync()` ; `.Count` devient `CountAsync()`, `[n]` devient `Nth(n)` |
| `[FindsBy(How = How.Id, Using = "x")]` | propriété `ILocator` calculée |
| `RelativeBy` (`above`, `near`…) | **à reprendre** : `Locator(…).Filter(…)` ou sélecteurs `:near()` |

Les sélecteurs construits par concaténation ou passés en paramètre (`By` dans une méthode utilitaire) sont convertis aussi : les paramètres `By` deviennent des chaînes de sélecteur.

## Actions et lectures

| Selenium | Playwright | Niveau |
|---|---|---|
| `Click()`, `Clear()` | `ClickAsync()`, `ClearAsync()` | automatique |
| `SendKeys(texte)` | `FillAsync(texte)` (remplace le contenu ; Selenium ajoutait à la fin) | automatique |
| `SendKeys(Keys.Enter)` | `PressAsync("Enter")` | automatique |
| `SendKeys(cheminDeFichier)` | `SetInputFilesAsync(chemin)` | à relire |
| `Submit()` | `EvaluateAsync` sur le formulaire (`requestSubmit`) | automatique |
| `Text` | `InnerTextAsync()` | automatique |
| `Displayed`, `Enabled`, `Selected` | `IsVisibleAsync()`, `IsEnabledAsync()`, `IsCheckedAsync()` | automatique |
| `GetAttribute`, `GetDomAttribute`, `GetDomProperty`, `GetCssValue` | `GetAttributeAsync`, `EvaluateAsync` | automatique ou à relire selon l'attribut |
| `Location`, `Size`, `Coordinates` | **à reprendre** : `BoundingBoxAsync()` | à reprendre |
| `new SelectElement(e).SelectByText(t)` | `SelectOptionAsync(new SelectOptionValue { Label = t })` | automatique |
| `new Actions(driver)…Perform()` | `HoverAsync`, `DblClickAsync`, `ClickAsync(Button = Right)`, `DragToAsync`, `Keyboard`, `Mouse` | automatique |

Chaque méthode qui appelle Playwright devient `async Task` ; ses appelants aussi, de proche en proche (tests, page objects, utilitaires). Une méthode passée en délégué (groupe de méthodes) reste synchrone avec toute sa famille, et ses usages sont signalés.

## Navigation, fenêtres, cadres, alertes

| Selenium | Playwright | Niveau |
|---|---|---|
| `Navigate().GoToUrl(u)`, `Url = u` | `GotoAsync(u)` | automatique |
| `Back()`, `Forward()`, `Refresh()` | `GoBackAsync()`, `GoForwardAsync()`, `ReloadAsync()` | automatique |
| `Title`, `Url`, `PageSource` | `TitleAsync()`, `Url`, `ContentAsync()` | automatique |
| `Manage().Window.Size = …`, `Maximize()` | `SetViewportSizeAsync` | automatique |
| `SwitchTo().Frame(…)` puis usages | `FrameLocator(…)` pour les usages qui suivent dans la même méthode | automatique |
| un cadre traversé en appelant une autre méthode du projet | **à reprendre** : passer le `IFrameLocator` à la méthode | à reprendre |
| `SwitchTo().Alert().Accept()` | gestionnaire de dialogue posé avant l'action qui l'ouvre | automatique |
| `WindowHandles`, `SwitchTo().Window(h)`, `NewWindow` | **à reprendre** : `Context.RunAndWaitForPageAsync` | à reprendre |

## Attentes

| Selenium | Playwright | Niveau |
|---|---|---|
| `new WebDriverWait(driver, t).Until(ExpectedConditions.ElementIsVisible(by))` | `Locator(sel).WaitForAsync(new() { State = Visible, Timeout = t })` | automatique |
| `ElementToBeClickable`, `InvisibilityOfElementLocated`, `TextToBePresentInElement…`, `TitleIs`, `UrlContains`… (18 conditions) | équivalent `WaitForAsync`, `WaitForURLAsync`, `WaitForFunctionAsync` | automatique |
| l'élément renvoyé par `Until` | conservé (`BasculeOutils.ApresAsync`) | automatique |
| `Until(d => …)` avec une lambda | sondée par `BasculeOutils.AttendreJusquaAsync` avec le même délai | automatique |
| `Manage().Timeouts().ImplicitWait = …` | délai repris dans `FindElements`, `Count`, `Displayed` (comme Selenium) ; Playwright attend déjà sur chaque action | automatique |
| `Thread.Sleep(n)` | `Task.Delay(n)` | à relire (souvent inutile avec Playwright) |

## JavaScript, cookies, captures

| Selenium | Playwright | Niveau |
|---|---|---|
| `ExecuteScript("return …", args)` | `EvaluateAsync<T>` si le type est connu, sinon `BasculeOutils.ScriptAsync` qui rend les mêmes types que Selenium (`long`, `double`, `bool`, `string`, collections) | automatique |
| `ExecuteAsyncScript` | **à reprendre** : `EvaluateAsync` avec une promesse | à reprendre |
| `Manage().Cookies.GetCookieNamed(n)`, `DeleteAllCookies()` | `Context.CookiesAsync()`, `Context.ClearCookiesAsync()` | automatique |
| `AddCookie(new Cookie(n, v))` | `Context.AddCookiesAsync` sur l'URL courante | automatique ; à relire avec domaine, chemin ou expiration ; à reprendre si le cookie vient d'ailleurs |
| `GetScreenshot().SaveAsFile(f)` | `ScreenshotAsync(new() { Path = f })` | automatique |

## Exceptions et assertions

* Un `try` / `catch (NoSuchElementException)` garde son sens : les recherches du bloc échouent tout de suite si l'élément manque (`BasculeOutils.ExigerAsync`, exception `ElementIntrouvableException`).
* `WebDriverTimeoutException` devient `TimeoutException`. `StaleElementReferenceException` devient `PlaywrightException`, à relire : un élément « périmé » n'existe pas avec Playwright, le localisateur est réévalué à chaque usage.
* Une assertion qui porte directement sur une lecture de la page (texte, titre, URL, visibilité, état, valeur, attribut) devient une **assertion web de Playwright**, qui réessaie jusqu'au délai : `Assert.That(driver.Title, Is.EqualTo("Accueil"))` devient `await Expect(Page).ToHaveTitleAsync("Accueil")`, `Assert.AreEqual("3", panier.Text)` devient `await Expect(panier).ToHaveTextAsync("3")`. Égal, différent, contient, commence ou finit par, vrai et faux sont reconnus, pour NUnit (`Assert.That` et classique), MSTest et xUnit ; le message de l'assertion est conservé.
* Les autres assertions restent celles du cadre de test ; seules les valeurs lues deviennent `await`.

## Ce qui est hors du périmètre

* **Appium** (applications mobiles natives) : Playwright ne les pilote pas. Le fichier est signalé une fois, ses parties web sont migrées, les pilotes Appium restent.
* `SessionId`, `Capabilities`, journaux du navigateur, BiDi : pas d'équivalent direct, signalés.
* Toute propriété ou méthode de Selenium que Bascule ne connaît pas : elle n'est jamais laissée en silence, un filet de sécurité la signale à reprendre.

## Ajouté au projet migré

`BasculeOutils.cs` (espace de noms `Bascule.Transition`, C# 8, aucune dépendance autre que Playwright) apparaît seulement si la suite en a besoin : attentes (`AttendreJusquaAsync`, `ApresAsync`), attente implicite (`TousAsync`, `CompterAsync`, `VisibleAsync`), dialogues, scripts (`ScriptAsync`), recherche stricte (`ExigerAsync`). Il contient aussi `Synchrone`, qui exécute un appel Playwright de façon bloquante là où une méthode ne peut pas devenir asynchrone (lambda d'un `Where` ou d'un `Count`, `Dispose`) : chaque usage est signalé **à relire**, car il vaut mieux le réécrire avec `await` quand c'est possible. Vous pouvez garder le fichier, ou remplacer ses appels par du Playwright idiomatique au fil de l'eau. Il est sous licence MIT (en-tête du fichier).
