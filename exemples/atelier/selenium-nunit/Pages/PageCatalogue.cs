using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Atelier.Tests.Pages
{
    public class PageCatalogue
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        private readonly By _filtre = By.Id("filtre");
        private readonly By _lignes = By.CssSelector("#pieces tbody tr");
        private readonly By _compteurPanier = By.Id("panier-compte");

        public PageCatalogue(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        public void AttendreChargement()
        {
            _wait.Until(ExpectedConditions.InvisibilityOfElementLocated(By.Id("chargement")));
        }

        public void Filtrer(string texte)
        {
            var champ = _driver.FindElement(_filtre);
            champ.Clear();
            champ.SendKeys(texte);
        }

        public void ChoisirCategorie(string libelle)
        {
            new SelectElement(_driver.FindElement(By.Id("categorie"))).SelectByText(libelle);
        }

        public int NombreDeLignes() => _driver.FindElements(_lignes).Count;

        public IList<string> References()
        {
            return _driver.FindElements(By.CssSelector("#pieces td.ref")).Select(e => e.Text).ToList();
        }

        public void Ajouter(string reference)
        {
            _driver.FindElement(By.CssSelector($"button.ajouter[data-ref='{reference}']")).Click();
        }

        public string CompteurPanier => _driver.FindElement(_compteurPanier).Text;

        public IWebElement Resultats => _driver.FindElement(By.Id("resultats"));

        public void OuvrirPanier() => _driver.FindElement(By.LinkText("Voir le panier")).Click();

        public void SeDeconnecter()
        {
            var menu = _driver.FindElement(By.Id("menu-compte"));
            new Actions(_driver).MoveToElement(menu).Perform();
            _driver.FindElement(By.Id("deconnexion")).Click();
            _driver.SwitchTo().Alert().Accept();
        }

        public bool BandeauCookiesVisible()
        {
            try
            {
                return _driver.FindElement(By.Id("bandeau-cookies")).Displayed;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        public void AccepterCookies() => _driver.FindElement(By.Id("accepter-cookies")).Click();

        public string LireAstuce()
        {
            _driver.SwitchTo().Frame("aide");
            _driver.FindElement(By.Id("astuce")).Click();
            var texte = _driver.FindElement(By.Id("texte-astuce")).Text;
            _driver.SwitchTo().DefaultContent();
            return texte;
        }
    }
}
