using OpenQA.Selenium;
using SeleniumExtras.PageObjects;

namespace Atelier.Tests.Pages
{
    public class PageConnexion
    {
        private readonly IWebDriver driver;

        [FindsBy(How = How.Id, Using = "utilisateur")]
        private IWebElement champUtilisateur;

        [FindsBy(How = How.Id, Using = "mot-de-passe")]
        private IWebElement champMotDePasse;

        [FindsBy(How = How.CssSelector, Using = "button[type=submit]")]
        private IWebElement boutonConnexion;

        [FindsBy(How = How.Id, Using = "erreur")]
        public IWebElement MessageErreur { get; set; }

        public PageConnexion(IWebDriver driver)
        {
            this.driver = driver;
            PageFactory.InitElements(driver, this);
        }

        public PageConnexion Ouvrir(string baseUrl)
        {
            driver.Navigate().GoToUrl(baseUrl + "/index.html");
            return this;
        }

        public void SeConnecter(string utilisateur, string motDePasse)
        {
            champUtilisateur.Clear();
            champUtilisateur.SendKeys(utilisateur);
            champMotDePasse.SendKeys(motDePasse);
            boutonConnexion.Click();
        }

        public void SeConnecterAvecEntree(string utilisateur, string motDePasse)
        {
            champUtilisateur.SendKeys(utilisateur);
            champMotDePasse.SendKeys(motDePasse + Keys.Enter);
        }
    }
}
