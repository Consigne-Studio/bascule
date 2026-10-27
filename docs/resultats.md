# Résultats mesurés

Ce que donne Bascule 1.0.0 sur des suites réelles. Les chiffres sont refaits à chaque version ; rien n'est retouché à la main.

## Suites de démonstration (ce dépôt)

Trois suites Selenium écrites comme on les écrit en entreprise (classe de base, page objects, attentes explicites, attente implicite, cadres, listes déroulantes, dialogues, scripts), sur un petit site statique fourni. Après `bascule migrer`, les suites migrées sont exécutées sous Playwright (Chromium).

| Suite | Tests | Conversions automatiques | À relire | À reprendre | Compilation | Tests verts sous Playwright |
|---|---|---|---|---|---|---|
| NUnit (`selenium-nunit`) | 17 | 170 | 3 | 0 | réussie | 17 / 17 |
| MSTest (`selenium-mstest`) | 8 | 68 | 0 | 0 | réussie | 8 / 8 |
| xUnit (`selenium-xunit`) | 7 | 41 | 0 | 2 | réussie | 6 / 7 |

Le test xUnit en échec est celui des deux points signalés « à reprendre » : un page object qui entre dans un cadre puis appelle une méthode utilitaire de sa classe de base. Il passe une fois les deux points repris comme l'indique le rapport (passer le `FrameLocator` à la méthode). Pour rejouer : `verifier-exemples.sh`, fourni dans le zip de l'édition Pro.

## Dépôts publics

Suites C# publiques, migrées sans aucune retouche avec `bascule migrer --compiler`. Les dépôts ne sont pas redistribués ici. Toutes les suites mesurées sont publiées, y compris les moins favorables.

| Dépôt | Tests | Part automatique | À reprendre | dont lignes qui ne compilent pas après migration | Erreurs de compilation non signalées |
|---|---|---|---|---|---|
| browserstack/nunit-browserstack | 2 | 95 % | 1 (grille distante) | 0 | 0 |
| saucelabs-training/demo-csharp, SeleniumExamples | 30 | 99 % | 6 | 2 (`SessionId`) | 0 |
| saucelabs-training/demo-csharp, Sauce.Demo | 33 | 64 % | 119 | 44 (dont 30 dans les fichiers Appium) | 0 |
| SeleniumHQ/seleniumhq.github.io, exemples .NET | 169 | 57 % | 427 | 116 | 0 |
| LambdaTest/nunit-selenium-sample | 1 | 53 % | 15 | non compilé ici (.NET Framework 4.5.2) | — |
| AutomateThePlanet Learning Series | 689 | 91 % | 364 | 250 (vérification en mémoire ; .NET Framework, format ancien) | — |

À lire avec leur contexte :

* **Le critère de confiance est la colonne de droite.** Après la réécriture, Bascule recompile en mémoire la suite d'origine et la suite migrée ; chaque erreur que la migration a fait apparaître devient un point « à reprendre » avec sa ligne. Avec `--compiler`, `dotnet build` complète la vérification avec les vrais paquets du projet (Appium, par exemple). Une erreur de compilation n'est donc jamais laissée en silence.
* Les exemples de la documentation Selenium montrent volontairement toute l'API, y compris ce qui n'a pas d'équivalent Playwright (BiDi, DevTools, grille, journaux du navigateur, fenêtres par identifiant) : c'est le cas le plus défavorable, pas une suite de tests d'application.
* Sauce.Demo mélange des tests web, des tests mobiles Appium et une grille distante : les parties mobiles et la grille restent à reprendre, comme annoncé.
* LambdaTest est un exemple de grille cloud : capacités du fournisseur (`SetCapability`) et `RemoteWebDriver`, qui n'ont pas d'équivalent direct.
* AutomateThePlanet déclare son propre type `IPage`, qui entre en conflit de nom avec celui de Playwright : ces lignes sont signalées. Ses 689 tests sont analysés, migrés et vérifiés en 35 secondes, sans plantage.

## Le diagnostic annonce-t-il la migration ?

L'édition gratuite classe les usages avec les mêmes règles de conversion, sans contenir le réécrivain ni recompiler. Elle donne donc une estimation, proche sur les suites d'application, plus optimiste sur les suites qui ne compilent pas d'emblée :

| Suite | À reprendre (diagnostic) | À reprendre (migration) | Part automatique (diagnostic / migration) |
|---|---|---|---|
| NUnit, MSTest, xUnit de démonstration | 0, 0, 2 | 0, 0, 2 | 97 / 98 %, 100 / 100 %, 93 / 95 % |
| BrowserStack | 1 | 1 | 91 / 95 % |
| SeleniumExamples | 4 | 6 | 98 / 99 % |
| Sauce.Demo | 68 | 119 | 70 / 64 % |
| Documentation Selenium | 269 | 427 | 66 / 57 % |
| LambdaTest | 14 | 15 | 42 / 53 % |
| AutomateThePlanet | 111 | 364 | 95 / 91 % |

L'écart vient presque entièrement des lignes qui ne compilent pas après la migration, que seule l'édition Pro peut voir. Sur les suites de démonstration, les deux éditions donnent les mêmes points.

## Ce que Bascule ne mesure pas

* Le temps d'exécution avant et après : il dépend de votre application et de votre infrastructure. Playwright est en général plus rapide, surtout en parallèle, mais Bascule ne le promet pas.
* La stabilité : un test instable sous Selenium peut le rester ; l'attente automatique de Playwright en corrige souvent une partie.
