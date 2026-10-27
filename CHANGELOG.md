# Journal des versions

## 1.0.0 (24 octobre 2026)

Première version publique.

* `bascule diagnostic` (édition gratuite, MIT) : inventaire d'une suite Selenium C# (NUnit, MSTest, xUnit, Reqnroll et SpecFlow comptés), classement de chaque usage de Selenium en automatique, à relire ou à reprendre, estimation du travail restant, rapport HTML autonome et JSON.
* `bascule migrer` (édition Pro) : réécriture Roslyn vers Playwright .NET 1.64 ; sélecteurs, actions, lectures, listes, `Actions`, attentes explicites (18 conditions) et implicite, cadres, dialogues, cookies, scripts, captures, assertions web ; propagation `async` / `await` ; classe de base `PageTest` ou cycle de vie conservé ; `.csproj` converti et versions minimales relevées ; `--compiler` ; rapport de migration avec le diff de chaque fichier.
* Vérification de la compilation après migration, en mémoire : chaque erreur apparue devient un point « à reprendre » ; avec `--compiler`, les erreurs de `dotnet build` rejoignent aussi le rapport.
* Mesures publiées dans [docs/resultats.md](docs/resultats.md) : 3 suites de démonstration, 6 dépôts publics.
