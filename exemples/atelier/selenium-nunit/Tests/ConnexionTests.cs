using NUnit.Framework;
using OpenQA.Selenium;
using Atelier.Tests.Infrastructure;
using Atelier.Tests.Pages;

namespace Atelier.Tests.Tests
{
    [TestFixture]
    public class ConnexionTests : TestBase
    {
        [Test]
        public void Connexion_valide_ouvre_le_catalogue()
        {
            new PageConnexion(driver).Ouvrir(BaseUrl).SeConnecter("demo", "demo");
            wait.Until(d => d.Url.Contains("catalogue"));
            Assert.That(driver.Title, Is.EqualTo("Atelier démo — catalogue"));
        }

        [Test]
        public void Connexion_avec_la_touche_entree()
        {
            new PageConnexion(driver).Ouvrir(BaseUrl).SeConnecterAvecEntree("demo", "demo");
            wait.Until(d => d.FindElement(By.Id("panier-compte")).Displayed);
            StringAssert.Contains("catalogue", driver.Url);
        }

        [Test]
        public void Mot_de_passe_faux_affiche_une_erreur()
        {
            var page = new PageConnexion(driver).Ouvrir(BaseUrl);
            page.SeConnecter("demo", "faux");
            Assert.That(page.MessageErreur.Displayed, Is.True);
            Assert.AreEqual("Identifiant ou mot de passe incorrect.", page.MessageErreur.Text);
        }

        [TestCase("", "demo")]
        [TestCase("demo", "")]
        public void Champs_vides_refuses(string utilisateur, string motDePasse)
        {
            var page = new PageConnexion(driver).Ouvrir(BaseUrl);
            page.SeConnecter(utilisateur, motDePasse);
            Assert.IsTrue(page.MessageErreur.Displayed);
            Assert.That(driver.Url, Does.Contain("index.html"));
        }

        [Test]
        public void Deconnexion_revient_a_l_accueil()
        {
            var catalogue = SeConnecter();
            catalogue.SeDeconnecter();
            wait.Until(d => d.FindElement(By.Id("connexion")).Displayed);
            Assert.That(driver.FindElement(By.TagName("h2")).Text, Is.EqualTo("Connexion"));
        }
    }
}
