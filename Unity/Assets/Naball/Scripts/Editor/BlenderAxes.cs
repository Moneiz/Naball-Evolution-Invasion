using System.Collections.Generic;
using UnityEngine;

namespace Naball.EditorTools
{
    /// <summary>
    /// Conversion des coordonnées Blender (Z en haut, main droite) vers Unity (Y en haut, main gauche).
    /// Le FBX est exporté en "-Z avant, Y haut" puis Unity inverse X, ce qui donne (-x, z, -y).
    /// Plutôt que de le supposer, on vérifie sur les objets du niveau dont on connaît la position Blender.
    /// </summary>
    public struct BlenderAxes
    {
        public Vector3 sign;   // signes appliqués à (x, z, y)
        public float scale;
        public float error;    // écart moyen constaté, en unités Unity

        static readonly Vector3[] Candidates =
        {
            new Vector3(-1, 1, -1), new Vector3(1, 1, 1), new Vector3(1, 1, -1), new Vector3(-1, 1, 1),
        };

        public Vector3 Position(float[] b) => scale * new Vector3(sign.x * b[0], sign.y * b[2], sign.z * b[1]);

        /// <summary>
        /// Vitesse de rotation (axes locaux). Une rotation se transforme comme un pseudo-vecteur :
        /// quand la conversion inverse l'orientation du repère, son sens s'inverse aussi.
        /// </summary>
        public Vector3 Rotation(float[] b)
        {
            float handedness = sign.x * sign.y * sign.z * -1f; // l'échange y/z inverse déjà une fois
            return handedness * new Vector3(sign.x * b[0], sign.y * b[2], sign.z * b[1]);
        }

        public static BlenderAxes Calibrate(IEnumerable<(Vector3 unity, float[] blender)> pairs)
        {
            var list = new List<(Vector3 unity, float[] blender)>(pairs);
            var best = new BlenderAxes { sign = Candidates[0], scale = 1f, error = float.MaxValue };
            if (list.Count == 0)
                return best;
            foreach (var candidate in Candidates)
            {
                var axes = new BlenderAxes { sign = candidate, scale = 1f };
                float dot = 0f, norm = 0f;
                foreach (var (u, b) in list)
                {
                    var m = axes.Position(b);
                    dot += Vector3.Dot(u, m);
                    norm += m.sqrMagnitude;
                }
                axes.scale = norm > 0f ? dot / norm : 1f;
                float error = 0f;
                foreach (var (u, b) in list)
                    error += (u - axes.Position(b)).magnitude;
                axes.error = error / list.Count;
                if (axes.error < best.error)
                    best = axes;
            }
            return best;
        }
    }
}
