using UnityEngine;

namespace Naball
{
    /// <summary>
    /// Lecture des commandes, au clavier (QWERTY et AZERTY) comme à la manette, avec l'Input Manager par défaut.
    /// Clavier : ZQSD / WASD / flèches, Espace saut, Maj dash, clic gauche tir, F / R bascule vers le bleu / le rouge,
    /// souris caméra.
    /// Manette : stick gauche, A saut, B dash, X tir, LB / RB bascule vers le bleu / le rouge, stick droit caméra
    /// si l'axe existe.
    /// </summary>
    public static class Controls
    {
        const float DeadZone = 0.2f;

        static bool Any(params KeyCode[] keys)
        {
            foreach (var k in keys)
                if (Input.GetKey(k))
                    return true;
            return false;
        }

        static bool AnyDown(params KeyCode[] keys)
        {
            foreach (var k in keys)
                if (Input.GetKeyDown(k))
                    return true;
            return false;
        }

        /// <summary>Direction voulue dans le plan (x = droite, y = avant), longueur 0 à 1.</summary>
        public static Vector2 Move
        {
            get
            {
                if (DialogSystem.BlocksInput)
                    return Vector2.zero;
                var keys = new Vector2(
                    (Any(KeyCode.D, KeyCode.RightArrow) ? 1f : 0f) - (Any(KeyCode.A, KeyCode.Q, KeyCode.LeftArrow) ? 1f : 0f),
                    (Any(KeyCode.W, KeyCode.Z, KeyCode.UpArrow) ? 1f : 0f) - (Any(KeyCode.S, KeyCode.DownArrow) ? 1f : 0f));
                if (keys != Vector2.zero)
                    return keys.normalized;
                // "Horizontal"/"Vertical" incluent le stick gauche de la manette.
                var stick = new Vector2(Axis("Horizontal"), Axis("Vertical"));
                return stick.magnitude < DeadZone ? Vector2.zero : Vector2.ClampMagnitude(stick, 1f);
            }
        }

        public static bool JumpDown => !DialogSystem.BlocksInput && AnyDown(KeyCode.Space, KeyCode.JoystickButton0);
        public static bool JumpHeld => !DialogSystem.BlocksInput && Any(KeyCode.Space, KeyCode.JoystickButton0);
        public static bool DashDown => !DialogSystem.BlocksInput &&
            AnyDown(KeyCode.LeftShift, KeyCode.RightShift, KeyCode.JoystickButton1);
        /// <summary>Bascule d'un cran : −1 vers le bleu, +1 vers le rouge, 0 sans appui.</summary>
        public static int ShiftDown =>
            DialogSystem.BlocksInput ? 0
            : AnyDown(KeyCode.F, KeyCode.JoystickButton4) ? -1
            : AnyDown(KeyCode.R, KeyCode.JoystickButton5) ? 1
            : 0;

        public static bool ShootDown => !DialogSystem.BlocksInput &&
            (Input.GetMouseButtonDown(0) || AnyDown(KeyCode.JoystickButton2));

        /// <summary>Rotation de caméra demandée ce frame, en degrés (x = lacet, y = tangage).</summary>
        public static Vector2 Look(float mouseSensitivity, float stickSpeed)
        {
            if (DialogSystem.BlocksInput)
                return Vector2.zero;
            var look = Vector2.zero;
            if (Cursor.lockState == CursorLockMode.Locked)
                look += new Vector2(Axis("Mouse X"), Axis("Mouse Y")) * mouseSensitivity;
            var stick = new Vector2(Axis("RightStickX"), -Axis("RightStickY"));
            if (stick.magnitude > DeadZone)
                look += stick * (stickSpeed * Time.unscaledDeltaTime);
            return look;
        }

        /// <summary>Lit un axe de l'Input Manager sans erreur s'il n'est pas déclaré dans le projet.</summary>
        static float Axis(string name)
        {
            if (missingAxes.Contains(name))
                return 0f;
            try { return Input.GetAxisRaw(name); }
            catch (System.ArgumentException)
            {
                missingAxes.Add(name);
                return 0f;
            }
        }

        static readonly System.Collections.Generic.HashSet<string> missingAxes = new System.Collections.Generic.HashSet<string>();
    }
}
