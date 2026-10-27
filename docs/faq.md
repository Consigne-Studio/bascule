# Questions fréquentes

## Avant de commencer

**Faut-il que la suite compile ?** Non. Bascule lit le code avec Roslyn et embarque les métadonnées de l'API Selenium : il n'a besoin ni des paquets NuGet de la suite, ni d'une restauration, ni d'un `dotnet build`. Une suite cassée est analysée comme une autre ; ses erreurs propres restent les siennes.

**Mon code part-il quelque part ?** Non. Bascule ne fait aucun appel réseau. Le rapport est un fichier HTML autonome (polices comprises), le JSON reste sur votre poste.

**Quelle version de .NET faut-il ?** Le SDK .NET 8 ou plus récent pour lancer l'outil. La suite analysée peut cibler .NET Core 3.1 à .NET 10, ou .NET Framework 4.6.2 et plus (Playwright .NET prend en charge les deux).

**Et une suite en Java, Python ou TypeScript ?** Bascule ne traite que le C#. Pour le reste, Playwright fournit lui-même des outils et des guides.

## Pendant la migration (édition Pro)

**Mes fichiers d'origine sont-ils modifiés ?** Pas par défaut : `bascule migrer` écrit une copie à côté (`MaSuite-playwright`), ou dans le dossier donné par `--sortie`. `--en-place` modifie les fichiers d'origine ; réservez-le à un dépôt Git propre, pour relire le diff dans votre outil habituel.

**Le résultat compile-t-il ?** Sur les suites d'exemple et sur les dépôts publics mesurés, chaque erreur de compilation restante tombe dans un fichier où un point « à reprendre » est signalé. `--compiler` lance `dotnet build` sur le résultat et inscrit les erreurs dans le rapport. Tant que des points restent à reprendre, Selenium reste référencé : les deux bibliothèques cohabitent le temps de la transition.

**Les navigateurs ?** Bascule n'en installe aucun. Après la migration, une fois le projet compilé : `pwsh bin/Debug/net8.0/playwright.ps1 install` (ou `chromium` seul). Voir la documentation de Playwright .NET.

**Pourquoi mes méthodes deviennent-elles `async Task` ?** Playwright est asynchrone. Bascule propage `async` / `await` de proche en proche, des tests aux page objects et aux utilitaires, et adapte les appels. Une méthode passée en délégué (groupe de méthodes, événement) reste synchrone avec toute sa famille, et ses usages sont signalés.

**Pourquoi `SendKeys` devient-il `FillAsync` ?** C'est l'usage presque universel : saisir une valeur dans un champ vide. `FillAsync` remplace le contenu, là où `SendKeys` ajoutait à la fin. Si un test compte sur l'ajout (saisie en plusieurs fois), remplacez par `PressSequentiallyAsync`.

**Et l'attente implicite ?** Playwright attend déjà avant chaque action. Bascule reprend votre délai d'attente implicite là où Selenium l'appliquait et Playwright non (nombre d'éléments, liste d'éléments, visibilité), pour que les tests gardent leur comportement.

**Où sont les points à reprendre ?** Dans le rapport, section « À reprendre » : fichier, ligne, code concerné et équivalent Playwright à écrire, avec un champ de filtre. Le code concerné est laissé tel quel (Selenium), pour que rien ne change en silence.

**Puis-je relancer la migration après une correction de ma suite Selenium ?** Oui, sur une copie fraîche : la migration part toujours du code Selenium. Ne relancez pas sur un résultat déjà migré.

**Les tests Reqnroll ou SpecFlow ?** Les définitions d'étapes sont du C# ordinaire et sont migrées comme le reste ; les fichiers `.feature` ne changent pas et leurs scénarios sont comptés. Les hooks qui créent le navigateur (`[BeforeScenario]`, injection du pilote) sont propres à chaque suite et souvent signalés à reprendre. Aucune suite Reqnroll publique n’a encore été mesurée : lancez d’abord le diagnostic.

## Licence et achat

**Combien de postes avec une licence Pro ?** Illimités, pour une organisation (une entreprise, ses filiales comprises). Un prestataire qui migre la suite d'un client achète une licence par client.

**L'outil continue-t-il de fonctionner après 12 mois ?** Oui. La licence est perpétuelle pour la version achetée ; les 12 mois portent sur les mises à jour.

**Puis-je essayer avant d'acheter ?** Le diagnostic gratuit applique les mêmes règles de conversion que l'édition Pro : il estime ce que la migration convertira, ce qu'il faudra relire et reprendre, et combien de temps cela prendra. La migration, qui recompile la suite, trouve en général quelques points de plus. Si l'édition Pro ne fonctionne pas comme décrit, elle est remboursée sous 30 jours.

**Facture au nom de la société ?** Gumroad vend en qualité de revendeur : son reçu, à la raison sociale et au numéro de TVA renseignés au paiement, tient lieu de facture. Sur bon de commande, la facture est émise directement.

## Contact

support@consignestudio.fr
