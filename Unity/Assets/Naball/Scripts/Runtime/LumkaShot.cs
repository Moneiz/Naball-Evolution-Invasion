using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Projectile de Lumka : file droit devant, s'arrête sur le premier obstacle et prévient l'objet touché
    /// par le message OnLumkaShot (les ennemis porteront ce message quand ils seront portés).
    /// </summary>
    public class LumkaShot : MonoBehaviour
    {
        public float speed = 32f;
        public float lifetime = 1.5f;
        public float radius = 0.2f;
        [HideInInspector] public Transform owner;

        float deathTime;

        void Start() => deathTime = Time.time + lifetime;

        void Update()
        {
            float step = speed * Time.deltaTime;
            RaycastHit? nearest = null;
            foreach (var hit in Physics.SphereCastAll(transform.position, radius, transform.forward, step, ~0, QueryTriggerInteraction.Ignore))
                if ((owner == null || !hit.transform.IsChildOf(owner)) && (nearest == null || hit.distance < nearest.Value.distance))
                    nearest = hit;
            if (nearest is RaycastHit h)
            {
                h.collider.SendMessageUpwards("OnLumkaShot", this, SendMessageOptions.DontRequireReceiver);
                transform.position += transform.forward * h.distance;
                Die();
                return;
            }
            transform.position += transform.forward * step;
            if (Time.time >= deathTime)
                Die();
        }

        void Die()
        {
            // La traînée finit de s'estomper avant la destruction.
            enabled = false;
            foreach (var r in GetComponentsInChildren<MeshRenderer>())
                r.enabled = false;
            var light = GetComponentInChildren<Light>();
            if (light != null)
                light.enabled = false;
            var trail = GetComponentInChildren<TrailRenderer>();
            Destroy(gameObject, trail != null ? trail.time : 0f);
        }
    }
}
