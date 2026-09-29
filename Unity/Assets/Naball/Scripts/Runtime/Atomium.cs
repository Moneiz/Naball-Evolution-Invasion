using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Atomium : fragment de la dalle magique qui donne un pouvoir à Lumka dès qu'elle le touche
    /// (docs/scenario.md, « Comment Lumka obtient ses pouvoirs »).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Atomium : MonoBehaviour
    {
        [Tooltip("bascule, tir ou dash")]
        public string power = "bascule";
        [TextArea] public string message;

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void Update() => transform.Rotate(30f * Time.deltaTime, 60f * Time.deltaTime, 0f, Space.World);

        void OnTriggerEnter(Collider other)
        {
            if (!PlayerCharacter.Owns(other))
                return;
            Grant(power);
            GameState.Data.nbAto++;
            if (Hud.Instance != null && !string.IsNullOrEmpty(message))
                Hud.Instance.Show(message, 5f);
            Destroy(gameObject);
        }

        /// <summary>Active un pouvoir par son nom, tel qu'il est écrit dans les fichiers de salle.</summary>
        public static void Grant(string power)
        {
            var powers = GameState.Data.powers;
            switch (power)
            {
                case "bascule": powers.shift = true; break;
                case "tir": powers.eta = true; break;
                case "dash": powers.move = true; break;
                case "saut": powers.jump = true; break;
                default: Debug.LogWarning($"Pouvoir inconnu : {power}"); break;
            }
        }
    }
}
