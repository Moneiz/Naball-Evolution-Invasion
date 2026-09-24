using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Naball
{
    /// <summary>
    /// Portail du hub (Ger.portal.*). Dans l'original : sensor Collision "Naball" + propriété alr &gt; n,
    /// puis sauvegarde, écran Load_lvl et changement de scène après quelques secondes.
    /// </summary>
    public class Portal : MonoBehaviour
    {
        public string targetScene;
        [Tooltip("Avancement minimal (alr) pour que le portail fonctionne.")]
        public int requiredProgress;
        [Tooltip("Portail activé par un message que ce niveau n'envoie jamais.")]
        public bool locked;
        public float delay = 4f;
        public string loadingImage;
        [Tooltip("Disque Cos_back.* qui tourne quand le portail est actif.")]
        public Transform indicator;
        [Tooltip("Motion drot 0.0873 par tick autour de Z dans Blender.")]
        public Vector3 indicatorRadiansPerSecond = new Vector3(0f, 5.24f, 0f);

        bool busy;
        float lastRefusal = -10f;

        public bool IsActive => !locked && GameState.Data.progress >= requiredProgress;

        void Update()
        {
            if (indicator != null && IsActive)
                indicator.Rotate(indicatorRadiansPerSecond * (Mathf.Rad2Deg * Time.deltaTime), Space.Self);
        }

        void OnCollisionEnter(Collision collision) => Touch(collision.collider);
        void OnTriggerEnter(Collider other) => Touch(other);

        void Touch(Collider other)
        {
            if (busy || other.GetComponentInParent<NaballController>() == null)
                return;
            if (!IsActive)
            {
                if (Time.time - lastRefusal > 3f && Hud.Instance != null)
                    Hud.Instance.Show(locked ? "Ce portail est scellé." : "Ce portail n'est pas encore ouvert.");
                lastRefusal = Time.time;
                return;
            }
            StartCoroutine(Travel(other.GetComponentInParent<NaballController>()));
        }

        IEnumerator Travel(NaballController player)
        {
            busy = true;
            GameState.Save();
            Hud.Instance?.ShowLoading(loadingImage);
            yield return new WaitForSeconds(delay);
            if (Application.CanStreamedLevelBeLoaded(targetScene))
            {
                SceneManager.LoadScene(targetScene);
                yield break;
            }
            Hud.Instance?.HideLoading();
            Hud.Instance?.Show($"« {targetScene} » n'est pas encore porté sous Unity.", 4f);
            player.Respawn();
            busy = false;
        }
    }
}
