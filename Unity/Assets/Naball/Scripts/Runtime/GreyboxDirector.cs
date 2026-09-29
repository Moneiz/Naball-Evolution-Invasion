using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Mise en place d'une salle greybox : pouvoirs de départ et point d'apparition.
    /// Rien n'est sauvegardé, pour que les essais ne touchent pas la partie en cours.
    /// </summary>
    public class GreyboxDirector : MonoBehaviour
    {
        public PlayerCharacter player;
        public FollowCamera followCamera;
        public Transform spawn;
        public string[] startPowers = new string[0];
        [TextArea] public string title;

        void Start()
        {
            // Une salle de test part toujours sans les pouvoirs qu'elle enseigne.
            GameState.Data.powers.shift = false;
            foreach (var power in startPowers)
                Atomium.Grant(power);
            if (spawn != null && player != null)
            {
                player.respawnPoint = spawn;
                player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            }
            if (followCamera != null)
                followCamera.Snap();
            if (!string.IsNullOrEmpty(title))
                Hud.Instance?.Show(title, 4f);
        }

        void Update()
        {
            // F9 : recommencer la salle depuis le début.
            if (Input.GetKeyDown(KeyCode.F9))
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);
        }
    }
}
