using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bascule.Chargement;

/// <summary>Compilation Roslyn d'un projet, pour l'analyse sémantique.
/// Les assemblys Selenium embarquées par Bascule servent de références : le code du client n'a pas besoin d'être restauré ni compilé.</summary>
public static class Compilateur
{
    private static readonly Lazy<IReadOnlyList<MetadataReference>> References = new(ChargerReferences);

    public static CSharpParseOptions OptionsAnalyse { get; } =
        new(LanguageVersion.Preview, DocumentationMode.None, SourceCodeKind.Regular, ["DEBUG", "TRACE"]);

    public static SyntaxTree Analyser(string texte, string chemin) =>
        CSharpSyntaxTree.ParseText(texte, OptionsAnalyse, chemin, System.Text.Encoding.UTF8);

    /// <summary>Arbres de syntaxe du projet et de ses références de projet (transitives), sans doublon.</summary>
    public static CSharpCompilation Compiler(ProjetDeTests projet, IReadOnlyDictionary<string, SyntaxTree> arbres, Func<string, ProjetDeTests?> projetParChemin)
    {
        var vus = new HashSet<string>(StringComparer.Ordinal);
        var fichiers = new List<string>();
        void Ajouter(ProjetDeTests p, int profondeur)
        {
            if (!vus.Add(p.CheminCsproj) || profondeur > 16) return;
            fichiers.AddRange(p.Fichiers);
            foreach (var r in p.ReferencesDeProjet)
                if (projetParChemin(r) is { } rp) Ajouter(rp, profondeur + 1);
        }
        Ajouter(projet, 0);
        var liste = fichiers.Distinct(StringComparer.Ordinal).Where(arbres.ContainsKey).Select(f => arbres[f]).ToList();
        if (projet.UsingsGlobaux.Count > 0)
            liste.Add(Analyser(string.Concat(projet.UsingsGlobaux.Select(u => $"global using global::{u};\n")), Path.Combine(projet.Dossier, "obj", "UsingsGlobaux.g.cs")));
        return CSharpCompilation.Create(
            projet.Nom + ".Bascule",
            liste,
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    /// <summary>Compile un seul texte (tests, extraits).</summary>
    public static CSharpCompilation CompilerTexte(params string[] sources) =>
        CSharpCompilation.Create("Extrait",
            sources.Select((s, i) => Analyser(s, $"Extrait{i}.cs")),
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    private static IReadOnlyList<MetadataReference> ChargerReferences()
    {
        var chemins = new HashSet<string>(StringComparer.Ordinal);
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpa)
            foreach (var c in tpa.Split(Path.PathSeparator))
                if (c.Length > 0) chemins.Add(c);
        foreach (var t in new[]
                 {
                     typeof(OpenQA.Selenium.IWebDriver), typeof(OpenQA.Selenium.Support.UI.SelectElement),
                     typeof(SeleniumExtras.WaitHelpers.ExpectedConditions), typeof(SeleniumExtras.PageObjects.PageFactory),
                 })
            chemins.Add(t.Assembly.Location);
        return chemins.Where(File.Exists).Select(c => (MetadataReference)MetadataReference.CreateFromFile(c)).ToList();
    }
}
