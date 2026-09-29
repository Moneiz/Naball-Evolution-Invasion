using UnityEngine;
using UnityEngine.UI;

namespace Naball
{
    /// <summary>
    /// Port du DialogsEngine 1.05 (Ressources/Dialog.py + DlgInvocation.py) : met le jeu en pause et
    /// affiche les répliques une à une. Chaque dialogue n'est joué qu'une fois par sauvegarde.
    /// </summary>
    public class DialogSystem : MonoBehaviour
    {
        public static DialogSystem Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.current != null;
        /// <summary>Vrai aussi pendant l'image où le dialogue se ferme, pour que la touche ne serve pas deux fois.</summary>
        public static bool BlocksInput => IsOpen || (Instance != null && Instance.closedFrame == Time.frameCount);

        GameObject panel;
        Text text, hint;
        DialogFile current;
        int index;
        int openedFrame, closedFrame = -1;

        void Awake()
        {
            Instance = this;
            var canvas = Ui.Canvas("Dialogues", 20).transform;
            canvas.SetParent(transform, false);
            var box = Ui.Panel("Boite", canvas, new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.26f),
                new Color(0f, 0f, 0f, 0.75f));
            panel = box.gameObject;
            text = Ui.Label("Texte", box.transform, 30, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(32, 36), new Vector2(-32, -16));
            hint = Ui.Label("Suite", box.transform, 18, TextAnchor.LowerRight,
                Vector2.zero, Vector2.one, new Vector2(16, 8), new Vector2(-16, -8));
            hint.text = "Entrée >";
            hint.color = new Color(1f, 1f, 1f, 0.6f);
            panel.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Time.timeScale = 1f;
                Instance = null;
            }
        }

        /// <summary>Joue un dialogue s'il n'a pas encore été vu dans cette sauvegarde.</summary>
        public static void Play(string name, bool evenIfSeen = false)
        {
            if (Instance == null || string.IsNullOrEmpty(name) || IsOpen)
                return;
            var seen = GameState.Data.seenDialogs;
            if (seen.Contains(name) && !evenIfSeen)
                return;
            var file = Localization.Dialog(name);
            if (file == null || file.lines.Count == 0)
                return;
            if (!seen.Contains(name))
                seen.Add(name);
            Instance.Open(file);
        }

        void Open(DialogFile file)
        {
            current = file;
            index = 0;
            openedFrame = Time.frameCount;
            panel.SetActive(true);
            Time.timeScale = 0f;  // l'original suspendait la scène du niveau
            ShowLine();
        }

        void ShowLine()
        {
            var line = current.lines[index];
            text.text = line.text;
            text.color = Localization.DialogColor(line.color);
        }

        void Update()
        {
            if (current == null || Time.frameCount == openedFrame)
                return;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                index++;
                if (index < current.lines.Count)
                {
                    ShowLine();
                    return;
                }
                current = null;
                closedFrame = Time.frameCount;
                panel.SetActive(false);
                Time.timeScale = 1f;
            }
        }
    }
}
