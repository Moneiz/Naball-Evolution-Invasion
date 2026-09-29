using System;
using UnityEngine;

namespace Naball
{
    /// <summary>
    /// La dimension courante du monde, de −1 (bleu : fige, refroidit) à +1 (rouge : accélère, réchauffe),
    /// 0 étant le monde neutre. Repris de TransDimensionalHandler du prototype lumka-player (−10..+10),
    /// avec une différence essentielle : la bascule n'est pas instantanée, la valeur glisse vers la cible
    /// et les objets suivent leur courbe pendant ce temps, ce qui permet des énigmes de timing.
    /// </summary>
    public class DimensionSystem : MonoBehaviour
    {
        public const float Blue = -1f, Neutral = 0f, Red = 1f;

        static DimensionSystem instance;

        /// <summary>Le système de la scène, créé à la demande pour que tout objet puisse l'interroger.</summary>
        public static DimensionSystem Instance
        {
            get
            {
                if (instance == null)
                    instance = FindAnyObjectByType<DimensionSystem>() ?? new GameObject("Dimensions").AddComponent<DimensionSystem>();
                return instance;
            }
        }

        /// <summary>Le système s'il existe déjà, sans en créer un (utile pendant la fermeture d'une scène).</summary>
        public static DimensionSystem Existing => instance;

        [Tooltip("Durée d'une bascule d'une dimension à la voisine (neutre → bleu par exemple).")]
        public float shiftDuration = 0.5f;
        [Range(-1f, 1f)] public float startDimension;

        public float Value { get; private set; }
        public float Target { get; private set; }
        public bool IsShifting => !Mathf.Approximately(Value, Target);

        /// <summary>Appelé au début d'une bascule avec la dimension visée.</summary>
        public event Action<float> ShiftStarted;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;
            Value = Target = Mathf.Clamp(startDimension, Blue, Red);
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public void ShiftTo(float target)
        {
            target = Mathf.Clamp(target, Blue, Red);
            if (Mathf.Approximately(target, Target))
                return;
            Target = target;
            ShiftStarted?.Invoke(target);
        }

        /// <summary>Un cran vers le rouge (+1) ou vers le bleu (−1). Renvoie false si on est déjà au bout.</summary>
        public bool Step(int direction)
        {
            float next = Mathf.Clamp(Mathf.Round(Target) + Mathf.Sign(direction), Blue, Red);
            if (Mathf.Approximately(next, Target))
                return false;
            ShiftTo(next);
            return true;
        }

        void FixedUpdate()
        {
            // Pas physique : les objets qui bougent avec la dimension portent Lumka sans à-coups.
            Value = Mathf.MoveTowards(Value, Target, Time.fixedDeltaTime / Mathf.Max(0.01f, shiftDuration));
        }

        /// <summary>Couleur d'une dimension, pour la teinte d'écran et les échos.</summary>
        public static Color ColorOf(float dimension) =>
            dimension < 0f ? Color.Lerp(Color.white, new Color(0.35f, 0.6f, 1f), -dimension)
                           : Color.Lerp(Color.white, new Color(1f, 0.4f, 0.3f), dimension);
    }
}
