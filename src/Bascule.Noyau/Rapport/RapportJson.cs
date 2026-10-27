using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bascule.Rapport;

public static class RapportJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string Generer(RapportBascule r)
    {
        var objet = new
        {
            r.Mode, r.Edition, r.Version, r.Date, r.Source, r.Sortie,
            Synthese = new
            {
                Tests = r.Tests, Scenarios = r.Scenarios,
                Automatique = r.Compter(Niveau.Automatique), ARelire = r.Compter(Niveau.ARelire),
                Manuel = r.Compter(Niveau.Manuel), Information = r.Compter(Niveau.Information),
                TauxAutomatique = Math.Round(r.TauxAutomatique, 3), HeuresRestantes = r.HeuresRestantes,
            },
            r.Projets, r.Compilation, r.Constats,
        };
        return JsonSerializer.Serialize(objet, Options);
    }
}
