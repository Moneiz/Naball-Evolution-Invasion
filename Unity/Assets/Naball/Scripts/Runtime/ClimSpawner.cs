using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Emplacement d'un Clim (Empty "ger.climN") : fait apparaître le Clim après 20 ticks s'il n'a pas
    /// déjà été ramassé dans cette sauvegarde (_climsFunc.main).
    /// </summary>
    public class ClimSpawner : MonoBehaviour
    {
        public string climId;
        public GameObject climPrefab;
        public float collectRadius = 9f;
        public float magnetSpeed = 20f;
        public float spawnDelay = 20f / 60f;

        void Start()
        {
            if (GameState.IsClimCollected(climId))
            {
                gameObject.SetActive(false);
                return;
            }
            Invoke(nameof(Spawn), spawnDelay);
        }

        void Spawn()
        {
            if (climPrefab == null)
                return;
            var clim = Instantiate(climPrefab, transform.position, Quaternion.identity, transform);
            var pickup = clim.GetComponent<ClimPickup>();
            pickup.climId = climId;
            pickup.magnetRadius = collectRadius;
            pickup.magnetSpeed = magnetSpeed;
        }
    }
}
