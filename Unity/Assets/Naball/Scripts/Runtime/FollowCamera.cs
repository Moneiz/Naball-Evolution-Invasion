using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Équivalent de l'actuator Camera du BGE (Ger.cam : hauteur 5, distance 15 à 20, amortissement 0.031)
    /// suivi d'un "Track To" vers la boule. Q et E tournent autour de la boule (touches A/Z de l'original).
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public float height = 5f;
        public float minDistance = 15f;
        public float maxDistance = 20f;
        [Tooltip("Fraction de l'écart rattrapée à chaque tick de 1/60 s.")]
        public float damping = 0.031f;
        public float orbitSpeed = 90f;
        public float lookHeight = 1f;

        void LateUpdate()
        {
            if (target == null)
                return;

            var toCamera = transform.position - target.position;
            toCamera.y = 0f;
            float orbit = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
            if (orbit != 0f && !DialogSystem.BlocksInput)
                toCamera = Quaternion.Euler(0f, orbit * orbitSpeed * Time.deltaTime, 0f) * toCamera;
            if (toCamera.sqrMagnitude < 0.01f)
                toCamera = -target.forward;

            float distance = Mathf.Clamp(toCamera.magnitude, minDistance, maxDistance);
            var desired = target.position + toCamera.normalized * distance + Vector3.up * height;
            float t = 1f - Mathf.Pow(1f - damping, Time.unscaledDeltaTime * 60f);
            transform.position = Vector3.Lerp(transform.position, desired, t);
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }

        /// <summary>Place la caméra directement à sa position de repos, sans amortissement.</summary>
        public void Snap()
        {
            if (target == null)
                return;
            var back = -target.forward;
            transform.position = target.position + back * minDistance + Vector3.up * height;
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }
    }
}
