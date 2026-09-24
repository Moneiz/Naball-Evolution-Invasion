using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Le Clim blanc : tourne sur lui-même, file vers la boule quand elle est proche (actuator Steering)
    /// et s'ajoute au compteur au contact.
    /// </summary>
    public class ClimPickup : MonoBehaviour
    {
        public string climId;
        public float magnetRadius = 9f;
        public float magnetSpeed = 20f;

        static NaballController player;

        void Update()
        {
            if (player == null)
                player = FindAnyObjectByType<NaballController>();
            if (player == null || magnetSpeed <= 0f)
                return;
            var target = player.transform.position;
            if ((target - transform.position).sqrMagnitude <= magnetRadius * magnetRadius)
                transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<NaballController>() == null)
                return;
            GameState.CollectClim(climId);
            if (Hud.Instance != null)
                Hud.Instance.OnClimCollected();
            Destroy(gameObject);
        }
    }
}
