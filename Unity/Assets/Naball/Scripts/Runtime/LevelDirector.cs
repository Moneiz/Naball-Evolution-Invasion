using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Naball
{
    /// <summary>
    /// Mise en place du hub selon l'avancement (Maps/Ger.py + gameInstance/Cont/ger.sii) :
    /// point d'apparition de la boule, position de la caméra, dialogue d'accueil et lumière.
    /// </summary>
    public class LevelDirector : MonoBehaviour
    {
        [Serializable]
        public class ProgressState
        {
            public int progress;
            public Transform playerSpawn;
            public Transform cameraStart;
            public string welcomeDialog;
            [Tooltip("Ger.lightVariation : intensité du soleil, négatif = inchangée.")]
            public float sunIntensity = -1f;
        }

        public NaballController player;
        public FollowCamera followCamera;
        public Light sun;
        [Tooltip("Sol principal (Ger.normal) : son dialogue change avec l'avancement.")]
        public DialogTrigger welcomeTrigger;
        public List<ProgressState> states = new List<ProgressState>();

        void Start()
        {
            var state = Current();
            if (state == null)
                return;
            if (state.playerSpawn != null)
            {
                player.respawnPoint = state.playerSpawn;
                player.transform.SetPositionAndRotation(state.playerSpawn.position, state.playerSpawn.rotation);
            }
            if (state.cameraStart != null)
                followCamera.transform.position = state.cameraStart.position;
            else
                followCamera.Snap();
            if (welcomeTrigger != null)
                welcomeTrigger.dialog = state.welcomeDialog;
            if (sun != null && state.sunIntensity >= 0f)
                sun.intensity = state.sunIntensity;
        }

        ProgressState Current()
        {
            ProgressState best = null;
            foreach (var s in states)
                if (s.progress <= GameState.Data.progress && (best == null || s.progress > best.progress))
                    best = s;
            return best ?? (states.Count > 0 ? states[0] : null);
        }

        void Update()
        {
            // Raccourcis de test : F5 / F6 changent l'avancement, F9 repart d'une sauvegarde vierge.
            if (!Application.isEditor && !Debug.isDebugBuild)
                return;
            if (Input.GetKeyDown(KeyCode.F5))
                Reload(() => GameState.SetProgress(GameState.Data.progress - 1));
            if (Input.GetKeyDown(KeyCode.F6))
                Reload(() => GameState.SetProgress(GameState.Data.progress + 1));
            if (Input.GetKeyDown(KeyCode.F9))
                Reload(GameState.Reset);
        }

        void Reload(Action change)
        {
            change();
            GameState.Save();
            Debug.Log($"Avancement du hub : {GameState.Data.progress}");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void OnApplicationQuit() => GameState.Save();
    }
}
