namespace Bascule.Chargement;

/// <summary>Ensemble des projets trouvés à partir d'un chemin (.sln, .slnx, .csproj ou dossier).</summary>
public sealed class Espace
{
    public required string Racine { get; init; }
    public required IReadOnlyList<ProjetDeTests> Projets { get; init; }

    public static Espace Ouvrir(string chemin)
    {
        chemin = Path.GetFullPath(chemin);
        List<string> csprojs;
        string racine;
        if (File.Exists(chemin) && chemin.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            csprojs = [chemin];
            racine = Path.GetDirectoryName(chemin)!;
        }
        else if (File.Exists(chemin) && (chemin.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || chemin.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)))
        {
            racine = Path.GetDirectoryName(chemin)!;
            csprojs = ProjetsDeSolution(chemin).Where(File.Exists).ToList();
        }
        else if (Directory.Exists(chemin))
        {
            racine = chemin;
            csprojs = TrouverCsproj(chemin).ToList();
        }
        else
        {
            throw new FileNotFoundException($"Chemin introuvable ou non pris en charge : {chemin}. Donnez un dossier, un .sln, un .slnx ou un .csproj.");
        }
        csprojs.Sort(StringComparer.Ordinal);
        return new Espace { Racine = racine, Projets = csprojs.Distinct().Select(ProjetDeTests.Lire).ToList() };
    }

    private static IEnumerable<string> TrouverCsproj(string dossier)
    {
        var pile = new Stack<string>();
        pile.Push(dossier);
        while (pile.Count > 0)
        {
            var d = pile.Pop();
            foreach (var f in Directory.EnumerateFiles(d, "*.csproj")) yield return Path.GetFullPath(f);
            foreach (var sous in Directory.EnumerateDirectories(d))
            {
                var n = Path.GetFileName(sous);
                if (n is "bin" or "obj" or "node_modules" or "packages" || n.StartsWith('.')) continue;
                pile.Push(sous);
            }
        }
    }

    private static IEnumerable<string> ProjetsDeSolution(string sln)
    {
        var dossier = Path.GetDirectoryName(sln)!;
        var texte = File.ReadAllText(sln);
        // .sln : Project("{…}") = "Nom", "chemin\Nom.csproj", "{…}" ; .slnx : <Project Path="…" />
        var motif = new System.Text.RegularExpressions.Regex(@"[""']([^""']+\.csproj)[""']");
        foreach (System.Text.RegularExpressions.Match m in motif.Matches(texte))
            yield return Path.GetFullPath(Path.Combine(dossier, m.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar)));
    }
}
