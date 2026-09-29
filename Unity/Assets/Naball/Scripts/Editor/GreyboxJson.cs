using System;

namespace Naball.EditorTools
{
    // Format des fichiers de salle Assets/Naball/Greybox/*.json, décrit dans Assets/Naball/Greybox/LISEZMOI.md.
    // Les noms de champs sont en français : ce sont eux qu'on écrit dans les fichiers.
    // Positions et tailles en mètres, x à droite, y en haut, z vers l'avant (le joueur part vers +z).

    [Serializable]
    public class GreyboxJson
    {
        public string titre;
        public float[] apparition;          // pieds de Lumka
        public float orientation;           // degrés autour de y, 0 = vers +z
        public string dimensionDepart = "neutre";
        public float dureeBascule = 0.5f;
        public string[] pouvoirs = new string[0];   // pouvoirs donnés dès le départ : bascule, tir, dash
        public GreyboxPiece[] pieces = new GreyboxPiece[0];
        public GreyboxFruit[] fruits = new GreyboxFruit[0];
        public GreyboxClim[] clims = new GreyboxClim[0];
        public GreyboxAtomium[] atomiums = new GreyboxAtomium[0];
        public GreyboxPoint[] controles = new GreyboxPoint[0];
        public GreyboxSign[] panneaux = new GreyboxSign[0];
        public GreyboxSign fin;
    }

    [Serializable]
    public class GreyboxPiece
    {
        public string nom;
        public string forme = "bloc";       // bloc, rampe (monte vers +z), pilier
        public float[] position;            // centre de la face du dessous
        public float[] taille;              // largeur (x), hauteur (y), profondeur (z)
        public float[] rotation;            // degrés
        public string dimensions = "";      // vide = partout ; sinon "bleu", "neutre", "rouge" séparés par des virgules
        public float[] bleu, neutre, rouge;                          // décalage de position dans chaque dimension
        public float[] rotationBleu, rotationNeutre, rotationRouge;  // rotation ajoutée dans chaque dimension
        public string surface = "sol";      // sol, mur, lave
    }

    [Serializable]
    public class GreyboxFruit
    {
        public float[] position;
        public string vers = "bleu";        // dimension imposée par le fruit
    }

    [Serializable]
    public class GreyboxClim
    {
        public float[] position;
        public string dimensions = "";
    }

    [Serializable]
    public class GreyboxAtomium
    {
        public float[] position;
        public string pouvoir = "bascule";
        public string message;
    }

    [Serializable]
    public class GreyboxPoint
    {
        public float[] position;
    }

    [Serializable]
    public class GreyboxSign
    {
        public float[] position;
        public string texte;
    }
}
