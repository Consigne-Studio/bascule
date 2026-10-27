using System.Diagnostics;
using System.Reflection;
using System.Text;
using Bascule.Chargement;
using Bascule.Rapport;

namespace Bascule.Cli;

internal static class Program
{
#if EDITION_PRO
    private const string Edition = "pro";
#else
    private const string Edition = "gratuite";
#endif

    private static string Version =>
        typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "1.0.0";

    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            if (args.Length == 0 || args[0] is "aide" or "help" or "-h" or "--help" or "/?")
            {
                Aide();
                return args.Length == 0 ? 1 : 0;
            }
            if (args[0] is "version" or "--version")
            {
                Console.WriteLine($"Bascule {Version} (édition {Edition})");
                return 0;
            }
            var options = Options.Lire(args.Skip(1).ToArray());
            return args[0] switch
            {
                "diagnostic" => Diagnostic(options),
                "migrer" => Migrer(options),
                _ => Erreur($"Commande inconnue : {args[0]}. Tapez « bascule aide »."),
            };
        }
        catch (OptionInvalideException e)
        {
            return Erreur(e.Message);
        }
        catch (FileNotFoundException e)
        {
            return Erreur(e.Message);
        }
    }

    private static int Erreur(string message)
    {
        Console.Error.WriteLine("Erreur : " + message);
        return 2;
    }

    private static void Aide()
    {
        Console.WriteLine($"""
            Bascule {Version} — migration Selenium C# vers Playwright .NET (édition {Edition})

            Utilisation :
              bascule diagnostic <chemin> [--rapport <dossier>]
                  Analyse la suite (dossier, .sln, .slnx ou .csproj) sans rien modifier.
                  Produit bascule-rapport.html et bascule-rapport.json.

              bascule migrer <chemin> [--sortie <dossier> | --en-place] [--compiler] [--rapport <dossier>]
                  Réécrit la suite en Playwright .NET. Par défaut, dans une copie à côté
                  de la source (<dossier>-playwright). --en-place modifie les fichiers d'origine
                  (à réserver à un dépôt Git propre). La suite migrée est recompilée en mémoire :
                  toute erreur apparue devient un point « à reprendre ». --compiler lance
                  aussi « dotnet build » sur le résultat.
                  (édition Pro)

              bascule version
              bascule aide

            Le code n'est ni compilé ni exécuté pendant l'analyse, et rien n'est envoyé sur le réseau.
            """);
    }

    private static int Diagnostic(Options o)
    {
        var espace = Espace.Ouvrir(o.Chemin);
        Console.WriteLine($"Analyse de {espace.Racine} : {espace.Projets.Count} projet(s)…");
        var rapport = Bascule.Analyse.Diagnostic.Analyser(espace, Version, Edition);
        var dossier = o.Rapport ?? espace.Racine;
        Ecrire(rapport, dossier);
        Resume(rapport);
        return 0;
    }

    private static int Migrer(Options o)
    {
#if EDITION_PRO
        var espace = Espace.Ouvrir(o.Chemin);
        var sortie = o.EnPlace ? espace.Racine : Path.GetFullPath(o.Sortie ?? espace.Racine.TrimEnd(Path.DirectorySeparatorChar) + "-playwright");
        if (!o.EnPlace)
        {
            if (Directory.Exists(sortie) && Directory.EnumerateFileSystemEntries(sortie).Any())
                throw new OptionInvalideException($"Le dossier de sortie existe et n'est pas vide : {sortie}. Choisissez-en un autre avec --sortie.");
            if (sortie.StartsWith(espace.Racine + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new OptionInvalideException("Le dossier de sortie ne peut pas être à l'intérieur du dossier analysé.");
        }
        Console.WriteLine($"Migration de {espace.Racine} : {espace.Projets.Count} projet(s)…");
        var chrono = Stopwatch.StartNew();
        var resultat = Bascule.Conversion.Convertisseur.Migrer(espace, Version, new Progress<string>(m => Console.WriteLine("  " + m)));
        if (!o.EnPlace) CopierDossier(espace.Racine, sortie);
        foreach (var f in resultat.Fichiers)
        {
            var chemin = Path.Combine(sortie, f.CheminRelatif);
            Directory.CreateDirectory(Path.GetDirectoryName(chemin)!);
            File.WriteAllText(chemin, f.Contenu, new UTF8Encoding(EncoderAvecBom(chemin)));
        }
        resultat.Rapport.Sortie = sortie;
        Console.WriteLine($"  {resultat.Fichiers.Count(f => !f.Nouveau)} fichier(s) modifié(s), {resultat.Fichiers.Count(f => f.Nouveau)} ajouté(s), en {chrono.Elapsed.TotalSeconds:0.0} s.");
        if (o.Compiler)
        {
            Console.WriteLine("Compilation du résultat (dotnet build)…");
            resultat.Rapport.Compilation = Compilation.Lancer(sortie, out var erreurs);
            // Une erreur que la vérification en mémoire n'avait pas vue devient aussi un point « à reprendre ».
            foreach (var (fichier, ligne, texte) in erreurs.DistinctBy(e => (e.Fichier, e.Ligne)))
                if (!resultat.Rapport.Constats.Any(c => c.Categorie == Categories.Compilation && c.Fichier == fichier && c.Ligne == ligne))
                    resultat.Rapport.Constats.Add(new Constat(fichier, ligne, Categories.Compilation, Niveau.Manuel,
                        $"Erreur de « dotnet build » après la migration ({texte}). Ligne {ligne} du fichier migré : à reprendre à la main."));
        }
        Ecrire(resultat.Rapport, o.Rapport ?? sortie);
        Resume(resultat.Rapport);
        return resultat.Rapport.Compilation is { Reussi: false } ? 3 : 0;
#else
        _ = o;
        return Erreur("La migration automatique fait partie de l'édition Pro (licence par organisation, voir le README du dépôt). L'édition gratuite produit le diagnostic complet : « bascule diagnostic <chemin> ».");
#endif
    }

    private static bool EncoderAvecBom(string chemin) => File.Exists(chemin) && File.ReadAllBytes(chemin) is [0xEF, 0xBB, 0xBF, ..];

    private static void CopierDossier(string source, string cible)
    {
        foreach (var d in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, d);
            if (Ignore(rel)) continue;
            Directory.CreateDirectory(Path.Combine(cible, rel));
        }
        Directory.CreateDirectory(cible);
        foreach (var f in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, f);
            if (Ignore(rel)) continue;
            File.Copy(f, Path.Combine(cible, rel), overwrite: false);
        }
    }

    private static bool Ignore(string relatif)
    {
        var parts = relatif.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(p => p is "bin" or "obj" or ".git" or ".vs" or "node_modules" or "TestResults");
    }

    private static void Ecrire(RapportBascule rapport, string dossier)
    {
        Directory.CreateDirectory(dossier);
        var html = Path.Combine(dossier, "bascule-rapport.html");
        var json = Path.Combine(dossier, "bascule-rapport.json");
        File.WriteAllText(html, RapportHtml.Generer(rapport), new UTF8Encoding(false));
        File.WriteAllText(json, RapportJson.Generer(rapport), new UTF8Encoding(false));
        Console.WriteLine($"Rapport : {html}");
    }

    private static void Resume(RapportBascule r)
    {
        Console.WriteLine();
        Console.WriteLine($"  Tests trouvés            {r.Tests + r.Scenarios}");
        Console.WriteLine(r.Mode == "migration"
            ? $"  Conversions automatiques {r.Compter(Niveau.Automatique)}"
            : $"  Usages convertibles      {r.Compter(Niveau.Automatique)}");
        Console.WriteLine($"  À relire                 {r.Compter(Niveau.ARelire)}");
        Console.WriteLine($"  À reprendre à la main    {r.Compter(Niveau.Manuel)}");
        Console.WriteLine($"  Part automatique         {r.TauxAutomatique:P0}");
        Console.WriteLine($"  Travail restant estimé   ≈ {r.HeuresRestantes.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',')} h (ordre de grandeur, pas un devis)");
        if (r.Compilation is { } c)
            Console.WriteLine($"  Compilation              {(c.Reussi ? "réussie" : $"{c.Erreurs} erreur(s)")}");
    }
}

internal sealed class OptionInvalideException(string message) : Exception(message);

internal sealed class Options
{
    public string Chemin { get; private set; } = ".";
    public string? Sortie { get; private set; }
    public string? Rapport { get; private set; }
    public bool EnPlace { get; private set; }
    public bool Compiler { get; private set; }

    public static Options Lire(string[] args)
    {
        var o = new Options();
        bool chemin = false;
        for (int i = 0; i < args.Length; i++)
        {
            string Valeur() => i + 1 < args.Length ? args[++i] : throw new OptionInvalideException($"{args[i]} attend une valeur.");
            switch (args[i])
            {
                case "--sortie" or "-o": o.Sortie = Path.GetFullPath(Valeur()); break;
                case "--rapport": o.Rapport = Path.GetFullPath(Valeur()); break;
                case "--en-place": o.EnPlace = true; break;
                case "--compiler": o.Compiler = true; break;
                default:
                    if (args[i].StartsWith('-')) throw new OptionInvalideException($"Option inconnue : {args[i]}.");
                    if (chemin) throw new OptionInvalideException("Un seul chemin à analyser.");
                    o.Chemin = args[i];
                    chemin = true;
                    break;
            }
        }
        if (o.EnPlace && o.Sortie is not null) throw new OptionInvalideException("--en-place et --sortie s'excluent.");
        return o;
    }
}
