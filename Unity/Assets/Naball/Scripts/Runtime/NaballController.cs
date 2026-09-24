using UnityEngine;

namespace Naball
{
    /// <summary>
    /// La boule. Port des logic bricks de l'objet "Cube" de Ger_FieldSwamp : le BGE déplaçait l'objet
    /// de dloc par tick (60 ticks/s) et le tournait de drot par tick. Les vitesses ci-dessous en découlent.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class NaballController : MonoBehaviour
    {
        [Header("Déplacement (Motion : dloc -0.1 / -0.25, drot 0.0524 par tick)")]
        public float walkSpeed = 6f;
        public float sprintSpeed = 15f;
        public float turnSpeed = 180f;

        [Header("Saut et vol (Motion : dloc z 0.16 ; Motion.001 : dloc -0.2 / +0.1, propriété Flying 0..2 s)")]
        public float jumpSpeed = 9.6f;
        public float flyForwardSpeed = 12f;
        public float flyUpSpeed = 6f;
        public float flyDuration = 2f;

        [Header("Chute hors du monde (Ger.NaballBack)")]
        public float fallLimit = -60f;

        public Animation skin;       // Armature.003 : ArmatureAction.004 découpée en clips
        public Transform respawnPoint;

        Rigidbody body;
        float flyTime;
        bool grounded, jumpRequested;
        Collider ownCollider;

        public bool IsMoving { get; private set; }

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            ownCollider = GetComponent<Collider>();
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        bool Key(KeyCode a, KeyCode b) => Input.GetKey(a) || Input.GetKey(b);

        void Update()
        {
            if (DialogSystem.BlocksInput)
                return;
            if (Input.GetKeyDown(KeyCode.Space) && grounded && GameState.Data.powers.jump)
                jumpRequested = true;
        }

        void FixedUpdate()
        {
            grounded = CheckGrounded();
            if (grounded)
                flyTime = 0f;

            var powers = GameState.Data.powers;
            bool input = !DialogSystem.BlocksInput;
            bool forward = input && Key(KeyCode.UpArrow, KeyCode.W);
            bool back = input && Key(KeyCode.DownArrow, KeyCode.S);
            float turn = input ? (Key(KeyCode.RightArrow, KeyCode.D) ? 1f : 0f) - (Key(KeyCode.LeftArrow, KeyCode.A) ? 1f : 0f) : 0f;
            bool sprint = input && powers.move && Key(KeyCode.LeftShift, KeyCode.RightShift);
            bool space = input && Input.GetKey(KeyCode.Space);

            if (turn != 0f)
                body.MoveRotation(body.rotation * Quaternion.Euler(0f, turn * turnSpeed * Time.fixedDeltaTime, 0f));

            // dloc ne laissait aucune inertie horizontale : la boule s'arrête dès qu'on relâche la touche.
            float speed = forward ? (sprint ? sprintSpeed : walkSpeed) : back ? -walkSpeed * 0.5f : 0f;
            var velocity = body.linearVelocity;
            var horizontal = transform.forward * speed;
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;

            if (jumpRequested)
            {
                velocity.y = jumpSpeed;
                jumpRequested = false;
                Play("Jump");
            }
            else if (!grounded && space && powers.fly && flyTime < flyDuration)
            {
                flyTime += Time.fixedDeltaTime;
                var glide = transform.forward * flyForwardSpeed;
                velocity = new Vector3(glide.x, Mathf.Max(velocity.y, flyUpSpeed), glide.z);
            }
            body.linearVelocity = velocity;

            IsMoving = speed != 0f;
            if (grounded)
                Play(IsMoving ? "Roll" : "Idle");

            if (transform.position.y < fallLimit)
                Respawn();
        }

        bool CheckGrounded()
        {
            var bounds = ownCollider.bounds;
            var origin = bounds.center;
            float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.9f;
            float distance = bounds.extents.y - radius + 0.15f;
            foreach (var hit in Physics.SphereCastAll(origin, radius, Vector3.down, distance, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider != ownCollider)
                    return true;
            return false;
        }

        void Play(string clip)
        {
            if (skin == null || skin.GetClip(clip) == null)
                return;
            if (clip == "Jump" || !skin.IsPlaying("Jump"))
                skin.CrossFade(clip, 0.1f);
        }

        public void Respawn()
        {
            if (respawnPoint == null)
                return;
            body.linearVelocity = Vector3.zero;
            body.position = respawnPoint.position;
            transform.position = respawnPoint.position;
        }
    }
}
