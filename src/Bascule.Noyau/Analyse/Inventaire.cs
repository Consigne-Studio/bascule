using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Bascule.Chargement;
using Bascule.Rapport;

namespace Bascule.Analyse;

/// <summary>Comptages d'un projet : fichiers, tests, scénarios BDD.</summary>
public static class Inventaire
{
    private static readonly HashSet<string> AttributsTest = new(StringComparer.Ordinal)
    {
        "Test", "TestCase", "TestCaseSource", "Theory", "Fact", "TestMethod", "DataTestMethod",
    };

    public static RapportProjet ResumeProjet(ProjetDeTests projet, string racine, IReadOnlyDictionary<string, SyntaxTree> arbres)
    {
        var rp = new RapportProjet
        {
            Nom = projet.Nom,
            Chemin = Path.GetRelativePath(racine, projet.CheminCsproj).Replace('\\', '/'),
            Cadre = projet.LibelleCadre,
            Fichiers = projet.Fichiers.Count,
            AncienFormat = !projet.StyleSdk,
        };
        foreach (var f in projet.Fichiers)
        {
            if (!arbres.TryGetValue(f, out var arbre)) continue;
            var racineSyntaxe = arbre.GetRoot();
            if (arbre.ToString().Contains("OpenQA.Selenium", StringComparison.Ordinal)) rp.FichiersSelenium++;
            foreach (var m in racineSyntaxe.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var attrs = m.AttributeLists.SelectMany(l => l.Attributes).Select(Nom).ToList();
                if (!attrs.Any(AttributsTest.Contains)) continue;
                // Un [TestCase] par jeu de données : chaque cas est un test.
                var cas = attrs.Count(a => a is "TestCase" or "DataRow" or "InlineData");
                rp.Tests += Math.Max(1, cas);
            }
        }
        // Scénarios Gherkin (SpecFlow, Reqnroll).
        if (projet.UtiliseSpecFlow || projet.UtiliseReqnroll)
            foreach (var feature in Directory.EnumerateFiles(projet.Dossier, "*.feature", SearchOption.AllDirectories))
                rp.Scenarios += File.ReadLines(feature).Count(l => l.TrimStart().StartsWith("Scenario", StringComparison.OrdinalIgnoreCase)
                                                                || l.TrimStart().StartsWith("Scénario", StringComparison.OrdinalIgnoreCase));
        return rp;
    }

    private static string Nom(AttributeSyntax a)
    {
        var n = a.Name switch
        {
            QualifiedNameSyntax q => q.Right.Identifier.ValueText,
            SimpleNameSyntax s => s.Identifier.ValueText,
            _ => a.Name.ToString(),
        };
        return n.EndsWith("Attribute", StringComparison.Ordinal) ? n[..^"Attribute".Length] : n;
    }
}
