using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Atelier.Tests.Pages;

namespace Atelier.Tests.Infrastructure
{
    /// <summary>Base commune : un navigateur Chrome par test, capture d'écran en cas d'échec.</summary>
    public abstract class TestBase
    {
        protected IWebDriver driver;
        protected WebDriverWait wait;

        protected static string BaseUrl => Environment.GetEnvironmentVariable("ATELIER_URL") ?? "http://localhost:5080";

        [SetUp]
        public void OuvrirNavigateur()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless=new");
            options.AddArgument("--window-size=1280,900");
            driver = new ChromeDriver(options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
            driver.Manage().Window.Maximize();
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        [TearDown]
        public void FermerNavigateur()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                var capture = ((ITakesScreenshot)driver).GetScreenshot();
                capture.SaveAsFile(Path.Combine(TestContext.CurrentContext.WorkDirectory, TestContext.CurrentContext.Test.Name + ".png"));
            }
            driver.Quit();
        }

        protected PageCatalogue SeConnecter()
        {
            new PageConnexion(driver).Ouvrir(BaseUrl).SeConnecter("demo", "demo");
            wait.Until(ExpectedConditions.UrlContains("catalogue"));
            var catalogue = new PageCatalogue(driver);
            catalogue.AttendreChargement();
            return catalogue;
        }
    }
}
