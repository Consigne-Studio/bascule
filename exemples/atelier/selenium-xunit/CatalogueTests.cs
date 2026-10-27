using Atelier.XUnit.Pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Atelier.XUnit;

public class CatalogueTests : NavigateurTest
{
    private PageCatalogue Connecte()
    {
        new PageAccueil(Driver).Ouvrir(BaseUrl).SeConnecter("demo", "demo");
        new WebDriverWait(Driver, TimeSpan.FromSeconds(5)).Until(d => d.Url.Contains("catalogue"));
        return new PageCatalogue(Driver).Attendre();
    }

    [Fact]
    public void Mauvais_mot_de_passe_affiche_l_erreur()
    {
        var accueil = new PageAccueil(Driver).Ouvrir(BaseUrl);
        accueil.SeConnecter("demo", "erreur");
        Assert.True(accueil.ErreurAffichee);
        Assert.Equal("Identifiant ou mot de passe incorrect.", accueil.TexteErreur);
    }

    [Theory]
    [InlineData("pompe", "2 pièce(s)")]
    [InlineData("DN", "2 pièce(s)")]
    [InlineData("introuvable", "0 pièce(s)")]
    public void La_recherche_filtre_le_catalogue(string texte, string attendu)
    {
        var catalogue = Connecte();
        catalogue.Rechercher(texte);
        Assert.Equal(attendu, catalogue.Compteur);
    }

    [Fact]
    public void Les_designations_contiennent_la_sonde()
    {
        var catalogue = Connecte();
        var noms = catalogue.Designations.Select(e => e.Text).ToList();
        Assert.Contains("Sonde PT100", noms);
        Assert.Equal(8, noms.Count);
    }

    [Fact]
    public void L_aide_s_ouvre_dans_le_cadre()
    {
        var catalogue = Connecte();
        Assert.False(string.IsNullOrWhiteSpace(catalogue.AstuceDeLAide()));
    }

    [Fact]
    public void Le_titre_de_la_page()
    {
        Connecte();
        Assert.StartsWith("Atelier démo", Driver.Title);
    }
}
