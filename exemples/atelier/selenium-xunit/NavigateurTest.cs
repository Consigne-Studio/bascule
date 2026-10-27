using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Atelier.XUnit;

/// <summary>Style xUnit : le constructeur ouvre le navigateur, Dispose le ferme.</summary>
public abstract class NavigateurTest : IDisposable
{
    protected readonly IWebDriver Driver;
    protected static readonly string BaseUrl = Environment.GetEnvironmentVariable("ATELIER_URL") ?? "http://localhost:5080";

    protected NavigateurTest()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        Driver = new ChromeDriver(options);
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
    }

    public void Dispose()
    {
        Driver.Quit();
        GC.SuppressFinalize(this);
    }
}
