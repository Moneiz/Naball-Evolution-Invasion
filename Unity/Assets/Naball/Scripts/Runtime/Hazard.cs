using UnityEngine;

namespace Naball
{
    /// <summary>Surface mortelle (lave) : le joueur qui la touche réapparaît au dernier point de contrôle.</summary>
    public class Hazard : MonoBehaviour
    {
        public string message = "La lave brûle… Essaie en bleu.";

        void OnCollisionEnter(Collision collision) => Touch(collision.collider);
        void OnTriggerEnter(Collider other) => Touch(other);

        void Touch(Collider other)
        {
            var player = other.GetComponentInParent<PlayerCharacter>();
            if (player == null)
                return;
            player.Respawn();
            if (Hud.Instance != null && !string.IsNullOrEmpty(message))
                Hud.Instance.Show(message, 2f);
        }
    }
}
