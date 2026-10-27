using System.Diagnostics;
using System.Text.RegularExpressions;
using Bascule.Rapport;

namespace Bascule.Cli;

/// <summary>Lance « dotnet build » sur le résultat de la migration et compte les erreurs.</summary>
internal static class Compilation
{
    public static ResultatCompilation Lancer(string dossier) => Lancer(dossier, out _);

    /// <param name="erreurs">Toutes les erreurs, relatives au dossier : (fichier, ligne, texte).</param>
    public static ResultatCompilation Lancer(string dossier, out List<(string Fichier, int Ligne, string Texte)> toutes)
    {
        toutes = [];
        var cible = Directory.EnumerateFiles(dossier, "*.sln").Concat(Directory.EnumerateFiles(dossier, "*.slnx")).FirstOrDefault()
                    ?? Directory.EnumerateFiles(dossier, "*.csproj", SearchOption.AllDirectories).FirstOrDefault();
        if (cible is null) return new ResultatCompilation(false, 1, 0, ["Aucun projet à compiler."]);
        var psi = new ProcessStartInfo("dotnet", ["build", cible, "-nologo", "-clp:NoSummary"])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = dossier,
        };
        using var p = Process.Start(psi)!;
        var sortie = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        var erreurs = Regex.Matches(sortie, @"^.*: error [A-Z]+\d+:.*$", RegexOptions.Multiline).Select(m => m.Value.Trim()).Distinct().ToList();
        var avert = Regex.Matches(sortie, @": warning [A-Z]+\d+:", RegexOptions.Multiline).Count;
        foreach (var e in erreurs)
        {
            var m = Regex.Match(e, @"^\s*(?<f>.+?)\((?<l>\d+),\d+\): error (?<t>[A-Z]+\d+:.*?)(\s*\[[^\]]+\.csproj\])?$");
            if (m.Success)
                toutes.Add((Path.GetRelativePath(dossier, Path.GetFullPath(m.Groups["f"].Value, dossier)).Replace('\\', '/'), int.Parse(m.Groups["l"].Value), m.Groups["t"].Value.Trim()));
        }
        var premieres = erreurs.Select(e => Regex.Replace(e, @"\s*\[[^\]]+\.csproj\]$", "")).Select(e => e.Replace(dossier + Path.DirectorySeparatorChar, "")).Take(20).ToList();
        return new ResultatCompilation(p.ExitCode == 0, erreurs.Count, avert, premieres);
    }
}
