using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Zone musicale (Sound_Field.*, script AudiViews.GerMod) : volume = 1 - distance / rayon,
    /// mesuré à plat entre la zone et la boule.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SoundField : MonoBehaviour
    {
        public float radius = 50f;
        public Transform listener;

        AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            if (!source.isPlaying)
                source.Play();
        }

        void Update()
        {
            if (listener == null)
                return;
            var d = transform.position - listener.position;
            d.y = 0f;
            source.volume = Mathf.Clamp01(1f - d.magnitude / radius);
        }
    }
}
