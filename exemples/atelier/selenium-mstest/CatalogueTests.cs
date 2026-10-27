using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace Atelier.MSTest;

/// <summary>Style « tout dans le test » : pilote créé dans TestInitialize, pas de page objects.</summary>
[TestClass]
public class CatalogueTests
{
    private IWebDriver _driver = null!;
    private WebDriverWait _wait = null!;
    private static readonly string BaseUrl = Environment.GetEnvironmentVariable("ATELIER_URL") ?? "http://localhost:5080";

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void Demarrer()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        _driver = new ChromeDriver(options);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        _driver.Navigate().GoToUrl(BaseUrl + "/index.html");
        _driver.FindElement(By.Id("utilisateur")).SendKeys("demo");
        _driver.FindElement(By.Id("mot-de-passe")).SendKeys("demo");
        _driver.FindElement(By.CssSelector("button[type=submit]")).Click();
        _wait.Until(d => d.Url.Contains("catalogue"));
        _wait.Until(d => d.FindElements(By.CssSelector("#pieces tbody tr")).Count > 0);
    }

    [TestCleanup]
    public void Arreter() => _driver.Quit();

    [TestMethod]
    public void Le_catalogue_liste_huit_pieces()
    {
        var lignes = _driver.FindElements(By.CssSelector("#pieces tbody tr"));
        Assert.AreEqual(8, lignes.Count);
        Assert.AreEqual("8 pièce(s)", _driver.FindElement(By.Id("resultats")).Text);
    }

    [TestMethod]
    public void La_premiere_ligne_est_la_vanne_a_boisseau()
    {
        var premiere = _driver.FindElement(By.CssSelector("#pieces tbody tr"));
        Assert.AreEqual("VAN-101", premiere.FindElement(By.CssSelector("td.ref")).Text);
        Assert.AreEqual("VAN-101", premiere.GetAttribute("data-ref"));
        StringAssert.Contains(premiere.FindElement(By.CssSelector("td.prix")).Text, "84,00");
    }

    [TestMethod]
    public void Filtrer_par_categorie_capteurs()
    {
        new SelectElement(_driver.FindElement(By.Id("categorie"))).SelectByValue("capteurs");
        var refs = _driver.FindElements(By.CssSelector("#pieces td.ref")).Select(e => e.Text).ToList();
        CollectionAssert.AreEqual(new[] { "CAP-301", "CAP-302", "CAP-303" }, refs);
    }

    [TestMethod]
    public void Le_bouton_ajouter_change_de_libelle()
    {
        var bouton = _driver.FindElement(By.CssSelector("button.ajouter[data-ref='POM-201']"));
        Assert.IsTrue(bouton.Enabled);
        bouton.Click();
        Assert.AreEqual("Ajouté", bouton.Text);
        Assert.AreEqual("1", _driver.FindElement(By.Id("panier-compte")).Text);
    }

    [TestMethod]
    public void Le_panier_survit_au_rechargement()
    {
        _driver.FindElement(By.CssSelector("button.ajouter[data-ref='CAP-302']")).Click();
        _driver.Navigate().Refresh();
        _wait.Until(d => d.FindElement(By.Id("panier-compte")).Text == "1");
        Assert.AreEqual("1", _driver.FindElement(By.Id("panier-compte")).Text);
    }

    [TestMethod]
    public void Le_cookie_de_session_est_pose()
    {
        var session = _driver.Manage().Cookies.GetCookieNamed("session");
        Assert.IsNotNull(session);
        Assert.AreEqual("demo", session.Value);
    }

    [TestMethod]
    public void Lire_le_nombre_de_pieces_par_javascript()
    {
        var js = (IJavaScriptExecutor)_driver;
        var nombre = js.ExecuteScript("return document.querySelectorAll('#pieces tbody tr').length;");
        Assert.AreEqual(8L, nombre);
    }

    [TestMethod]
    public void Retour_arriere_depuis_le_panier()
    {
        _driver.FindElement(By.Id("voir-panier")).Click();
        _wait.Until(d => d.Url.EndsWith("panier.html"));
        _driver.Navigate().Back();
        _wait.Until(d => d.Title.Contains("catalogue"));
        Assert.IsTrue(_driver.Url.Contains("catalogue"));
    }
}
