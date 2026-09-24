using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Naball
{
    /// <summary>
    /// Compteur de Clims, messages courts et écran de chargement (scènes HUD et Load_lvl de l'original).
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public static Hud Instance { get; private set; }

        [Tooltip("Dialogue affiché au premier Clim ramassé (Hud.nbClim, dlg.name).")]
        public string firstClimDialog = "#01x02";
        public AudioClip climSound;

        Text climCounter, message;
        Image loading;
        AudioSource audioSource;
        Coroutine messageRoutine;

        void Awake()
        {
            Instance = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            var canvas = Ui.Canvas("HUD", 10).transform;
            canvas.SetParent(transform, false);
            climCounter = Ui.Label("Clims", canvas, 32, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -64), new Vector2(424, -16));
            message = Ui.Label("Message", canvas, 28, TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.75f), new Vector2(0.9f, 0.85f));
            message.color = new Color(1f, 0.85f, 0.3f);

            loading = Ui.Panel("Chargement", canvas, Vector2.zero, Vector2.one, Color.white);
            loading.preserveAspect = false;
            loading.gameObject.SetActive(false);
            var loadingLabel = Ui.Label("Texte", loading.transform, 36, TextAnchor.LowerRight,
                Vector2.zero, Vector2.one, new Vector2(24, 24), new Vector2(-24, -24));
            loadingLabel.text = "Chargement…";

            GameState.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            GameState.Changed -= Refresh;
            if (Instance == this)
                Instance = null;
        }

        void Refresh() => climCounter.text = $"Clims  {GameState.Data.nbClim}/{GameState.ClimTotal}";

        public void OnClimCollected()
        {
            if (climSound != null)
                audioSource.PlayOneShot(climSound);
            if (GameState.Data.nbClim == 1)
                DialogSystem.Play(firstClimDialog);
        }

        public void Show(string text, float seconds = 3f)
        {
            if (messageRoutine != null)
                StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(ShowRoutine(text, seconds));
        }

        IEnumerator ShowRoutine(string text, float seconds)
        {
            message.text = text;
            yield return new WaitForSeconds(seconds);
            message.text = "";
        }

        public void ShowLoading(string image)
        {
            var texture = string.IsNullOrEmpty(image) ? null : Resources.Load<Texture2D>("LoadingScreens/" + image);
            loading.sprite = texture != null
                ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f))
                : null;
            loading.color = texture != null ? Color.white : Color.black;
            loading.gameObject.SetActive(true);
        }

        public void HideLoading() => loading.gameObject.SetActive(false);
    }
}
