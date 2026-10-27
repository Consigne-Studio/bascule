using System.Globalization;
using System.Text;

namespace Bascule.Rapport;

/// <summary>Rapport HTML autonome (polices intégrées, aucun appel réseau), à la charte « Consigne » :
/// planche grise, cadre à zones, bulle d'instrument DEV-606, cartouche, une seule couleur de signal (vert marche).</summary>
public static class RapportHtml
{
    public const string Repere = "DEV-606";
    private const string Signal = "#2BB673";
    // Format français construit à la main : l'outil doit tourner aussi sans données ICU (mode « globalization invariant »).
    private static readonly NumberFormatInfo Fr = new() { NumberDecimalSeparator = ",", NumberGroupSeparator = "\u00A0", NumberGroupSizes = [3] };

    private static string E(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
    private static string N(double v) => v.ToString(v % 1 == 0 ? "#,0" : "#,0.#", Fr);
    private static string Pct(double v) => Math.Round(v * 100).ToString("0", Fr) + " %";

    public static string Generer(RapportBascule r)
    {
        bool migration = r.Mode == "migration";
        var nom = Path.GetFileName(r.Source.TrimEnd('/', '\\'));
        var auto = r.Compter(Niveau.Automatique);
        var relire = r.Compter(Niveau.ARelire);
        var manuel = r.Compter(Niveau.Manuel);
        var sb = new StringBuilder(64 * 1024);
        sb.Append("<!doctype html><html lang=\"fr\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        sb.Append($"<title>Bascule · {(migration ? "migration" : "diagnostic")} · {E(nom)}</title>");
        sb.Append($"<meta name=\"generator\" content=\"Bascule {E(r.Version)}\"><style>");
        sb.Append($"@font-face{{font-family:Archivo;src:url(data:font/woff2;base64,{Police("Archivo-var.woff2")}) format('woff2');font-weight:100 900;font-stretch:62% 125%}}");
        sb.Append($"@font-face{{font-family:'Martian Mono';src:url(data:font/woff2;base64,{Police("MartianMono-var.woff2")}) format('woff2');font-weight:100 800;font-stretch:75% 112.5%}}");
        sb.Append($"@font-face{{font-family:Atkinson;src:url(data:font/woff2;base64,{Police("AtkinsonHyperlegibleNext-var.woff2")}) format('woff2');font-weight:200 800}}");
        sb.Append(Css);
        sb.Append("</style></head><body><main class=\"planche\">");
        sb.Append(Cadre());

        // ---- Couverture : bulle, titre, chiffres clés, cartouche.
        sb.Append("<section class=\"couv\"><div class=\"tete\">");
        sb.Append(Bulle());
        sb.Append($"<div class=\"data sous\">{(migration ? "Rapport de migration" : "Diagnostic avant migration")}<br>Selenium C# → Playwright .NET</div></div>");
        sb.Append($"<h1 class=\"titre\">{(migration ? "Migration" : "Diagnostic")}<br>{E(nom)}</h1>");
        sb.Append("<p class=\"intro\">");
        sb.Append(migration
            ? $"Bascule a réécrit cette suite Selenium en Playwright .NET. Ce rapport liste ce qui a été converti, ce qui mérite une relecture et ce qui reste à reprendre à la main, fichier par fichier."
            : $"Bascule a analysé cette suite sans la modifier ni l'exécuter. Ce rapport annonce ce que la migration convertira seule, ce qui sera à relire et ce qui restera à reprendre à la main.");
        sb.Append("</p>");
        sb.Append("<div class=\"specs\">");
        Spec(sb, N(r.Tests + r.Scenarios), r.Scenarios > 0 ? "tests et scénarios" : "tests trouvés");
        Spec(sb, Pct(r.TauxAutomatique), migration ? "conversions automatiques" : "convertible automatiquement");
        Spec(sb, N(relire), "points à relire");
        Spec(sb, N(manuel), "points à reprendre");
        Spec(sb, "≈ " + N(r.HeuresRestantes) + " h", "travail restant estimé");
        sb.Append("</div>");
        sb.Append("<div class=\"pied\"><div class=\"data gen\">");
        sb.Append($"Bascule {E(r.Version)} · édition {E(r.Edition)}<br>Analyse locale : aucun code envoyé, aucun test exécuté");
        sb.Append("</div>");
        sb.Append("<div class=\"cart\"><div class=\"r\" style=\"grid-template-columns:1fr 110px\">");
        sb.Append($"<div class=\"c\"><div class=\"k data\">Désignation</div><div class=\"v titre\">{E(nom)}</div></div>");
        sb.Append($"<div class=\"c\"><div class=\"k data\">Repère</div><div class=\"v data\">{Repere}</div></div></div>");
        sb.Append("<div class=\"r\" style=\"grid-template-columns:1fr 1fr 110px\">");
        sb.Append($"<div class=\"c\"><div class=\"k data\">Mode</div><div class=\"v data\">{(migration ? "Migration" : "Diagnostic")}</div></div>");
        sb.Append($"<div class=\"c\"><div class=\"k data\">Date</div><div class=\"v data\">{r.Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}</div></div>");
        sb.Append($"<div class=\"c\"><div class=\"k data\">Projets</div><div class=\"v data\">{r.Projets.Count}</div></div></div></div></div>");
        sb.Append("</section>");

        // ---- Exemple signature : un test avant / après, une ligne marquée.
        var exemple = ExempleSignature(r);
        if (exemple is not null)
        {
            sb.Append("<section class=\"feuille\"><header><span class=\"data\">01 · Avant / après</span><h2 class=\"titre\">Une conversion de la suite</h2></header>");
            sb.Append($"<p class=\"t\">{E(exemple.Message)} <span class=\"lieu data\">{E(exemple.Fichier)} · ligne {exemple.Ligne}</span></p>");
            sb.Append("<div class=\"diff\"><div><div class=\"lab data\">Selenium</div><pre class=\"code\">");
            sb.Append(E(exemple.Avant));
            sb.Append("</pre></div><div><div class=\"lab data\">Playwright</div><pre class=\"code signal\">");
            sb.Append(E(exemple.Apres));
            sb.Append("</pre></div></div></section>");
        }

        // ---- Synthèse par catégorie.
        sb.Append($"<section class=\"feuille\"><header><span class=\"data\">{(exemple is null ? "01" : "02")} · Synthèse</span><h2 class=\"titre\">Par catégorie</h2></header>");
        sb.Append("<table class=\"cat\"><thead><tr><th>Catégorie</th><th class=\"n\">Automatique</th><th class=\"n\">À relire</th><th class=\"n\">À reprendre</th><th style=\"width:34%\">Répartition</th></tr></thead><tbody>");
        foreach (var cat in Categories.Ordre)
        {
            var cs = r.Constats.Where(c => c.Categorie == cat && c.Niveau != Niveau.Information).ToList();
            if (cs.Count == 0) continue;
            int a = cs.Count(c => c.Niveau == Niveau.Automatique), l = cs.Count(c => c.Niveau == Niveau.ARelire), m = cs.Count(c => c.Niveau == Niveau.Manuel);
            double t = a + l + m;
            sb.Append($"<tr><td>{E(cat)}</td><td class=\"n\">{a}</td><td class=\"n\">{(l == 0 ? "·" : l)}</td><td class=\"n\">{(m == 0 ? "·" : m)}</td>");
            sb.Append($"<td><div class=\"barre\" title=\"{a} automatique(s), {l} à relire, {m} à reprendre\"><i class=\"a\" style=\"width:{(a / t * 100).ToString("0.##", CultureInfo.InvariantCulture)}%\"></i><i class=\"l\" style=\"width:{(l / t * 100).ToString("0.##", CultureInfo.InvariantCulture)}%\"></i><i class=\"m\" style=\"width:{(m / t * 100).ToString("0.##", CultureInfo.InvariantCulture)}%\"></i></div></td></tr>");
        }
        sb.Append($"<tr class=\"tot\"><td>Total</td><td class=\"n\">{auto}</td><td class=\"n\">{relire}</td><td class=\"n\">{manuel}</td><td></td></tr></tbody></table>");
        sb.Append("<p class=\"note legende\"><span class=\"cle a\"></span>automatique <span class=\"cle l\"></span>à relire <span class=\"cle m\"></span>à reprendre à la main</p>");
        sb.Append("<p class=\"note\">Travail restant estimé : 20 min par point à reprendre (5 min quand le même point se répète dans un fichier), 4 min par point à relire, 1 min par test pour l'exécuter et vérifier son résultat. C'est un ordre de grandeur pour planifier, pas un devis.</p>");
        sb.Append("</section>");

        // ---- Listes détaillées.
        int numero = exemple is null ? 2 : 3;
        Liste(sb, r, Niveau.Manuel, $"{numero++:00} · À reprendre à la main", "À reprendre", "Aucun point à reprendre : tout ce qui touche à Selenium a été converti.");
        Liste(sb, r, Niveau.ARelire, $"{numero++:00} · À relire", "À relire", "Aucun point à relire.");
        if (migration) Conversions(sb, r, $"{numero++:00} · Conversions automatiques");
        Projets(sb, r, $"{numero++:00} · Projets", migration);
        if (r.Compilation is { } comp)
        {
            sb.Append($"<section class=\"feuille\"><header><span class=\"data\">{numero++:00} · Vérification</span><h2 class=\"titre\">Compilation du résultat</h2></header>");
            sb.Append($"<p class=\"t\">« dotnet build » sur le dossier migré : <b>{(comp.Reussi ? "réussie" : "en échec")}</b>, {comp.Erreurs} erreur(s), {comp.Avertissements} avertissement(s).</p>");
            if (comp.PremieresErreurs.Count > 0)
                sb.Append("<pre class=\"code\">").Append(E(string.Join("\n", comp.PremieresErreurs))).Append("</pre>");
            sb.Append("</section>");
        }
        Methode(sb, migration, $"{numero:00} · Méthode");
        sb.Append("</main>");
        sb.Append(Script);
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static void Spec(StringBuilder sb, string valeur, string libelle) =>
        sb.Append($"<div><div class=\"titre\">{E(valeur)}</div><div class=\"data\">{E(libelle)}</div></div>");

    private static Constat? ExempleSignature(RapportBascule r)
    {
        // Une conversion lisible : une assertion ou une action, avec un avant et un après d'une ligne chacun.
        var candidats = r.Constats.Where(c => c.Niveau == Niveau.Automatique && c.Avant is { Length: > 20 and < 120 } && c.Apres is { Length: > 20 and < 140 }
                                             && !c.Avant.Contains('\n') && !c.Apres.Contains('\n')).ToList();
        return candidats.FirstOrDefault(c => c.Categorie == Categories.Assertions)
               ?? candidats.FirstOrDefault(c => c.Categorie == Categories.Attentes)
               ?? candidats.FirstOrDefault();
    }

    private static void Liste(StringBuilder sb, RapportBascule r, Niveau niveau, string numero, string titre, string vide)
    {
        var cs = r.Constats.Where(c => c.Niveau == niveau).OrderBy(c => c.Fichier, StringComparer.Ordinal).ThenBy(c => c.Ligne).ToList();
        sb.Append($"<section class=\"feuille\"><header><span class=\"data\">{E(numero)}</span><h2 class=\"titre\">{E(titre)} ({cs.Count})</h2></header>");
        if (cs.Count == 0)
        {
            sb.Append($"<p class=\"t\">{E(vide)}</p></section>");
            return;
        }
        sb.Append("<input class=\"filtre\" type=\"search\" placeholder=\"Filtrer (fichier, catégorie, texte)\" aria-label=\"Filtrer la liste\">");
        foreach (var g in cs.GroupBy(c => c.Fichier))
        {
            sb.Append($"<div class=\"fichier\"><div class=\"nom data\">{E(g.Key)} <span>· {g.Count()}</span></div>");
            foreach (var c in g)
            {
                sb.Append($"<div class=\"ligne\"><span class=\"no data\">L{c.Ligne}</span><div><div class=\"msg\"><span class=\"tag\">{E(c.Categorie)}</span> {E(c.Message)}</div>");
                if (!string.IsNullOrEmpty(c.Avant)) sb.Append($"<code>{E(c.Avant)}</code>");
                sb.Append("</div></div>");
            }
            sb.Append("</div>");
        }
        sb.Append("</section>");
    }

    private static void Conversions(StringBuilder sb, RapportBascule r, string numero)
    {
        var cs = r.Constats.Where(c => c.Niveau == Niveau.Automatique && !string.IsNullOrEmpty(c.Avant) && !string.IsNullOrEmpty(c.Apres)).ToList();
        sb.Append($"<section class=\"feuille\"><header><span class=\"data\">{E(numero)}</span><h2 class=\"titre\">Conversions automatiques ({r.Compter(Niveau.Automatique)})</h2></header>");
        sb.Append("<p class=\"t\">Le détail complet est dans bascule-rapport.json et dans le diff de votre dépôt. Voici les conversions regroupées par catégorie, cinq au plus par catégorie.</p>");
        foreach (var cat in Categories.Ordre)
        {
            var ex = cs.Where(c => c.Categorie == cat).GroupBy(c => c.Message).Select(g => g.First()).Take(5).ToList();
            if (ex.Count == 0) continue;
            sb.Append($"<h3>{E(cat)}</h3><table class=\"conv\"><thead><tr><th>Selenium</th><th>Playwright</th></tr></thead><tbody>");
            foreach (var c in ex)
                sb.Append($"<tr><td><code>{E(c.Avant)}</code><div class=\"lieu data\">{E(c.Fichier)} · L{c.Ligne}</div></td><td><code>{E(c.Apres)}</code><div class=\"note\">{E(c.Message)}</div></td></tr>");
            sb.Append("</tbody></table>");
        }
        sb.Append("</section>");
    }

    private static void Projets(StringBuilder sb, RapportBascule r, string numero, bool migration)
    {
        sb.Append($"<section class=\"feuille\"><header><span class=\"data\">{E(numero)}</span><h2 class=\"titre\">Projets analysés ({r.Projets.Count})</h2></header>");
        sb.Append("<table><thead><tr><th>Projet</th><th>Cadre de test</th><th class=\"n\">Fichiers</th><th class=\"n\">Avec Selenium</th><th class=\"n\">Tests</th>");
        if (migration) sb.Append("<th>Dépendances</th>");
        sb.Append("</tr></thead><tbody>");
        foreach (var p in r.Projets)
        {
            sb.Append($"<tr><td><b>{E(p.Nom)}</b><div class=\"lieu data\">{E(p.Chemin)}{(p.AncienFormat ? " · format ancien" : "")}</div></td><td>{E(p.Cadre)}</td><td class=\"n\">{p.Fichiers}</td><td class=\"n\">{p.FichiersSelenium}</td><td class=\"n\">{p.Tests + p.Scenarios}</td>");
            if (migration) sb.Append($"<td class=\"dep\">{string.Join("<br>", p.Dependances.Concat(p.FichiersAjoutes.Select(f => "Ajouté : " + f)).Select(E))}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table></section>");
    }

    private static void Methode(StringBuilder sb, bool migration, string numero)
    {
        sb.Append($"<section class=\"feuille\"><header><span class=\"data\">{E(numero)}</span><h2 class=\"titre\">Méthode et limites</h2></header><ul class=\"t\">");
        sb.Append("<li>Le code est lu avec Roslyn, le compilateur C#, et chaque appel est identifié par son type réel (IWebDriver, IWebElement, By…), pas par son nom.</li>");
        sb.Append("<li><b>Automatique</b> : Bascule produit l'équivalent Playwright. <b>À relire</b> : converti, mais le comportement peut différer (pause fixe, dialogue, attribut calculé). <b>À reprendre</b> : sans équivalent automatique (fenêtres multiples, grille distante, DevTools…) ; le code est laissé tel quel et Selenium reste référencé tant qu'il sert.</li>");
        sb.Append(migration
            ? "<li>La migration ne compile ni n'exécute vos tests (sauf option --compiler). Lancez la suite après migration : c'est la seule preuve qui compte.</li>"
            : "<li>Le diagnostic ne modifie aucun fichier et n'exécute rien. Ses chiffres suivent les règles de la migration (édition Pro) ; un écart de quelques points reste possible sur les cas rares.</li>");
        sb.Append("<li>Rien n'est envoyé sur le réseau : l'analyse se fait entièrement sur votre poste.</li></ul></section>");
    }

    private static string Bulle() =>
        $"<svg class=\"bulle\" viewBox=\"0 0 120 120\" role=\"img\" aria-label=\"{Repere}\"><circle cx=\"60\" cy=\"60\" r=\"57\" fill=\"{Signal}\" stroke=\"#1B1E21\" stroke-width=\"3\"/>" +
        "<line x1=\"3\" y1=\"60\" x2=\"117\" y2=\"60\" stroke=\"#1B1E21\" stroke-width=\"3\"/>" +
        "<text x=\"60\" y=\"49\" text-anchor=\"middle\" font-family=\"Archivo\" font-weight=\"800\" font-stretch=\"62%\" font-size=\"34\" fill=\"#1B1E21\">DEV</text>" +
        "<text x=\"60\" y=\"92\" text-anchor=\"middle\" font-family=\"Martian Mono\" font-weight=\"500\" font-stretch=\"87.5%\" font-size=\"22\" fill=\"#1B1E21\">606</text></svg>";

    private static string Cadre() =>
        "<div class=\"zones h data\" aria-hidden=\"true\"><span>A</span><span>B</span><span>C</span><span>D</span><span>E</span><span>F</span></div>";

    private static string Police(string nom)
    {
        using var s = typeof(RapportHtml).Assembly.GetManifestResourceStream("Bascule.Rapport.Ressources." + nom)
                      ?? throw new InvalidOperationException($"Police manquante : {nom}");
        using var m = new MemoryStream();
        s.CopyTo(m);
        return Convert.ToBase64String(m.ToArray());
    }

    private const string Css = """
        *{box-sizing:border-box;margin:0;padding:0}
        html{background:#D4D7DA}
        body{font-family:Atkinson,system-ui,sans-serif;color:#1B1E21;-webkit-font-smoothing:antialiased;font-size:15px;line-height:1.5}
        .planche{position:relative;max-width:1120px;margin:24px auto;padding:56px 56px 40px;border:1.5px solid #1B1E21;
          background:#D4D7DA;background-image:linear-gradient(rgba(27,30,33,.075) 1px,transparent 1px),linear-gradient(90deg,rgba(27,30,33,.075) 1px,transparent 1px);background-size:16px 16px}
        .zones.h{position:absolute;left:0;right:0;top:-20px;display:grid;grid-template-columns:repeat(6,1fr);text-align:center;font-size:10px;color:#5C636A}
        .titre{font-family:Archivo,'Arial Narrow',sans-serif;font-weight:800;font-stretch:62%;text-transform:uppercase;line-height:.88;letter-spacing:-.005em}
        .data{font-family:'Martian Mono',ui-monospace,monospace;font-stretch:87.5%;font-weight:500;text-transform:uppercase;letter-spacing:.06em;font-variant-numeric:tabular-nums}
        .couv .tete{display:flex;gap:24px;align-items:center}
        .bulle{width:104px;height:104px;flex:none}
        .sous{font-size:12px;color:#5C636A;line-height:1.7}
        h1{font-size:clamp(48px,8vw,96px);margin:40px 0 20px;overflow-wrap:anywhere}
        .intro{font-size:19px;max-width:720px}
        .specs{display:grid;grid-template-columns:repeat(5,1fr);border-top:1.5px solid #1B1E21;border-bottom:1.5px solid #1B1E21;margin:40px 0 32px}
        .specs>div{padding:14px 0 14px 14px;border-left:1px solid #1B1E21}.specs>div:first-child{padding-left:0;border-left:0}
        .specs .titre{font-size:44px}.specs .data{font-size:10px;margin-top:8px;color:#5C636A}
        .pied{display:flex;justify-content:space-between;align-items:flex-end;gap:24px;flex-wrap:wrap}
        .gen{font-size:10px;color:#5C636A;line-height:1.7}
        .cart{width:420px;max-width:100%;background:#F3F4F5;border:1.5px solid #1B1E21}
        .cart .r{display:grid;border-top:1px solid #1B1E21}.cart .r:first-child{border-top:0}
        .cart .c{padding:5px 9px 7px;border-left:1px solid #1B1E21;min-width:0}.cart .c:first-child{border-left:0}
        .cart .k{font-size:8px;color:#5C636A}
        .cart .v{font-size:12px;margin-top:3px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
        .cart .v.titre{font-size:22px}
        .feuille{background:#F3F4F5;border:1.5px solid #1B1E21;margin-top:32px;padding:28px 32px 32px}
        .feuille header{display:flex;flex-direction:column;gap:8px;border-bottom:1.5px solid #1B1E21;padding-bottom:12px;margin-bottom:20px}
        .feuille header .data{font-size:10px;color:#5C636A}
        .feuille h2{font-size:36px}
        h3{font-family:Archivo,'Arial Narrow',sans-serif;font-weight:800;font-stretch:62%;text-transform:uppercase;font-size:20px;line-height:1;margin:24px 0 10px}
        p.t{max-width:820px;margin-bottom:12px}
        ul.t{margin:0 0 0 18px}ul.t li{margin-bottom:8px;max-width:860px}
        .note{font-size:13px;color:#5C636A;margin-top:10px;max-width:820px}
        .lieu{font-size:10px;color:#5C636A;margin-top:4px;overflow-wrap:anywhere}
        table{width:100%;border-collapse:collapse;font-size:14px}
        th{font-family:'Martian Mono',monospace;font-stretch:87.5%;font-weight:500;text-transform:uppercase;letter-spacing:.06em;font-size:10px;text-align:left;color:#5C636A;border-bottom:1.5px solid #1B1E21;padding:6px 8px}
        td{padding:7px 8px;border-bottom:1px solid #D4D7DA;vertical-align:top}
        td.n,th.n{text-align:right;font-variant-numeric:tabular-nums;white-space:nowrap}
        tr.tot td{border-top:1.5px solid #1B1E21;border-bottom:0;font-weight:600}
        td.dep{font-size:12px;color:#2A2F34}
        .barre{height:14px;background:#D4D7DA;display:flex}
        .barre i{display:block;height:100%}
        .barre i.a,.cle.a{background:#1B1E21}.barre i.l,.cle.l{background:#9AA0A6}
        .barre i.m,.cle.m{background:repeating-linear-gradient(45deg,#1B1E21 0 1.5px,#F3F4F5 1.5px 5px)}
        .cle{display:inline-block;width:12px;height:12px;vertical-align:-1px;margin:0 6px 0 14px;border:1px solid #1B1E21}.cle:first-child{margin-left:0}
        code,pre.code{font-family:'Martian Mono',ui-monospace,monospace;font-stretch:87.5%;font-size:12px;letter-spacing:0}
        td code,.ligne code{display:block;white-space:pre-wrap;overflow-wrap:anywhere;background:#fff;border:1px solid #D4D7DA;padding:5px 8px;margin-top:6px}
        .diff{display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-top:8px}
        .diff .lab{font-size:10px;color:#5C636A;margin-bottom:6px}
        pre.code{white-space:pre-wrap;overflow-wrap:anywhere;background:#fff;border:1px solid #9AA0A6;padding:12px 14px;font-size:13px;line-height:1.6}
        pre.code.signal{border:1.5px solid #1B1E21;box-shadow:inset 6px 0 0 #2BB673}
        .tag{display:inline-block;font-family:'Martian Mono',monospace;font-stretch:87.5%;font-size:9px;letter-spacing:.06em;text-transform:uppercase;padding:1px 6px;border:1px solid #1B1E21;margin-right:4px;vertical-align:1px}
        .filtre{width:100%;max-width:420px;font:inherit;font-size:14px;padding:8px 10px;border:1.5px solid #1B1E21;background:#fff;border-radius:0;margin-bottom:16px}
        .fichier{border-top:1.5px solid #1B1E21;padding-top:10px;margin-top:16px}
        .fichier .nom{font-size:11px;margin-bottom:6px;overflow-wrap:anywhere}.fichier .nom span{color:#5C636A}
        .ligne{display:grid;grid-template-columns:56px 1fr;gap:8px;padding:8px 0;border-bottom:1px solid #D4D7DA}
        .ligne .no{font-size:11px;color:#5C636A;padding-top:2px}
        .conv td{width:50%}
        .cache{display:none}
        @media (max-width:760px){.planche{padding:32px 16px;margin:0;border-left:0;border-right:0}.specs{grid-template-columns:1fr 1fr}.specs>div{border-left:0;padding-left:0;border-top:1px solid #1B1E21}
          .diff{grid-template-columns:1fr}.feuille{padding:20px 16px}table{display:block;overflow-x:auto;max-width:100%}td,th{padding:6px 5px}.cat th:last-child,.cat td:last-child{display:none}}
        @media print{html,body{background:none}.planche{border:0;margin:0;max-width:none}.filtre{display:none}.feuille{break-inside:avoid-page}}
        """;

    private const string Script = """
        <script>
        document.querySelectorAll('.filtre').forEach(function (champ) {
          champ.addEventListener('input', function () {
            var q = champ.value.toLowerCase();
            champ.parentElement.querySelectorAll('.fichier').forEach(function (f) {
              var visibles = 0;
              f.querySelectorAll('.ligne').forEach(function (l) {
                var ok = !q || (f.querySelector('.nom').textContent + ' ' + l.textContent).toLowerCase().indexOf(q) >= 0;
                l.classList.toggle('cache', !ok); if (ok) visibles++;
              });
              f.classList.toggle('cache', visibles === 0);
            });
          });
        });
        </script>
        """;
}
