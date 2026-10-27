namespace Bascule.Rapport;

/// <summary>Ce que l'outil a fait (ou ferait) d'un usage de Selenium.</summary>
public enum Niveau
{
    /// <summary>Converti automatiquement, équivalent attendu.</summary>
    Automatique,
    /// <summary>Converti automatiquement, mais le comportement peut différer : une relecture humaine est conseillée.</summary>
    ARelire,
    /// <summary>Non converti ou converti par un pont temporaire : une intervention est nécessaire.</summary>
    Manuel,
    /// <summary>Remarque sans action requise (suppression d'une instruction devenue inutile, par exemple).</summary>
    Information,
}

/// <summary>Une ligne du rapport.</summary>
public sealed record Constat(
    string Fichier,
    int Ligne,
    string Categorie,
    Niveau Niveau,
    string Message,
    string? Avant = null,
    string? Apres = null);

/// <summary>Catégories fixes du rapport (l'ordre est celui de l'affichage).</summary>
public static class Categories
{
    public const string Cycle = "Cycle de vie du navigateur";
    public const string Navigation = "Navigation";
    public const string Selecteurs = "Sélecteurs";
    public const string Actions = "Actions sur les éléments";
    public const string Lecture = "Lecture d'état";
    public const string Assertions = "Assertions";
    public const string Attentes = "Attentes";
    public const string Pauses = "Pauses fixes";
    public const string Listes = "Listes déroulantes";
    public const string Avance = "Clavier, souris, glisser-déposer";
    public const string JavaScript = "JavaScript";
    public const string Captures = "Captures d'écran";
    public const string Cadres = "Cadres (iframes)";
    public const string Dialogues = "Alertes et dialogues";
    public const string Fenetres = "Fenêtres et onglets";
    public const string Cookies = "Cookies et fenêtre";
    public const string PageObjects = "Page objects";
    public const string Exceptions = "Exceptions Selenium";
    public const string Asynchrone = "Passage en asynchrone";
    public const string Projet = "Projet et dépendances";
    public const string Compilation = "Compilation du résultat";

    public static readonly string[] Ordre =
    [
        Cycle, Navigation, Selecteurs, Actions, Lecture, Assertions, Attentes, Pauses, Listes, Avance,
        JavaScript, Captures, Cadres, Dialogues, Fenetres, Cookies, PageObjects, Exceptions, Asynchrone, Projet, Compilation,
    ];
}
