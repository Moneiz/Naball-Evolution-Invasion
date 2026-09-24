using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Naball
{
    /// <summary>Pouvoirs de la boule (power.gs dans le jeu original).</summary>
    [Serializable]
    public class Powers
    {
        // Valeurs de départ choisies pour que la tranche verticale soit jouable dans le hub.
        public bool move = true;   // sprint (Maj)
        public bool jump = true;   // saut (Espace)
        public bool fly = true;    // vol plané, accordé par Power_ger dans Ger_FieldSwamp
        public bool eta;           // tir (C) : pas encore porté
        public bool solidify;      // pas encore porté
    }

    /// <summary>
    /// Une sauvegarde. Remplace les fichiers .gs relus avec eval() : hud.gs, ger.gs, power.gs, stats.gs
    /// et tmp/__dialogs__.gs sont regroupés dans un seul JSON par emplacement.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string language = "fr";
        public int progress;                       // "alr" de ger.gs : avancement dans le hub
        public List<string> collectedClims = new List<string>();
        public List<string> seenDialogs = new List<string>();
        public int nbClim, nbAto, nbCage, nbXp;
        public float life = 50f, lifeMax = 100f;
        public Powers powers = new Powers();
        public string lastSession = "";
    }

    /// <summary>Accès à la sauvegarde courante.</summary>
    public static class GameState
    {
        public const int ClimTotal = 161;          // Hud.climFormat affichait "/161"
        public static int Slot = 1;                // l'original forçait save1/ à chaque sauvegarde

        static SaveData current;

        public static SaveData Data => current ??= Load(Slot);

        public static event Action Changed;

        static string PathFor(int slot) =>
            System.IO.Path.Combine(Application.persistentDataPath, "save" + slot, "save.json");

        public static SaveData Load(int slot)
        {
            var path = PathFor(slot);
            if (!File.Exists(path))
                return new SaveData();
            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(path)) ?? new SaveData();
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning($"Sauvegarde illisible ({path}) : {e.Message}");
                return new SaveData();
            }
        }

        public static void Save()
        {
            var path = PathFor(Slot);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            Data.lastSession = DateTime.UtcNow.ToString("dd/MM/yy-HH:mm");
            File.WriteAllText(path, JsonUtility.ToJson(Data, true));
        }

        public static void Reset()
        {
            current = new SaveData();
            Changed?.Invoke();
        }

        public static bool IsClimCollected(string id) => Data.collectedClims.Contains(id);

        public static void CollectClim(string id)
        {
            if (IsClimCollected(id))
                return;
            Data.collectedClims.Add(id);
            Data.nbClim++;
            Changed?.Invoke();
        }

        public static void SetProgress(int progress)
        {
            Data.progress = Mathf.Max(0, progress);
            Changed?.Invoke();
        }
    }
}
