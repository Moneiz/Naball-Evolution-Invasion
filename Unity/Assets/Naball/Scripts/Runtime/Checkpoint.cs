using UnityEngine;

namespace Naball
{
    /// <summary>Point de contrôle : le joueur qui le traverse réapparaîtra ici après une chute.</summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerCharacter>();
            if (player != null)
                player.respawnPoint = transform;
        }
    }
}
