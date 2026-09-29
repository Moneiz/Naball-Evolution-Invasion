using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Fruit 4D (prototype lumka-player) : le toucher fait basculer le monde vers sa dimension.
    /// Il repousse après quelques secondes, pour qu'une énigme ratée puisse être recommencée.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Fruit4D : MonoBehaviour
    {
        [Range(-1f, 1f)] public float dimension = DimensionSystem.Blue;
        public float regrowDelay = 4f;
        public AudioClip sound;

        Renderer[] renderers;
        Collider trigger;
        float regrowAt = -1f;

        void Awake()
        {
            trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
            renderers = GetComponentsInChildren<Renderer>();
        }

        void Update()
        {
            transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
            if (regrowAt > 0f && Time.time >= regrowAt)
                Show(true);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!trigger.enabled || !PlayerCharacter.Owns(other))
                return;
            DimensionSystem.Instance.ShiftTo(dimension);
            if (sound != null)
                AudioSource.PlayClipAtPoint(sound, transform.position);
            Show(false);
        }

        void Show(bool visible)
        {
            regrowAt = visible ? -1f : Time.time + regrowDelay;
            trigger.enabled = visible;
            foreach (var r in renderers)
                r.enabled = visible;
        }
    }
}
