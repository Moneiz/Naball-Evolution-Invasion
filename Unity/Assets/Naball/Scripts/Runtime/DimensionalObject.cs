using System.Collections.Generic;
using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Objet qui change avec la dimension : décalage de position, rotation et présence, donnés pour le bleu,
    /// le neutre et le rouge, et interpolés entre les trois pendant la bascule.
    /// Absent de la dimension courante, l'objet perd ses collisions et n'est plus qu'un écho translucide
    /// de la couleur de la dimension où il existe : le joueur voit ce que la bascule va changer.
    /// Remplace TransDimensionalObject du prototype, dont les courbes par axe étaient difficiles à régler.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class DimensionalObject : MonoBehaviour
    {
        [Header("Présence (bleu, neutre, rouge)")]
        public bool inBlue = true;
        public bool inNeutral = true;
        public bool inRed = true;

        [Header("Décalage de position par rapport à la position de départ")]
        public Vector3 blueOffset;
        public Vector3 neutralOffset;
        public Vector3 redOffset;

        [Header("Rotation ajoutée (degrés)")]
        public Vector3 blueRotation;
        public Vector3 neutralRotation;
        public Vector3 redRotation;

        [Tooltip("Matériau de l'écho affiché quand l'objet est absent. Vide : un écho est créé automatiquement.")]
        public Material echoMaterial;

        Vector3 basePosition;
        Quaternion baseRotation;
        Rigidbody body;
        Collider[] colliders;
        Renderer[] renderers;
        Material[][] solidMaterials;
        bool present = true;
        Vector3 lastPosition;
        Quaternion lastRotation;

        /// <summary>Vitesse du dernier pas physique, pour que Lumka suive une plateforme qui bouge.</summary>
        public Vector3 Velocity { get; private set; }
        public Vector3 AngularVelocity { get; private set; }
        public bool Present => present;

        static readonly Dictionary<Color, Material> echoCache = new Dictionary<Color, Material>();

        void Awake()
        {
            basePosition = transform.position;
            baseRotation = transform.rotation;
            lastPosition = basePosition;
            lastRotation = baseRotation;
            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            solidMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
                solidMaterials[i] = renderers[i].sharedMaterials;

            if (!Mathf.Approximately(OffsetRange(), 0f) || !Mathf.Approximately(RotationRange(), 0f))
            {
                // Un objet qui bouge doit être cinématique pour pousser et porter le joueur proprement.
                if (!TryGetComponent(out body))
                    body = gameObject.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        void Start() => Apply(DimensionSystem.Instance.Value, instant: true);

        void FixedUpdate() => Apply(DimensionSystem.Instance.Value, instant: false);

        float OffsetRange() => (blueOffset - neutralOffset).sqrMagnitude + (redOffset - neutralOffset).sqrMagnitude;
        float RotationRange() => (blueRotation - neutralRotation).sqrMagnitude + (redRotation - neutralRotation).sqrMagnitude;

        static T Blend<T>(float d, T blue, T neutral, T red, System.Func<T, T, float, T> lerp) =>
            d < 0f ? lerp(neutral, blue, -d) : lerp(neutral, red, d);

        /// <summary>Présence entre 0 et 1 pour une valeur de dimension donnée.</summary>
        public float Presence(float d) => Blend(d, inBlue ? 1f : 0f, inNeutral ? 1f : 0f, inRed ? 1f : 0f, Mathf.Lerp);

        void Apply(float d, bool instant)
        {
            var position = basePosition + Blend(d, blueOffset, neutralOffset, redOffset, Vector3.Lerp);
            var rotation = baseRotation * Quaternion.Euler(Blend(d, blueRotation, neutralRotation, redRotation, Vector3.Lerp));
            if (body != null && !instant)
            {
                body.MovePosition(position);
                body.MoveRotation(rotation);
            }
            else if (body != null || instant)
                transform.SetPositionAndRotation(position, rotation);
            // Un objet immobile (Clim attiré par exemple) garde la position que d'autres scripts lui donnent.

            float dt = Time.fixedDeltaTime;
            Velocity = instant ? Vector3.zero : (position - lastPosition) / dt;
            (rotation * Quaternion.Inverse(lastRotation)).ToAngleAxis(out float angle, out var axis);
            if (angle > 180f)
                angle -= 360f;
            AngularVelocity = instant || float.IsInfinity(axis.x) ? Vector3.zero : axis * (angle * Mathf.Deg2Rad / dt);
            lastPosition = position;
            lastRotation = rotation;

            // L'objet est solide tant qu'il est présent à plus de moitié : il bascule au milieu de la transition.
            SetPresent(Presence(d) >= 0.5f);
        }

        /// <summary>Vitesse d'un point de l'objet (translation + rotation), pour porter Lumka.</summary>
        public Vector3 PointVelocity(Vector3 point) =>
            Velocity + Vector3.Cross(AngularVelocity, point - transform.position);

        void SetPresent(bool value)
        {
            if (value == present)
                return;
            present = value;
            foreach (var c in colliders)
                c.enabled = value;
            var echo = value ? null : EchoMaterial();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (value)
                    renderers[i].sharedMaterials = solidMaterials[i];
                else
                {
                    var mats = new Material[solidMaterials[i].Length];
                    for (int m = 0; m < mats.Length; m++)
                        mats[m] = echo;
                    renderers[i].sharedMaterials = mats;
                }
                renderers[i].shadowCastingMode = value
                    ? UnityEngine.Rendering.ShadowCastingMode.On
                    : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        Material EchoMaterial()
        {
            if (echoMaterial != null)
                return echoMaterial;
            // Couleur de la dimension où l'objet existe : bleu, rouge, ou blanc s'il n'existe qu'en neutre.
            var color = inBlue && !inRed ? DimensionSystem.ColorOf(DimensionSystem.Blue)
                      : inRed && !inBlue ? DimensionSystem.ColorOf(DimensionSystem.Red)
                      : Color.white;
            color.a = 0.18f;
            if (!echoCache.TryGetValue(color, out var mat) || mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                // Mode Fade du shader Standard.
                mat.SetFloat("_Mode", 2f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
                mat.color = color;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.4f);
                echoCache[color] = mat;
            }
            return mat;
        }
    }
}
