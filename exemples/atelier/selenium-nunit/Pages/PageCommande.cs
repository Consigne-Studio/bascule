using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Atelier.Tests.Pages
{
    public class PageCommande
    {
        private readonly IWebDriver driver;
        private readonly WebDriverWait wait;

        public PageCommande(IWebDriver driver)
        {
            this.driver = driver;
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        }

        public void Remplir(string nom, string courriel, string adresse)
        {
            driver.FindElement(By.Id("nom")).SendKeys(nom);
            driver.FindElement(By.Id("courriel")).SendKeys(courriel);
            driver.FindElement(By.Name("adresse")).SendKeys(adresse);
        }

        public void JoindreBon(string chemin)
        {
            driver.FindElement(By.Id("bon")).SendKeys(chemin);
        }

        public string FichierChoisi => driver.FindElement(By.Id("fichier-choisi")).Text;

        public void AccepterConditions()
        {
            var caseCgv = driver.FindElement(By.Id("cgv"));
            if (!caseCgv.Selected)
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", caseCgv);
                caseCgv.Click();
            }
        }

        public void Valider() => driver.FindElement(By.Id("valider")).Click();

        public string AttendreConfirmation()
        {
            var confirmation = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("confirmation")));
            return confirmation.Text;
        }

        public void DeplacerEnTete(string libelle)
        {
            var source = driver.FindElement(By.XPath($"//ul[@id='priorites']/li[text()='{libelle}']"));
            var cible = driver.FindElements(By.CssSelector("#priorites li"))[0];
            new Actions(driver).ClickAndHold(source).MoveToElement(cible).Release().Perform();
        }

        public IList<string> OrdreDeLivraison()
        {
            var resultat = new List<string>();
            foreach (var element in driver.FindElements(By.CssSelector("#priorites li")))
            {
                resultat.Add(element.Text);
            }
            return resultat;
        }

        public string TitreDocument() => (string)((IJavaScriptExecutor)driver).ExecuteScript("return document.title;");
    }
}
