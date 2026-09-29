using UnityEngine;
using UnityEngine.UI;

namespace Naball
{
    /// <summary>
    /// Teinte d'écran selon la dimension (bleu ou rouge), avec un éclair au début de chaque bascule.
    /// Le prototype passait par un Volume URP (aberration chromatique, gamma) ; ici un simple calque d'UI
    /// suffit et fonctionne avec le pipeline intégré.
    /// </summary>
    public class DimensionTint : MonoBehaviour
    {
        [Range(0f, 1f)] public float strength = 0.16f;
        [Range(0f, 1f)] public float flash = 0.35f;

        Image overlay;
        float flashAmount;

        void Awake()
        {
            var canvas = Ui.Canvas("Teinte de dimension", 5).transform;
            canvas.SetParent(transform, false);
            overlay = Ui.Panel("Teinte", canvas, Vector2.zero, Vector2.one, Color.clear);
            overlay.raycastTarget = false;
        }

        void OnEnable() => DimensionSystem.Instance.ShiftStarted += OnShift;

        void OnDisable()
        {
            if (DimensionSystem.Existing != null)
                DimensionSystem.Existing.ShiftStarted -= OnShift;
        }

        void OnShift(float target) => flashAmount = flash;

        void Update()
        {
            float d = DimensionSystem.Instance.Value;
            var color = DimensionSystem.ColorOf(d);
            flashAmount = Mathf.MoveTowards(flashAmount, 0f, Time.deltaTime * 2f);
            color.a = strength * Mathf.Abs(d) + flashAmount;
            overlay.color = color;
        }
    }
}
