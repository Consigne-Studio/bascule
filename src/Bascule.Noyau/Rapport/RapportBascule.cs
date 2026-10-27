using System.Text.Json.Serialization;

namespace Bascule.Rapport;

/// <summary>Résumé d'un projet de tests.</summary>
public sealed class RapportProjet
{
    public required string Nom { get; init; }
    public required string Chemin { get; init; }
    public required string Cadre { get; init; }
    public int Fichiers { get; set; }
    public int FichiersSelenium { get; set; }
    public int Tests { get; set; }
    public int Scenarios { get; set; }
    public bool AncienFormat { get; set; }
    public List<string> Dependances { get; } = [];
    public List<string> FichiersAjoutes { get; } = [];
}

/// <summary>Résultat d'un « dotnet build » du projet migré, quand il a été demandé.</summary>
public sealed record ResultatCompilation(bool Reussi, int Erreurs, int Avertissements, IReadOnlyList<string> PremieresErreurs);

/// <summary>Rapport complet d'un diagnostic ou d'une migration.</summary>
public sealed class RapportBascule
{
    public required string Mode { get; init; }            // "diagnostic" ou "migration"
    public required string Edition { get; init; }         // "gratuite" ou "pro"
    public required string Version { get; init; }
    public required DateTimeOffset Date { get; init; }
    public required string Source { get; init; }
    public string? Sortie { get; set; }
    public List<RapportProjet> Projets { get; } = [];
    public List<Constat> Constats { get; } = [];
    public ResultatCompilation? Compilation { get; set; }

    [JsonIgnore] public int Tests => Projets.Sum(p => p.Tests);
    [JsonIgnore] public int Scenarios => Projets.Sum(p => p.Scenarios);
    public int Compter(Niveau n) => Constats.Count(c => c.Niveau == n);

    /// <summary>Part des usages de Selenium convertis sans intervention (les informations ne comptent pas).</summary>
    public double TauxAutomatique
    {
        get
        {
            var auto = Compter(Niveau.Automatique);
            var total = auto + Compter(Niveau.ARelire) + Compter(Niveau.Manuel);
            return total == 0 ? 1 : (double)auto / total;
        }
    }

    /// <summary>Ordre de grandeur du travail humain restant, en heures : 20 min par point manuel distinct, 5 min quand le même point
    /// se répète dans le même fichier (un bloc de SetCapability, par exemple), 4 min par point à relire,
    /// 1 min par test pour l'exécuter et vérifier le résultat. Ce n'est pas un devis.</summary>
    public double HeuresRestantes => Math.Round((MinutesManuelles() + Compter(Niveau.ARelire) * 4 + (Tests + Scenarios) * 1) / 60.0, 1);

    private int MinutesManuelles()
    {
        var points = Constats.Where(c => c.Niveau == Niveau.Manuel).ToList();
        var distincts = points.Select(c => (c.Fichier, c.Categorie, c.Message)).Distinct().Count();
        return distincts * 20 + (points.Count - distincts) * 5;
    }
}
