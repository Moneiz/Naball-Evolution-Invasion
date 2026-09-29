using UnityEngine;

namespace Naball
{
    /// <summary>Dialogue lancé quand le joueur touche l'objet (DlgInvocation.RegDlg sur un sensor Collision).</summary>
    public class DialogTrigger : MonoBehaviour
    {
        public string dialog;

        void OnCollisionEnter(Collision collision) => Touch(collision.collider);
        void OnTriggerEnter(Collider other) => Touch(other);

        void Touch(Collider other)
        {
            if (PlayerCharacter.Owns(other))
                DialogSystem.Play(dialog);
        }
    }
}
