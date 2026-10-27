using NUnit.Framework;
using OpenQA.Selenium;
using Atelier.Tests.Infrastructure;
using Atelier.Tests.Pages;

namespace Atelier.Tests.Tests
{
    [TestFixture]
    public class CommandeTests : TestBase
    {
        private PagePanier PreparerPanier()
        {
            var catalogue = SeConnecter();
            catalogue.Ajouter("VAN-101");
            catalogue.Ajouter("CAP-301");
            catalogue.OuvrirPanier();
            return new PagePanier(driver);
        }

        [Test]
        public void Changer_la_quantite_met_a_jour_le_total()
        {
            var panier = PreparerPanier();
            panier.ChoisirQuantite("VAN-101", 3);
            Assert.That(panier.Total, Is.EqualTo("4"));
            Assert.That(panier.QuantiteChoisie("VAN-101"), Is.EqualTo("3"));
        }

        [Test]
        public void Vider_le_panier_demande_confirmation()
        {
            var panier = PreparerPanier();
            panier.Vider();
            Assert.That(panier.Total, Is.EqualTo("0"));
        }

        [Test]
        public void Ajouter_une_note()
        {
            var panier = PreparerPanier();
            panier.AjouterNote("Livrer avant 10 h");
            Assert.That(panier.Note, Is.EqualTo("Livrer avant 10 h"));
        }

        [Test]
        public void Passer_commande_avec_bon_et_ordre_de_livraison()
        {
            var panier = PreparerPanier();
            panier.Commander();
            var commande = new PageCommande(driver);
            commande.Remplir("Durand", "durand@example.com", "12 rue des Forges\n69000 Lyon");
            var bon = Path.Combine(Path.GetTempPath(), "bon-de-commande.pdf");
            File.WriteAllText(bon, "%PDF-1.4 démo");
            commande.JoindreBon(bon);
            Assert.That(commande.FichierChoisi, Is.EqualTo("bon-de-commande.pdf"));
            commande.DeplacerEnTete("Capteurs");
            Assert.That(commande.OrdreDeLivraison(), Is.EqualTo(new[] { "Capteurs", "Pompes", "Vannes" }));
            commande.AccepterConditions();
            commande.Valider();
            Assert.That(commande.AttendreConfirmation(), Is.EqualTo("Commande ATL-0001 enregistrée pour Durand."));
            Assert.That(commande.TitreDocument(), Is.EqualTo("Atelier démo — commande"));
        }

        [Test]
        public void Commande_sans_conditions_affiche_une_alerte()
        {
            var panier = PreparerPanier();
            panier.Commander();
            var commande = new PageCommande(driver);
            commande.Remplir("Durand", "durand@example.com", "Lyon");
            commande.Valider();
            var alerte = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.AlertIsPresent());
            Assert.That(alerte.Text, Is.EqualTo("Veuillez accepter les conditions de vente."));
            alerte.Accept();
            Assert.That(driver.FindElement(By.Id("confirmation")).Displayed, Is.False);
        }
    }
}
