using OpenQA.Selenium;

namespace Atelier.XUnit.Pages;

public class PageAccueil : PageBase
{
    private static readonly By Utilisateur = By.Id("utilisateur");
    private static readonly By MotDePasse = By.Id("mot-de-passe");
    private static readonly By Envoyer = By.CssSelector("#connexion button[type='submit']");
    private static readonly By Erreur = By.Id("erreur");

    public PageAccueil(IWebDriver driver) : base(driver) { }

    public PageAccueil Ouvrir(string baseUrl)
    {
        Driver.Navigate().GoToUrl($"{baseUrl}/index.html");
        return this;
    }

    public void SeConnecter(string utilisateur, string motDePasse)
    {
        Saisir(Utilisateur, utilisateur);
        Saisir(MotDePasse, motDePasse);
        Cliquer(Envoyer);
    }

    public bool ErreurAffichee => EstAffiche(Erreur);

    public string TexteErreur => Lire(Erreur);
}
