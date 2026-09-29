using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Caméra à la troisième personne qui orbite autour du joueur : souris (curseur capturé, Échap le libère)
    /// ou stick droit. Elle se replace doucement derrière le personnage quand on ne la touche pas,
    /// et se rapproche au lieu de traverser les murs.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        [Tooltip("Hauteur du point regardé au-dessus des pieds du personnage.")]
        public float lookHeight = 1.4f;
        public float distance = 7f;
        public float minPitch = -25f;
        public float maxPitch = 65f;
        public float defaultPitch = 18f;

        [Header("Commandes")]
        public float mouseSensitivity = 3f;
        public float stickSpeed = 160f;
        public bool invertY;

        [Header("Suivi")]
        [Tooltip("Vitesse à laquelle le point regardé rattrape le personnage (1/s).")]
        public float followSharpness = 12f;
        [Tooltip("Délai sans action sur la caméra avant qu'elle se replace derrière le personnage.")]
        public float recenterDelay = 1.5f;
        [Tooltip("Vitesse de ce replacement, en degrés par seconde, à pleine vitesse de course.")]
        public float recenterSpeed = 90f;
        public float collisionRadius = 0.3f;

        float yaw, pitch;
        float lastLookTime = -100f;
        Vector3 pivot;
        bool initialized;
        Vector3 lastTargetPosition;

        void Initialize()
        {
            if (initialized || target == null)
                return;
            initialized = true;
            pivot = target.position + Vector3.up * lookHeight;
            lastTargetPosition = target.position;
            var offset = transform.position - pivot;
            if (offset.sqrMagnitude < 0.01f)
                offset = -target.forward;
            SetAngles(offset);
        }

        void SetAngles(Vector3 offset)
        {
            yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
            float horizontal = new Vector2(offset.x, offset.z).magnitude;
            pitch = Mathf.Clamp(Mathf.Atan2(offset.y, horizontal) * Mathf.Rad2Deg, minPitch, maxPitch);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Cursor.lockState = CursorLockMode.None;
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked && !DialogSystem.BlocksInput)
                Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        }

        void LateUpdate()
        {
            if (target == null)
                return;
            Initialize();
            float dt = Time.unscaledDeltaTime;

            var look = Controls.Look(mouseSensitivity, stickSpeed);
            if (look != Vector2.zero)
            {
                lastLookTime = Time.unscaledTime;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch + (invertY ? look.y : -look.y), minPitch, maxPitch);
            }
            else if (Time.unscaledTime - lastLookTime > recenterDelay && dt > 0f)
            {
                // Replacement derrière le personnage, proportionnel à sa vitesse horizontale.
                var moved = target.position - lastTargetPosition;
                moved.y = 0f;
                float speed = moved.magnitude / dt;
                if (speed > 0.5f)
                {
                    float behind = Mathf.Atan2(target.forward.x, target.forward.z) * Mathf.Rad2Deg;
                    float rate = recenterSpeed * Mathf.Clamp01(speed / 7f);
                    yaw = Mathf.MoveTowardsAngle(yaw, behind, rate * dt);
                }
            }
            lastTargetPosition = target.position;

            var goal = target.position + Vector3.up * lookHeight;
            pivot = Vector3.Lerp(pivot, goal, 1f - Mathf.Exp(-followSharpness * dt));

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var back = rotation * Vector3.back;
            float allowed = distance;
            if (Physics.SphereCast(pivot, collisionRadius, back, out var hit, distance, ~0, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(target.root))
                allowed = Mathf.Max(0.5f, hit.distance);

            transform.SetPositionAndRotation(pivot + back * allowed, rotation);
        }

        /// <summary>Oriente la caméra comme si elle se trouvait à <paramref name="position"/> (position de départ d'un niveau).</summary>
        public void LookFrom(Vector3 position)
        {
            if (target == null)
                return;
            initialized = true;
            pivot = target.position + Vector3.up * lookHeight;
            lastTargetPosition = target.position;
            SetAngles(position - pivot);
            LateUpdate();
        }

        /// <summary>Place la caméra derrière le personnage, sans transition.</summary>
        public void Snap()
        {
            if (target == null)
                return;
            initialized = true;
            pivot = target.position + Vector3.up * lookHeight;
            lastTargetPosition = target.position;
            yaw = Mathf.Atan2(target.forward.x, target.forward.z) * Mathf.Rad2Deg;
            pitch = defaultPitch;
            LateUpdate();
        }
    }
}
