using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Ce que le reste du jeu attend du personnage joué (Lumka ou la boule d'origine) :
    /// être reconnu par les Clims, portails et dialogues, et pouvoir réapparaître.
    /// </summary>
    public abstract class PlayerCharacter : MonoBehaviour
    {
        public static PlayerCharacter Current { get; private set; }

        public Transform respawnPoint;
        [Tooltip("Altitude sous laquelle le personnage est tombé hors du monde (Ger.NaballBack).")]
        public float fallLimit = -60f;

        /// <summary>Point visé par les Clims attirés : le centre du corps plutôt que les pieds.</summary>
        public virtual Vector3 Center => transform.position;

        protected virtual void OnEnable() => Current = this;

        protected virtual void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        public static bool Owns(Collider other) => other.GetComponentInParent<PlayerCharacter>() != null;

        public virtual void Respawn()
        {
            if (respawnPoint == null)
                return;
            if (TryGetComponent<Rigidbody>(out var body))
            {
                body.linearVelocity = Vector3.zero;
                body.position = respawnPoint.position;
            }
            transform.position = respawnPoint.position;
        }
    }
}
