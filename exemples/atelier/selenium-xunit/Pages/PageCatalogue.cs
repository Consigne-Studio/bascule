using OpenQA.Selenium;

namespace Atelier.XUnit.Pages;

public class PageCatalogue : PageBase
{
    private static readonly By Resultats = By.Id("resultats");
    private static readonly By Filtre = By.Id("filtre");

    public PageCatalogue(IWebDriver driver) : base(driver) { }

    public PageCatalogue Attendre()
    {
        AttendreVisible(By.Id("pieces"));
        return this;
    }

    public void Rechercher(string texte) => Saisir(Filtre, texte);

    public string Compteur => Lire(Resultats);

    public IReadOnlyCollection<IWebElement> Designations => Driver.FindElements(By.CssSelector("#pieces td.nom"));

    public string AstuceDeLAide()
    {
        Driver.SwitchTo().Frame(Driver.FindElement(By.Id("cadre-aide")));
        Cliquer(By.Id("astuce"));
        var texte = AttendreVisible(By.Id("texte-astuce")).Text;
        Driver.SwitchTo().DefaultContent();
        return texte;
    }
}
