using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Reçoit les événements d'animation du FBX de Lumka (fonction Step dans la course)
    /// et joue un bruit de pas au hasard. Placé sur l'objet qui porte l'Animator.
    /// </summary>
    public class LumkaAnimationEvents : MonoBehaviour
    {
        public AudioClip[] steps;
        public AudioSource source;
        [Range(0f, 1f)] public float volume = 0.6f;

        public void Step()
        {
            if (source == null || steps == null || steps.Length == 0)
                return;
            source.pitch = Random.Range(0.92f, 1.08f);
            source.PlayOneShot(steps[Random.Range(0, steps.Length)], volume);
        }

        // Événement vide laissé dans le clip combatModeToR du prototype.
        public void NewEvent() { }
    }
}
