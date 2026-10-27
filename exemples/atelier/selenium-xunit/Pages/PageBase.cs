using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Atelier.XUnit.Pages;

/// <summary>Assistants communs aux pages, qui prennent des sélecteurs By (motif fréquent des suites réelles).</summary>
public abstract class PageBase
{
    protected readonly IWebDriver Driver;
    private readonly WebDriverWait _attente;

    protected PageBase(IWebDriver driver)
    {
        Driver = driver;
        _attente = new WebDriverWait(driver, TimeSpan.FromSeconds(8));
    }

    protected void Cliquer(By cible) => Driver.FindElement(cible).Click();

    protected void Saisir(By cible, string texte)
    {
        var champ = Driver.FindElement(cible);
        champ.Clear();
        champ.SendKeys(texte);
    }

    protected string Lire(By cible) => Driver.FindElement(cible).Text;

    protected IWebElement AttendreVisible(By cible) => _attente.Until(ExpectedConditions.ElementIsVisible(cible));

    protected bool EstAffiche(By cible) => Driver.FindElements(cible).Count > 0 && Driver.FindElement(cible).Displayed;
}
