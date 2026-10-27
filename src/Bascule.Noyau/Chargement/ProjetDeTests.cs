using System.Xml.Linq;

namespace Bascule.Chargement;

/// <summary>Cadre de test détecté dans un projet.</summary>
public enum CadreDeTest { Inconnu, NUnit, MSTest, XUnit, XUnitV3 }

/// <summary>Un projet C# (.csproj) et les fichiers source qu'il compile.</summary>
public sealed class ProjetDeTests
{
    public required string CheminCsproj { get; init; }
    public required string Dossier { get; init; }
    public required string Nom { get; init; }
    public required bool StyleSdk { get; init; }
    public required IReadOnlyList<string> Fichiers { get; init; }
    public required IReadOnlyList<string> Paquets { get; init; }
    public required IReadOnlyList<string> ReferencesDeProjet { get; init; }
    public required CadreDeTest Cadre { get; init; }
    public required string EspaceDeNomsRacine { get; init; }
    /// <summary>Usings globaux implicites (ImplicitUsings) et déclarés (&lt;Using Include="…" /&gt;), absents des fichiers source.</summary>
    public IReadOnlyList<string> UsingsGlobaux { get; init; } = [];
    public bool UtiliseReqnroll { get; init; }
    public bool UtiliseSpecFlow { get; init; }

    public bool ReferenceSelenium =>
        Paquets.Any(p => p.StartsWith("Selenium.", StringComparison.OrdinalIgnoreCase)
                      || p.StartsWith("DotNetSeleniumExtras", StringComparison.OrdinalIgnoreCase)
                      || p.StartsWith("SeleniumExtras", StringComparison.OrdinalIgnoreCase));

    public string LibelleCadre => Cadre switch
    {
        CadreDeTest.NUnit => "NUnit",
        CadreDeTest.MSTest => "MSTest",
        CadreDeTest.XUnit => "xUnit",
        CadreDeTest.XUnitV3 => "xUnit v3",
        _ => "aucun cadre de test reconnu",
    } + (UtiliseReqnroll ? " + Reqnroll" : UtiliseSpecFlow ? " + SpecFlow" : "");

    /// <summary>Lit un .csproj. Les projets « ancien format » (non SDK) sont lus aussi : leurs Compile Include donnent la liste des fichiers.</summary>
    public static ProjetDeTests Lire(string cheminCsproj)
    {
        cheminCsproj = Path.GetFullPath(cheminCsproj);
        var dossier = Path.GetDirectoryName(cheminCsproj)!;
        var doc = XDocument.Load(cheminCsproj);
        var racine = doc.Root!;
        bool sdk = racine.Attribute("Sdk") is not null
                   || racine.Elements().Any(e => e.Name.LocalName == "Sdk")
                   || racine.Elements().Any(e => e.Name.LocalName == "Import" && (string?)e.Attribute("Sdk") is not null);

        IEnumerable<XElement> Tous(string nom) => racine.Descendants().Where(e => e.Name.LocalName == nom);

        var paquets = Tous("PackageReference")
            .Select(e => (string?)e.Attribute("Include") ?? (string?)e.Attribute("Update"))
            .Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!).ToList();

        // Ancien format : packages.config, et <Reference Include="WebDriver, Version=…">.
        var packagesConfig = Path.Combine(dossier, "packages.config");
        if (File.Exists(packagesConfig))
        {
            try
            {
                paquets.AddRange(XDocument.Load(packagesConfig).Descendants("package")
                    .Select(e => (string?)e.Attribute("id")).Where(n => n is not null).Select(n => n!));
            }
            catch (System.Xml.XmlException) { /* fichier illisible : ignoré */ }
        }
        foreach (var r in Tous("Reference"))
        {
            var inc = ((string?)r.Attribute("Include"))?.Split(',')[0].Trim();
            if (inc is "WebDriver") paquets.Add("Selenium.WebDriver");
            else if (inc is "WebDriver.Support") paquets.Add("Selenium.Support");
            else if (inc is "nunit.framework") paquets.Add("NUnit");
            else if (inc is "Microsoft.VisualStudio.QualityTools.UnitTestFramework") paquets.Add("MSTest.TestFramework");
        }
        paquets = paquets.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var refsProjet = Tous("ProjectReference")
            .Select(e => (string?)e.Attribute("Include")).Where(n => n is not null)
            .Select(n => Path.GetFullPath(Path.Combine(dossier, n!.Replace('\\', Path.DirectorySeparatorChar))))
            .ToList();

        List<string> fichiers;
        if (sdk)
        {
            var exclus = Tous("Compile").Select(e => (string?)e.Attribute("Remove")).Where(x => x is not null)
                .Select(x => Path.GetFullPath(Path.Combine(dossier, x!.Replace('\\', '/')))).ToHashSet();
            fichiers = SourcesSdk(dossier).Where(f => !exclus.Contains(f)).ToList();
        }
        else
        {
            fichiers = Tous("Compile").Select(e => (string?)e.Attribute("Include")).Where(x => x is not null)
                .Select(x => Path.GetFullPath(Path.Combine(dossier, x!.Replace('\\', Path.DirectorySeparatorChar))))
                .Where(File.Exists).ToList();
        }
        fichiers.Sort(StringComparer.Ordinal);

        bool A(string prefixe) => paquets.Any(p => p.StartsWith(prefixe, StringComparison.OrdinalIgnoreCase));
        var cadre = A("xunit.v3") ? CadreDeTest.XUnitV3
                  : A("xunit") ? CadreDeTest.XUnit
                  : A("NUnit") ? CadreDeTest.NUnit
                  : A("MSTest") || A("Microsoft.VisualStudio.QualityTools") ? CadreDeTest.MSTest
                  : A("SpecFlow.NUnit") || A("Reqnroll.NUnit") ? CadreDeTest.NUnit
                  : A("SpecFlow.MsTest") || A("Reqnroll.MsTest") ? CadreDeTest.MSTest
                  : A("SpecFlow.xUnit") || A("Reqnroll.xUnit") ? CadreDeTest.XUnit
                  : CadreDeTest.Inconnu;

        var nom = Path.GetFileNameWithoutExtension(cheminCsproj);
        var espace = Tous("RootNamespace").Select(e => e.Value.Trim()).FirstOrDefault(v => v.Length > 0)
                     ?? nom.Replace('-', '_').Replace(' ', '_');

        var usings = new List<string>();
        var implicites = Tous("ImplicitUsings").Select(e => e.Value.Trim()).LastOrDefault();
        if (sdk && implicites is "enable" or "true")
            usings.AddRange(["System", "System.Collections.Generic", "System.IO", "System.Linq", "System.Net.Http", "System.Threading", "System.Threading.Tasks"]);
        foreach (var u in Tous("Using"))
        {
            if ((string?)u.Attribute("Include") is { Length: > 0 } inc && (string?)u.Attribute("Alias") is null && (string?)u.Attribute("Static") is not "true")
                usings.Add(inc);
            if ((string?)u.Attribute("Remove") is { Length: > 0 } rem) usings.Remove(rem);
        }

        return new ProjetDeTests
        {
            UsingsGlobaux = usings.Distinct().ToList(),
            CheminCsproj = cheminCsproj,
            Dossier = dossier,
            Nom = nom,
            StyleSdk = sdk,
            Fichiers = fichiers,
            Paquets = paquets,
            ReferencesDeProjet = refsProjet,
            Cadre = cadre,
            EspaceDeNomsRacine = espace,
            UtiliseReqnroll = A("Reqnroll"),
            UtiliseSpecFlow = A("SpecFlow"),
        };
    }

    /// <summary>Fichiers .cs d'un projet SDK : tout le dossier, sauf bin/obj et les sous-dossiers qui ont leur propre .csproj.</summary>
    private static IEnumerable<string> SourcesSdk(string dossier)
    {
        var pile = new Stack<string>();
        pile.Push(dossier);
        while (pile.Count > 0)
        {
            var d = pile.Pop();
            foreach (var f in Directory.EnumerateFiles(d, "*.cs"))
                yield return Path.GetFullPath(f);
            foreach (var sous in Directory.EnumerateDirectories(d))
            {
                var n = Path.GetFileName(sous);
                if (n is "bin" or "obj" or "node_modules" || n.StartsWith('.')) continue;
                if (Directory.EnumerateFiles(sous, "*.csproj").Any()) continue;
                pile.Push(sous);
            }
        }
    }
}
