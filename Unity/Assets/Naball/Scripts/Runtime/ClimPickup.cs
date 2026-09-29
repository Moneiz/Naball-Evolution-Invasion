using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Le Clim blanc : tourne sur lui-même, file vers le joueur quand elle est proche (actuator Steering)
    /// et s'ajoute au compteur au contact.
    /// </summary>
    public class ClimPickup : MonoBehaviour
    {
        public string climId;
        public float magnetRadius = 9f;
        public float magnetSpeed = 20f;

        Collider ownCollider;

        void Awake() => ownCollider = GetComponent<Collider>();

        void Update()
        {
            // Un Clim absent de la dimension courante (collisions coupées) n'est pas attiré.
            if (ownCollider != null && !ownCollider.enabled)
                return;
            var player = PlayerCharacter.Current;
            if (player == null || magnetSpeed <= 0f)
                return;
            var target = player.Center;
            if ((target - transform.position).sqrMagnitude <= magnetRadius * magnetRadius)
                transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!PlayerCharacter.Owns(other))
                return;
            GameState.CollectClim(climId);
            if (Hud.Instance != null)
                Hud.Instance.OnClimCollected();
            Destroy(gameObject);
        }
    }
}
