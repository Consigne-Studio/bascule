using Bascule.Rapport;

namespace Bascule.Tests;

/// <summary>Le diagnostic gratuit : chiffres stables sur les suites d'exemple, aucune écriture.</summary>
public class DiagnosticTests
{
    internal static string Exemple(string nom)
    {
        var d = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(d, "exemples"))) d = Path.GetDirectoryName(d)!;
        return Path.Combine(d, "exemples", "atelier", nom);
    }

    [Theory]
    [InlineData("selenium-nunit", 17, 3, 0)]
    [InlineData("selenium-mstest", 8, 0, 0)]
    [InlineData("selenium-xunit", 7, 0, 2)]
    public void Le_diagnostic_des_suites_d_exemple_est_stable(string nom, int tests, int aRelire, int manuels)
    {
        var diag = Bascule.Analyse.Diagnostic.Analyser(Bascule.Chargement.Espace.Ouvrir(Exemple(nom)), "test", "gratuite");
        Assert.Equal(tests, diag.Tests);
        Assert.Equal(aRelire, diag.Compter(Niveau.ARelire));
        Assert.Equal(manuels, diag.Compter(Niveau.Manuel));
        Assert.InRange(diag.TauxAutomatique, 0.9, 1.0);
    }

    [Fact]
    public void Le_diagnostic_ne_modifie_rien()
    {
        var dossier = Exemple("selenium-nunit");
        var avant = Directory.EnumerateFiles(dossier, "*", SearchOption.AllDirectories).Where(f => !f.Contains("/obj/") && !f.Contains("/bin/"))
            .ToDictionary(f => f, File.ReadAllText);
        Bascule.Analyse.Diagnostic.Analyser(Bascule.Chargement.Espace.Ouvrir(dossier), "test", "gratuite");
        foreach (var (f, t) in avant) Assert.Equal(t, File.ReadAllText(f));
    }

    [Fact]
    public void Le_rapport_html_est_autonome()
    {
        var diag = Bascule.Analyse.Diagnostic.Analyser(Bascule.Chargement.Espace.Ouvrir(Exemple("selenium-xunit")), "test", "gratuite");
        var html = RapportHtml.Generer(diag);
        Assert.Contains("@font-face", html);
        Assert.DoesNotContain("<script src", html);
        Assert.DoesNotContain("<link rel=\"stylesheet\"", html);
        Assert.Contains("PageCatalogue.cs", html);
    }
}
