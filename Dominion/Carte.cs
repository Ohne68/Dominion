﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Dominion
{
    public class Carte : ICloneable
    {
        public string Nom;
        public string Image;
        public int Cout;
        public string Type;
        public string EffetText;
        public int MonnaieDonnee;
        public int CarteDonnee;
        public int ActionDonnee;
        public int AchatDonne;
        public int JetonPointDonne;
        public int PointDonne;
        public bool EnJeu = false;
        public PictureBox PictureBox;


        //Cette variable sera affectée au début de chaque attaque, permettant d'être utilisée lors des réactions
        public static Joueur joueurSousAttaque = new Joueur();
        //On déclare le délégué sur lequel sera fixé la méthode d'attaque
        delegate void Attaque(Joueur cible);
        Attaque attaque;
        //Ce booléen passera à faux si l'adversaire annule l'attaque avec Douves par exemple
        public static bool continuerAttaque = true;


        public Carte() { }

        public Carte
            (string Nom, string Image, int Cout, string Type, string EffetText, int MonnaieDonnee
            , int CarteDonnee, int ActionDonnee, int AchatDonne, int JetonPointDonne, int PointDonne)
        {
            this.Nom = Nom;
            this.Image = Image;
            this.Cout = Cout;
            this.Type = Type;
            this.EffetText = EffetText;
            this.MonnaieDonnee = MonnaieDonnee;
            this.CarteDonnee = CarteDonnee;
            this.ActionDonnee = ActionDonnee;
            this.AchatDonne = AchatDonne;
            this.JetonPointDonne = JetonPointDonne;
            this.PointDonne = PointDonne;

            //La malédiction n'est pas compris dans les piles pour le moment.
            //Pour ne pas faire bugger la fonction Hover, on affecte une PictureBox 
            if (Nom == "Malédiction")
            {
                this.PictureBox = new PictureBox();
                this.PictureBox.ImageLocation = Image;
            }
        }

        public void MAJpb()
        {
            this.PictureBox.ImageLocation = this.Image;
            this.PictureBox.Anchor = (this.EnJeu) ? AnchorStyles.Bottom : AnchorStyles.Top; ;
            this.PictureBox.Visible = true;
            this.PictureBox.Enabled = true;
        }

        public void Effet()
        {
            Joueur JoueurActuel = PartieForm.JoueurActuel;
            List<Joueur> ListeJoueurs = LancementForm.ListeJoueurs;

            //On gère d'abord les cas d'attaque
            if (this.Type.Contains("Attaque"))
            {
                //On commence par lancer un switch des différentes attaques pour donner à notre délégué la fonction correspondante

                switch (this.Nom)
                {
                    //On va affecter à notre delegate Attaque la fonction correspondant à chaque attaque
                    //Puisque celui-ci sera ensuite appelé dans une boucle pour chaque joueur, on considère l'action pour un seul joueur
                    case "Noble brigand":
                        {
                            attaque = delegate (Joueur cible)
                       {
                           //Le joueur doit révéler les deux premières cartes de son deck

                           List<Carte> liste = cible.Devoiler(2);
                           //Si aucune de ces deux cartes n'est un trésor, la cible reçoit un cuivre
                           if (liste.Find(x => x.Type.Contains("Trésor")) is null)
                           { cible.Recevoir(PartieForm.mapListe.Find(x => x.carte.Nom == "Cuivre").carte); }
                           //Si une de ces deux cartes est un argent ou un or, l'attaquant lui vole
                           else if ((liste.FindAll(x => (x.Nom == "Argent") || (x.Nom == "Or"))).Count > 0)
                           {
                               List<Carte> choix = liste.FindAll(x => (x.Nom == "Argent") || (x.Nom == "Or"));
                               Carte carteChoisie = new Carte();
                               //Si les deux cartes sont soit un or soit argent, on en fait choisir une à l'attaquant
                               if (choix.Count > 1)
                               { carteChoisie = JoueurActuel.ChoisirUneCarte("Recevoir", choix, true); }
                               //Sinon, on applique le traitement directement
                               else
                               { carteChoisie = choix[0]; }
                               JoueurActuel.Defausse.Add(carteChoisie);
                               JoueurActuel.MAJInfos();
                               liste.Remove(carteChoisie);
                           }
                           //Dans tous les cas, la cible défausse ce qu'il a pioché, que l'attaquant lui ait volé une carte ou non
                           cible.Defausse.AddRange(liste);
                       };
                        }
                        break;

                    case "Tortionnaire":
                        {
                            attaque = delegate (Joueur cible)
                          {
                              //Le joueur va devoir choisir entre défausser 2 cartes ou recevoir une malédiction dans la main

                              ChoixForm.possibiliteChoisie = cible.ChoisirUnePossibilite("Défausser 2 cartes", "Recevoir une malédiction");
                              //Et on lance l'effet selon le choix du joueur
                              if (ChoixForm.possibiliteChoisie == "Défausser 2 cartes")
                              {
                                  List<Carte> aDefausser = cible.ChoisirDesCartes("Défausser", cible.Main, 2, true);
                                  cible.Defausser(aDefausser[0]);
                                  cible.Defausser(aDefausser[1]);
                              }
                              else
                              { cible.Recevoir(PartieForm.Malediction, "En main"); }
                          };
                        }
                        break;

                    case "Sorcière":
                        {
                            attaque = delegate (Joueur cible)
                            {
                                //Le joueur reçoit une malédiction

                                cible.Recevoir(PartieForm.Malediction);
                            };
                        }
                        break;
                }

                foreach (Joueur joueur in ListeJoueurs.FindAll(x => x != JoueurActuel))
                {
                    joueurSousAttaque = joueur;
                    //On réinitialise les booléens pour chaque joueur
                    continuerAttaque = true;
                    //Si la cible de l'attaque a une carte réaction (autre que l'Or des fous, qui ne réagit qu'à l'achat d'une province), il peut l'activer
                    if (!(joueur.Main.Find(x => (x.Type.Contains("Réaction")) && (x.Nom != "Or des fous")) is null))
                    {
                        Carte reaction = joueur.ChoisirUneCarte("Réaction", joueur.Main, false);
                        //Si le joueur choisit une carte, on lance l'effet
                        if (!(reaction is null))
                        { reaction.Reagir(); }
                    }
                    if (continuerAttaque)
                    { attaque(joueur); }
                }
            }
            else
            {
                //Puis on code les effets
                switch (this.Nom)
                {
                    case "Mascarade":
                        {
                            //Chaque joueur doit passer une carte de sa main au joueur suivant, puis le joueur ayant joué la carte peut écarter une carte

                            List<Carte> carteEcartees = new List<Carte>();
                            //On fait donc une boucle de tous les joueurs, et on va choisir la carte, l'écarter de la main puis l'ajouter dans celle du joueur suivant
                            for (int i = 0, c = ListeJoueurs.Count; i < c; i++)
                            {
                                //On utilise la méthode pour choisir une carte dans la main
                                carteEcartees.Add(ListeJoueurs[i].ChoisirUneCarte("Ecarter", ListeJoueurs[i].Main, true));
                                //Puis on écarte
                                ListeJoueurs[i].Ecarter(carteEcartees[i]);
                            }
                            //Puis la carte est passée au joueur suivant (ou au premier quand le compteur atteint la limite du nombre de joueurs)
                            for (int i = 0, c = ListeJoueurs.Count; i < c; i++)
                            {
                                if (ListeJoueurs[i] == ListeJoueurs[ListeJoueurs.Count - 1])
                                { ListeJoueurs[0].Main.Add(carteEcartees[i]); }
                                else
                                { ListeJoueurs[i + 1].Main.Add(carteEcartees[i]); }
                            }
                            JoueurActuel.MAJMain();

                            //Puis le deuxième effet : le joueur qui joue peut écarter une carte de sa main
                            Carte carteEcartee = new Carte();
                            carteEcartee = JoueurActuel.ChoisirUneCarte("Ecarter", JoueurActuel.Main, false);
                            if (!(carteEcartee is null))
                            { JoueurActuel.Ecarter(carteEcartee); }
                        }
                        break;

                    case "Evèque":
                        {
                            //Ici on ajoute 1 pièce et 1 jeton, puis on fait écarter une carte et on gagne d'autres jetons selon son coût, et les autres peuvent écarter également

                            Carte carteEcartee = new Carte();
                            //On fait choisir la carte puis on l'écarte
                            carteEcartee = JoueurActuel.ChoisirUneCarte("Ecarter", JoueurActuel.Main, true);
                            JoueurActuel.Ecarter(carteEcartee);
                            //Puis on ajoute le bon nombre de jetons de victoire
                            double tempCout = carteEcartee.Cout / 2;
                            JoueurActuel.JetonVictoireDispo += (int)Math.Floor(tempCout);

                            //Puis on boucle sur les autres joueurs pour qu'ils écartent
                            for (int i = 0; i < ListeJoueurs.Count; i++)
                            {
                                carteEcartee = null;
                                //Gérer le cas d'annulation
                                carteEcartee = ListeJoueurs[i].ChoisirUneCarte("Ecarter", ListeJoueurs[i].Main, false);
                                if (!(carteEcartee is null))
                                { ListeJoueurs[i].Ecarter(carteEcartee); }
                            }
                        }
                        break;

                    case "Bibliothèque":
                        {
                            //Le joueur va piocher des cartes jusqu'à en avoir 7 et peut défausser les cartes action

                            //On crée une liste pour les cartes allant être défaussées
                            List<Carte> deCoté = new List<Carte>();
                            while (JoueurActuel.Main.Count < 7)
                            {
                                JoueurActuel.Piocher(1);
                                Carte carte = JoueurActuel.Main[JoueurActuel.Main.Count - 1];
                                if (carte.Type.Contains("Action"))
                                {
                                    //On affiche la question.
                                    DialogResult dr = MessageBox.Show($"Voulez-vous mettre {carte.Nom} de côté?", "Choix", MessageBoxButtons.YesNo);
                                    //Si l'utilisateur répond oui, on ajoute la carte à la liste des cartes allant être défaussées
                                    if (dr == DialogResult.Yes)
                                    { deCoté.Add(carte); }
                                }
                            }
                            //Puis on défausse les cartes ayant été mises de côté
                            foreach (Carte carte in deCoté)
                            { JoueurActuel.Defausser(carte); }
                        }
                        break;

                    case "Aventurier":
                        {
                            //On va dévoiler des cartes jusqu'à trouver deux trésors. On défausse les autres

                            //On crée une List de cartes dévoilées
                            List<Carte> cartesDevoilees = new List<Carte>();
                            int i = 0;
                            //Le compteur sera incrémenté par les trésors uniquement
                            while (i < 2)
                            {
                                //Pour ne pas lever d'erreur, on vérifie que le deck contient au moins une carte
                                if (JoueurActuel.Deck.Count > 0)
                                {
                                    //Si la première carte du deck n'est pas un trésor, elle est ajoutée à la défausse
                                    if (JoueurActuel.Deck[0].Type != "Trésor")
                                    { JoueurActuel.Defausse.Add(JoueurActuel.Deck[0]); }
                                    else
                                    {
                                        //Sinon, elle est ajoutée à la main et le compteur est incrémenté
                                        JoueurActuel.Main.Add(JoueurActuel.Deck[0]);
                                        i++;
                                    }
                                    //Dans tous les cas, la carte est ajoutée à la List des cartes dévoilées, puis supprimée du Deck
                                    cartesDevoilees.Add(JoueurActuel.Deck[0]);
                                    JoueurActuel.Deck.RemoveAt(0);
                                }
                                //Si le deck est vide, on le mélange avec la défausse
                                else
                                { JoueurActuel.MelangerLeDeck(); }
                            }
                            //On affiche les cartes dévoilées
                            AffichageCartesDevoilees.listCartesDevoilees = cartesDevoilees;
                            AffichageCartesDevoilees form = new AffichageCartesDevoilees();
                            form.ShowDialog();
                            //Puis on met à jour les infos et la main

                        }
                        break;

                    case "Agrandissement":
                        {
                            //On écarte une carte de la main et reçoit une carte coûtant jusqu'à 3 de plus

                            //On fait d'abord choisir la carte à écarter dans la main
                            Carte carteEcartee = JoueurActuel.ChoisirUneCarte("Ecarter", JoueurActuel.Main, true);
                            JoueurActuel.Ecarter(carteEcartee);
                            MessageBox.Show("Choisissez une carte coûtant jusqu'à 3 de plus que la carte écartée");
                            //Puis on crée une liste de cartes coûtant au max 3 de plus que la carte écartée afin de la proposer en choix
                            List<Carte> piles = new List<Carte>();
                            //On crée une autre liste de Pile qu'on va trier pour afficher en premier les cartes les plus intéressantes
                            List<Pile> mapListe = new List<Pile>(PartieForm.mapListe);
                            mapListe = mapListe.OrderByDescending(x => x.carte.Cout).ToList();
                            foreach (Pile pile in mapListe)
                            {
                                //Les cartes proposées ne doivent pas coûter plus que 3 de plus que la carte écartée, mais elles doivent aussi encore être disponibles
                                if ((pile.nombre > 0) && (pile.carte.Cout <= carteEcartee.Cout + 3))
                                { piles.Add(pile.carte); }
                            }
                            //On choisit la carte et on la reçoit
                            Carte carteRecue = JoueurActuel.ChoisirUneCarte("Recevoir", piles, true);
                            JoueurActuel.Recevoir(carteRecue);
                        }
                        break;

                    case "Or des fous":
                        {
                            //Le trésor vaut 1 si c'est le premier joué, 4 si au moins un Or des fous a été joué précédemment

                            JoueurActuel.MonnaieDispo = ((JoueurActuel.Main.Find(x => (x.Nom == "Or des fous") && (x.EnJeu) && (x != this))) is null) ? JoueurActuel.MonnaieDispo + 1 : JoueurActuel.MonnaieDispo + 4;
                        }
                        break;

                    case "Rénovation":
                        {
                            //On écarte une carte de la main et on reçoit une carte coûtant jusqu'à 2 de plus
                            //(il s'agit ni plus ni moins que de l'effet d'agrandissement avec 2 à la place de 3)

                            //On fait d'abord choisir la carte à écarter dans la main
                            Carte carteEcartee = JoueurActuel.ChoisirUneCarte("Ecarter", JoueurActuel.Main, true);
                            JoueurActuel.Ecarter(carteEcartee);
                            MessageBox.Show("Choisissez une carte coûtant jusqu'à 2 de plus que la carte écartée");
                            //Puis on crée une liste de cartes coûtant au max 2 de plus que la carte écartée afin de la proposer en choix
                            List<Carte> piles = new List<Carte>();
                            //On crée une autre liste de Pile qu'on va trier pour afficher en premier les cartes les plus intéressantes
                            List<Pile> mapListe = new List<Pile>(PartieForm.mapListe);
                            mapListe = mapListe.OrderByDescending(x => x.carte.Cout).ToList();
                            foreach (Pile pile in mapListe)
                            {
                                //Les cartes proposées ne doivent pas coûter plus que 3 de plus que la carte écartée, mais elles doivent aussi encore être disponibles
                                if ((pile.nombre > 0) && (pile.carte.Cout <= carteEcartee.Cout + 2))
                                { piles.Add(pile.carte); }
                            }
                            //On choisit la carte et on la reçoit
                            Carte carteRecue = JoueurActuel.ChoisirUneCarte("Recevoir", piles, true);
                            JoueurActuel.Recevoir(carteRecue);
                        }
                        break;

                    case "Village agricole":
                        {
                            //Le joueur dévoile des cartes jusqu'à tomber sur une Action ou un Trésor

                            Carte carte = new Carte();

                            int i = 0;
                            do
                            {
                                carte = JoueurActuel.Deck[i];
                                i++;
                            }
                            while (!carte.Type.Contains("Action") && !carte.Type.Contains("Trésor"));
                            //Et on lance la fonction pour dévoiler le nombre de cartes approprié
                            List<Carte> cartesDevoilees = JoueurActuel.Devoiler(i);

                            //On sort la dernière carte de la List, on l'ajoute à la main, et on met les autres dans le défausse
                            JoueurActuel.Main.Add(cartesDevoilees.Last());
                            cartesDevoilees.RemoveAt(cartesDevoilees.Count - 1);
                            JoueurActuel.Defausse.AddRange(cartesDevoilees);
                        }
                        break;

                    case "Mine":
                        {
                            //Le joueur écarte un Trésor et en reçoit une dans la main coûtant jusqu'à 3 de plus
                            //Encore un code ressemblant à Agrandissement et Rénovation

                            //On fait choisir une carte Trésor et on l'écarte
                            Carte carteEcartee = JoueurActuel.ChoisirUneCarte("Ecarter", JoueurActuel.Main.FindAll(x => x.Type.Contains("Trésor")), true);
                            JoueurActuel.Ecarter(carteEcartee);
                            MessageBox.Show("Choisissez un Trésor coûtant jusqu'à 3 de plus que le Trésor écarté");
                            //Puis on crée une liste de trésors coûtant au max 3 de plus que le trésor écarté afin de la proposer en choix
                            List<Carte> piles = new List<Carte>();
                            //On crée une autre liste de Pile qu'on va trier pour afficher en premier les cartes les plus intéressantes
                            List<Pile> mapListe = new List<Pile>(PartieForm.mapListe);
                            mapListe = mapListe.OrderByDescending(x => x.carte.Cout).ToList();
                            foreach (Pile pile in mapListe)
                            {
                                //Les cartes proposées ne doivent pas coûter plus que 3 de plus que la carte écartée, mais elles doivent aussi encore être disponibles
                                if ((pile.nombre > 0) && (pile.carte.Cout <= carteEcartee.Cout + 3) && (pile.carte.Type.Contains("Trésor")))
                                { piles.Add(pile.carte); }
                            }
                            //On choisit la carte et on la reçoit
                            Carte carteRecue = JoueurActuel.ChoisirUneCarte("Recevoir", piles, true);
                            JoueurActuel.Recevoir(carteRecue, "En main");
                        }
                        break;

                    case "Marchand d'épices":
                        {
                            //Le joueur choisit une des possibiltiés en échange de l'écartement d'une carte de sa main

                            string choix = JoueurActuel.ChoisirUnePossibilite("+2 cartes +1 action", "+1 achat +2 monnaie");
                            Carte carteAecarter = JoueurActuel.ChoisirUneCarte("Ecarter", JoueurActuel.Main, true);
                            JoueurActuel.Ecarter(carteAecarter);
                            if (choix == "+2 cartes +1 action")
                            {
                                JoueurActuel.Piocher(2);
                                JoueurActuel.ActionDispo++;
                            }
                            else
                            {
                                JoueurActuel.AchatDispo++;
                                JoueurActuel.MonnaieDispo += 2;
                            }
                        }
                        break;

                    case "Ambassade":
                        {
                            //Après avoir pioché ses 5 cartes, le joueur doit en défausser 3.

                            List<Carte> aDefausser = JoueurActuel.ChoisirDesCartes("Défausser", JoueurActuel.Main, 3, true);
                            foreach (Carte carte in aDefausser)
                            { JoueurActuel.Defausser(carte); }
                        }
                        break;

                    case "Mandarin":
                        {
                            //Le joueur choisit une carte de sa main qu'il place sur son deck

                            Carte aDeplacer = JoueurActuel.ChoisirUneCarte("Deck", JoueurActuel.Main.FindAll(x => x.EnJeu == false), true);
                            JoueurActuel.Deck.Insert(0, aDeplacer);
                            JoueurActuel.Main.Remove(aDeplacer);
                        }  
                        break;
                }
            }
        }

        public void Reagir()
        {
            switch (this.Nom)
            {
                case "Douves":
                    continuerAttaque = false;
                    joueurSousAttaque.Defausser(this);
                    break;
                case "Or des fous":
                    {
                        //On donne le choix d'écarter la carte pour recevoir un or sur le deck
                        DialogResult dr = MessageBox.Show($"{PartieForm.tempJoueur.Nom}, voulez-vous écarter un Or des fous pour recevoir un Or sur votre deck?", "Choix", MessageBoxButtons.YesNo);
                        if (dr == DialogResult.Yes)
                        {
                            //On utilise la méthode Recevoir pour intégrer le code supprimant une Carte de la Pile etc
                            PartieForm.tempJoueur.Recevoir(PartieForm.mapListe.Find(x => x.carte.Nom == "Or").carte, "Deck");
                            PartieForm.tempJoueur.Ecarter(this);
                        }
                    }
                    break;
            }
        }

        public void CompterPointsDeVictoire()
        {
            switch (this.Nom)
            {
                case "Route de la soie":
                    {
                        //La carte vaut 1 point de victoire pour chaque 4 cartes Victoire possédées

                        double nbVictoire = PartieForm.tempJoueur.Deck.FindAll(x => x.Type.Contains("Victoire")).Count;
                        PartieForm.tempJoueur.Points += (int)Math.Floor(nbVictoire / 4);
                    }
                    break;
            }
        }

        public object Clone()
        {
            return new Carte
                (this.Nom, this.Image, this.Cout, this.Type, this.EffetText, this.MonnaieDonnee, this.CarteDonnee, this.ActionDonnee,
                this.AchatDonne, this.JetonPointDonne, this.PointDonne); ;
        }
    }
}
