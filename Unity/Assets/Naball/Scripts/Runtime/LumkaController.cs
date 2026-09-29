using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Lumka, le héros du remake (prototype lumka-player de 2021), repris avec un contrôle plus précis :
    /// déplacement analogique relatif à la caméra avec accélération et virages progressifs, pentes suivies,
    /// saut à hauteur variable avec tolérance au bord (coyote time) et mémoire de la touche (jump buffer),
    /// bascule entre les dimensions, dash et tir, qui consomment la même jauge d'énergie.
    /// Sur un objet qui bouge avec la dimension, Lumka est emportée avec lui.
    /// Le prototype déplaçait Lumka par transform.Translate, ce qui traversait les murs et ignorait les pentes :
    /// ici tout passe par le Rigidbody.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class LumkaController : PlayerCharacter
    {
        [Header("Course")]
        public float runSpeed = 7f;
        [Tooltip("Accélération au sol (m/s²).")]
        public float acceleration = 45f;
        [Tooltip("Freinage au sol quand on lâche la direction (m/s²).")]
        public float deceleration = 60f;
        [Tooltip("Accélération en l'air : on garde la main sur la trajectoire, un peu moins qu'au sol.")]
        public float airAcceleration = 22f;
        public float turnSpeed = 900f;
        [Tooltip("Pente maximale sur laquelle Lumka tient debout.")]
        public float maxSlope = 50f;

        [Header("Saut")]
        public float jumpHeight = 2.6f;
        [Tooltip("Gravité multipliée pendant la chute : saut plus vif, moins flottant.")]
        public float fallGravityMultiplier = 2.2f;
        [Tooltip("Gravité multipliée en montée quand on relâche la touche : saut court.")]
        public float shortJumpGravityMultiplier = 3f;
        public float maxFallSpeed = 35f;
        [Tooltip("Temps pendant lequel on peut encore sauter après avoir quitté un rebord.")]
        public float coyoteTime = 0.12f;
        [Tooltip("Un appui sur saut juste avant d'atterrir est gardé en mémoire pendant ce temps.")]
        public float jumpBuffer = 0.15f;

        [Header("Bascule de dimension (F / R)")]
        [Tooltip("Énergie d'une bascule libre (pouvoir obtenu avec le premier Atomium).")]
        public float shiftCost = 0.15f;

        [Header("Dash (Maj)")]
        public float dashSpeed = 20f;
        public float dashDuration = 0.2f;
        public float dashCost = 0.25f;
        public float dashCooldown = 0.35f;

        [Header("Tir (clic gauche)")]
        public LumkaShot shotPrefab;
        public Transform muzzle;
        public float shotCost = 0.1f;
        public float shotCooldown = 0.3f;
        public AudioClip shotSound;

        [Header("Énergie")]
        public float energyRegen = 0.3f;
        [Tooltip("Délai après une dépense avant que l'énergie remonte.")]
        public float energyRegenDelay = 0.8f;

        [Header("Liens")]
        public Animator animator;
        public Transform cameraTransform;

        public float Energy { get; private set; } = 1f;
        public bool Grounded { get; private set; }
        public bool IsDashing => Time.time < dashEnd;

        Rigidbody body;
        CapsuleCollider capsule;
        AudioSource audioSource;
        LumkaAnimationEvents footsteps;
        Vector3 groundNormal = Vector3.up;
        float lastGroundedTime = -10f, jumpPressedTime = -10f, lastSpendTime = -10f;
        float dashEnd = -10f, nextDashTime, nextShotTime;
        Vector3 dashDirection;
        bool jumping, airDashUsed, dashRequested, shootRequested;
        int shiftRequested;
        DimensionalObject groundObject;
        Vector3 groundPoint;

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int GroundedId = Animator.StringToHash("Grounded");
        static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");
        static readonly int JumpId = Animator.StringToHash("Jump");
        static readonly int DashId = Animator.StringToHash("Dash");
        static readonly int ShootId = Animator.StringToHash("Shoot");

        public override Vector3 Center => capsule != null ? transform.TransformPoint(capsule.center) : transform.position;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            body.useGravity = false;  // gravité appliquée à la main, pour la moduler pendant le saut
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            audioSource = GetComponent<AudioSource>();
            footsteps = GetComponentInChildren<LumkaAnimationEvents>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            // Les appuis sont lus à chaque image et consommés au pas physique suivant.
            if (Controls.JumpDown)
                jumpPressedTime = Time.time;
            if (Controls.DashDown)
                dashRequested = true;
            int shift = Controls.ShiftDown;
            if (shift != 0)
                shiftRequested = shift;
            if (Controls.ShootDown)
                shootRequested = true;

            if (Time.time - lastSpendTime > energyRegenDelay)
                Energy = Mathf.MoveTowards(Energy, 1f, energyRegen * Time.deltaTime);
            Hud.Instance?.SetEnergy(Energy);

            if (animator != null)
            {
                var horizontal = body.linearVelocity;
                horizontal.y = 0f;
                animator.SetFloat(SpeedId, horizontal.magnitude / runSpeed, 0.08f, Time.deltaTime);
                animator.SetBool(GroundedId, Grounded);
                animator.SetFloat(VerticalSpeedId, body.linearVelocity.y);
            }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var powers = GameState.Data.powers;
            var velocity = body.linearVelocity;

            UpdateGround(velocity);

            // Direction voulue, relative à la caméra.
            var input = Controls.Move;
            var wish = CameraRelative(input);

            // Bascule libre d'un cran vers le bleu ou le rouge.
            if (shiftRequested != 0)
            {
                var dimensions = DimensionSystem.Instance;
                float next = Mathf.Clamp(Mathf.Round(dimensions.Target) + shiftRequested, DimensionSystem.Blue, DimensionSystem.Red);
                if (powers.shift && !Mathf.Approximately(next, dimensions.Target) && Spend(shiftCost))
                    dimensions.ShiftTo(next);
                shiftRequested = 0;
            }

            // Dash
            if (dashRequested)
            {
                dashRequested = false;
                if (Time.time >= nextDashTime && (Grounded || !airDashUsed) && Spend(dashCost))
                {
                    dashDirection = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.forward;
                    dashEnd = Time.time + dashDuration;
                    nextDashTime = Time.time + dashDuration + dashCooldown;
                    airDashUsed |= !Grounded;
                    Face(dashDirection, instant: true);
                    Trigger(DashId);
                }
            }

            if (IsDashing)
            {
                velocity = dashDirection * dashSpeed;
                body.linearVelocity = velocity;
                return;
            }

            // Course : on rapproche la vitesse horizontale de la vitesse voulue.
            var horizontal = new Vector3(velocity.x, 0f, velocity.z);
            if (horizontal.magnitude > runSpeed)
                horizontal = horizontal.normalized * Mathf.Max(runSpeed, horizontal.magnitude - deceleration * dt);  // fin de dash
            var target = wish * runSpeed;
            float rate = !Grounded ? airAcceleration : wish.sqrMagnitude > 0.01f ? acceleration : deceleration;
            horizontal = Vector3.MoveTowards(horizontal, target, rate * dt);

            if (wish.sqrMagnitude > 0.01f)
                Face(wish, instant: false);

            // Saut, avec coyote time et mémoire de l'appui.
            bool buffered = Time.time - jumpPressedTime <= jumpBuffer;
            float gravity = -Physics.gravity.y;
            if (buffered && !jumping && powers.jump && Time.time - lastGroundedTime <= coyoteTime)
            {
                velocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);
                jumping = true;
                Grounded = false;
                jumpPressedTime = -10f;
                Trigger(JumpId);
            }

            if (Grounded && !jumping)
            {
                // Au sol : la vitesse suit la pente, et une légère poussée vers le bas garde Lumka collée
                // au sol dans les descentes au lieu de la faire décoller à chaque bosse.
                velocity = Vector3.ProjectOnPlane(horizontal, groundNormal) - groundNormal * 1.5f;
                // Sur un objet qui bouge avec la dimension, Lumka est emportée avec lui.
                if (groundObject != null)
                    velocity += groundObject.PointVelocity(groundPoint);
            }
            else
            {
                float multiplier = velocity.y < 0f ? fallGravityMultiplier
                    : !Controls.JumpHeld ? shortJumpGravityMultiplier
                    : 1f;
                velocity.y = Mathf.Max(velocity.y - gravity * multiplier * dt, -maxFallSpeed);
                velocity.x = horizontal.x;
                velocity.z = horizontal.z;
            }
            body.linearVelocity = velocity;

            // Tir
            if (shootRequested)
            {
                shootRequested = false;
                if (Time.time >= nextShotTime && shotPrefab != null && Spend(shotCost))
                    Shoot();
            }

            if (transform.position.y < fallLimit)
                Respawn();
        }

        void UpdateGround(Vector3 velocity)
        {
            float radius = capsule.radius * 0.95f;
            var origin = transform.position + Vector3.up * (capsule.radius + 0.05f);
            bool found = false;
            // En montée de saut, on ne cherche pas le sol : il accrocherait Lumka au départ.
            if (!(jumping && velocity.y > 0f))
            {
                foreach (var hit in Physics.SphereCastAll(origin, radius, Vector3.down, 0.35f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider == capsule || hit.collider.transform.IsChildOf(transform))
                        continue;
                    if (Vector3.Angle(hit.normal, Vector3.up) > maxSlope)
                        continue;
                    groundNormal = hit.normal;
                    groundPoint = hit.point;
                    groundObject = hit.collider.GetComponentInParent<DimensionalObject>();
                    found = true;
                    break;
                }
            }
            if (found && !Grounded)
            {
                if (footsteps != null)
                    footsteps.Step();  // bruit d'atterrissage
                airDashUsed = false;
                jumping = false;
            }
            Grounded = found;
            if (!found)
            {
                groundNormal = Vector3.up;
                groundObject = null;
            }
            else
                lastGroundedTime = Time.time;
        }

        Vector3 CameraRelative(Vector2 input)
        {
            if (input == Vector2.zero)
                return Vector3.zero;
            var reference = cameraTransform != null ? cameraTransform : transform;
            var forward = Vector3.ProjectOnPlane(reference.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.ProjectOnPlane(reference.up, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, forward);
            return forward * input.y + right * input.x;
        }

        void Face(Vector3 direction, bool instant)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;
            var goal = Quaternion.LookRotation(direction);
            body.MoveRotation(instant ? goal : Quaternion.RotateTowards(body.rotation, goal, turnSpeed * Time.fixedDeltaTime));
        }

        void Trigger(int id)
        {
            if (animator != null)
                animator.SetTrigger(id);
        }

        bool Spend(float amount)
        {
            if (Energy < amount)
            {
                Hud.Instance?.FlashEnergy();
                return false;
            }
            Energy -= amount;
            lastSpendTime = Time.time;
            return true;
        }

        void Shoot()
        {
            nextShotTime = Time.time + shotCooldown;
            // Lumka se tourne vers là où regarde la caméra, puis tire droit devant.
            var aim = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Face(aim, instant: true);
            var origin = muzzle != null ? muzzle.position : Center + transform.forward * 0.6f;
            var shot = Instantiate(shotPrefab, origin, Quaternion.LookRotation(aim));
            shot.owner = transform;
            Trigger(ShootId);
            if (shotSound != null && audioSource != null)
                audioSource.PlayOneShot(shotSound);
        }

        public override void Respawn()
        {
            base.Respawn();
            dashEnd = -10f;
            jumping = false;
        }
    }
}
