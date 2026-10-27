using Microsoft.CodeAnalysis;

namespace Bascule.Selenium;

/// <summary>Nature d'une valeur Selenium dans le code analysé.</summary>
public enum NatureSelenium
{
    Aucune,
    Pilote,          // IWebDriver, ChromeDriver, RemoteWebDriver…
    Element,         // IWebElement
    Elements,        // collection d'IWebElement
    Selecteur,       // By
    Attente,         // WebDriverWait, DefaultWait<IWebDriver>
    Liste,           // SelectElement
    Actions,         // Actions
    ExecuteurJs,     // IJavaScriptExecutor
    Capture,         // ITakesScreenshot
    Alerte,          // IAlert
    OptionsNavigateur, // ChromeOptions, FirefoxOptions, DriverService…
    Autre,           // autre type de l'espace OpenQA.Selenium
}

/// <summary>Reconnaissance des symboles Selenium à partir du modèle sémantique.</summary>
public static class ApiSelenium
{
    public static bool EstEspaceSelenium(INamespaceSymbol? ns)
    {
        var nom = ns?.ToDisplayString() ?? "";
        return nom == "OpenQA.Selenium" || nom.StartsWith("OpenQA.Selenium.", StringComparison.Ordinal)
               || nom.StartsWith("SeleniumExtras", StringComparison.Ordinal);
    }

    public static bool EstSelenium(ISymbol? s) => s is not null && EstEspaceSelenium(s.ContainingNamespace);

    public static NatureSelenium Nature(ITypeSymbol? type)
    {
        if (type is null or IErrorTypeSymbol) return NatureSelenium.Aucune;
        if (type is INamedTypeSymbol { IsGenericType: true } g && g.TypeArguments.Length == 1
            && Nature(g.TypeArguments[0]) == NatureSelenium.Element
            && (g.AllInterfaces.Any(i => i.Name == "IEnumerable") || g.Name == "IEnumerable"))
            return NatureSelenium.Elements;
        if (type is IArrayTypeSymbol a && Nature(a.ElementType) == NatureSelenium.Element) return NatureSelenium.Elements;
        if (!EstEspaceSelenium(type.ContainingNamespace))
        {
            // Types du projet qui héritent d'un type Selenium (pilote personnalisé, élément enveloppé…).
            for (var b = type.BaseType; b is not null; b = b.BaseType)
                if (EstEspaceSelenium(b.ContainingNamespace)) return Nature(b);
            return NatureSelenium.Aucune;
        }
        var nom = type.Name;
        if (nom is "IWebDriver" or "WebDriver" or "RemoteWebDriver" or "ChromeDriver" or "FirefoxDriver" or "EdgeDriver"
            or "SafariDriver" or "InternetExplorerDriver" or "ChromiumDriver" or "ISearchContext")
            return NatureSelenium.Pilote;
        if (nom is "IWebElement" or "WebElement") return NatureSelenium.Element;
        if (nom is "By") return NatureSelenium.Selecteur;
        if (nom is "WebDriverWait" or "DefaultWait") return NatureSelenium.Attente;
        if (nom is "SelectElement") return NatureSelenium.Liste;
        if (nom is "Actions") return NatureSelenium.Actions;
        if (nom is "IJavaScriptExecutor") return NatureSelenium.ExecuteurJs;
        if (nom is "ITakesScreenshot") return NatureSelenium.Capture;
        if (nom is "IAlert") return NatureSelenium.Alerte;
        if (nom.EndsWith("Options", StringComparison.Ordinal) || nom.EndsWith("DriverService", StringComparison.Ordinal))
            return NatureSelenium.OptionsNavigateur;
        return NatureSelenium.Autre;
    }

    /// <summary>Nature de la valeur renvoyée par wait.Until(ExpectedConditions.X(…)).</summary>
    public static NatureSelenium NatureCondition(string nom) => nom switch
    {
        "ElementIsVisible" or "ElementExists" or "ElementToBeClickable" => NatureSelenium.Element,
        "VisibilityOfAllElementsLocatedBy" or "PresenceOfAllElementsLocatedBy" => NatureSelenium.Elements,
        "AlertIsPresent" => NatureSelenium.Alerte,
        _ => NatureSelenium.Aucune,
    };

    /// <summary>Nom du navigateur Playwright pour un type de pilote Selenium.</summary>
    public static string? NavigateurPour(ITypeSymbol? type) => type?.Name switch
    {
        "ChromeDriver" or "ChromiumDriver" => "chromium",
        "EdgeDriver" => "msedge",
        "FirefoxDriver" => "firefox",
        "SafariDriver" => "webkit",
        "InternetExplorerDriver" => "chromium",
        "RemoteWebDriver" => "distant",
        _ => null,
    };
}
