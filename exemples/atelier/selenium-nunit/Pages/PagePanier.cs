using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Atelier.Tests.Pages
{
    public class PagePanier
    {
        private readonly IWebDriver driver;

        public PagePanier(IWebDriver driver)
        {
            this.driver = driver;
        }

        public string Total => driver.FindElement(By.Id("total")).Text;

        public string Note => driver.FindElement(By.Id("note")).Text;

        public void ChoisirQuantite(string reference, int quantite)
        {
            var liste = new SelectElement(driver.FindElement(By.CssSelector($"select.quantite[data-ref='{reference}']")));
            liste.SelectByValue(quantite.ToString());
        }

        public string QuantiteChoisie(string reference)
        {
            var liste = new SelectElement(driver.FindElement(By.CssSelector($"select.quantite[data-ref='{reference}']")));
            return liste.SelectedOption.Text;
        }

        public void Vider()
        {
            driver.FindElement(By.Id("vider")).Click();
            IAlert alerte = driver.SwitchTo().Alert();
            alerte.Accept();
        }

        public void AjouterNote(string note)
        {
            driver.FindElement(By.Id("ajouter-note")).Click();
            IAlert alerte = driver.SwitchTo().Alert();
            alerte.SendKeys(note);
            alerte.Accept();
        }

        public void Commander() => driver.FindElement(By.Id("commander")).Click();
    }
}
