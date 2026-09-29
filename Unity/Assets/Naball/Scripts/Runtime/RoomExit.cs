using UnityEngine;

namespace Naball
{
    /// <summary>Fin d'une salle greybox : affiche un message quand le joueur l'atteint.</summary>
    [RequireComponent(typeof(Collider))]
    public class RoomExit : MonoBehaviour
    {
        [TextArea] public string message = "Salle terminée !";
        bool done;

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (done || !PlayerCharacter.Owns(other))
                return;
            done = true;
            Hud.Instance?.Show(message, 6f);
        }
    }
}
