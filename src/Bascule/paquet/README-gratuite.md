# Bascule

Diagnostic d'une suite de tests **Selenium C#** avant sa migration vers **Playwright .NET** : nombre de tests, part convertible automatiquement, points à relire et à reprendre avec leur emplacement exact, ordre de grandeur du travail restant. Rapport HTML autonome et JSON.

```bash
bascule diagnostic MaSuite.sln
```

Rien n'est compilé, exécuté ni envoyé : Bascule lit le code avec Roslyn. NUnit, MSTest et xUnit ; Selenium 3 et 4.

Documentation, règles de conversion et résultats mesurés : dossier `docs` du dépôt GitHub qui publie ce paquet.

L'édition Pro réécrit la suite en Playwright .NET (`bascule migrer`) : voir le README du dépôt.
