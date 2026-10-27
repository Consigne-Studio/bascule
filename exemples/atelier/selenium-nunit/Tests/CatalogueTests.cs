using NUnit.Framework;
using OpenQA.Selenium;
using Atelier.Tests.Infrastructure;

namespace Atelier.Tests.Tests
{
    [TestFixture]
    public class CatalogueTests : TestBase
    {
        [Test]
        public void Le_catalogue_affiche_huit_pieces()
        {
            var catalogue = SeConnecter();
            Assert.AreEqual(8, catalogue.NombreDeLignes());
            Assert.That(catalogue.Resultats.Text, Is.EqualTo("8 pièce(s)"));
        }

        [Test]
        public void Filtrer_par_reference()
        {
            var catalogue = SeConnecter();
            catalogue.Filtrer("VAN");
            Thread.Sleep(500);
            CollectionAssert.AreEqual(new[] { "VAN-101", "VAN-102", "VAN-103" }, catalogue.References());
        }

        [Test]
        public void Filtrer_par_categorie()
        {
            var catalogue = SeConnecter();
            catalogue.ChoisirCategorie("Capteurs");
            Assert.That(catalogue.NombreDeLignes(), Is.EqualTo(3));
            Assert.That(catalogue.References(), Has.All.StartsWith("CAP-"));
        }

        [Test]
        public void Ajouter_au_panier_incremente_le_compteur()
        {
            var catalogue = SeConnecter();
            catalogue.Ajouter("POM-201");
            catalogue.Ajouter("CAP-302");
            Assert.That(catalogue.CompteurPanier, Is.EqualTo("2"));
            var bouton = driver.FindElement(By.CssSelector("button.ajouter[data-ref='POM-201']"));
            Assert.That(bouton.Text, Is.EqualTo("Ajouté"));
        }

        [Test]
        public void Bandeau_cookies_disparait_apres_acceptation()
        {
            var catalogue = SeConnecter();
            Assert.That(catalogue.BandeauCookiesVisible(), Is.True);
            catalogue.AccepterCookies();
            Assert.That(catalogue.BandeauCookiesVisible(), Is.False);
            Assert.That(driver.Manage().Cookies.GetCookieNamed("bandeau").Value, Is.EqualTo("vu"));
        }

        [Test]
        public void L_aide_en_ligne_donne_une_astuce()
        {
            var catalogue = SeConnecter();
            var astuce = catalogue.LireAstuce();
            StringAssert.StartsWith("Filtrez par référence", astuce);
            Assert.IsTrue(driver.FindElement(By.Id("pieces")).Displayed, "Le tableau doit rester visible après la lecture de l'aide.");
        }
    }
}
