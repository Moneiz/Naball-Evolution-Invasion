using System.Collections.Generic;
using UnityEngine;

namespace Naball.EditorTools
{
    /// <summary>
    /// Maillages des pièces greybox, centrés sur leur milieu, avec des UV en mètres : la grille garde
    /// la même taille sur toutes les pièces, ce qui aide à lire les distances de saut.
    /// </summary>
    public static class GreyboxMeshes
    {
        public static Mesh Box(Vector3 size, float tile)
        {
            var h = size / 2f;
            var b = new Builder(tile);
            b.Quad(new Vector3(-h.x, h.y, -h.z), new Vector3(-h.x, h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(h.x, h.y, -h.z));      // dessus
            b.Quad(new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, -h.y, h.z));  // dessous
            b.Quad(new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(h.x, -h.y, -h.z));  // avant (−z)
            b.Quad(new Vector3(h.x, -h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z), new Vector3(-h.x, -h.y, h.z));      // arrière (+z)
            b.Quad(new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, h.y, h.z), new Vector3(-h.x, h.y, -h.z), new Vector3(-h.x, -h.y, -h.z));  // gauche
            b.Quad(new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(h.x, h.y, h.z), new Vector3(h.x, -h.y, h.z));      // droite
            return b.Build("Bloc");
        }

        /// <summary>Rampe qui monte de l'avant (−z, hauteur 0) vers l'arrière (+z, pleine hauteur).</summary>
        public static Mesh Wedge(Vector3 size, float tile)
        {
            var h = size / 2f;
            var b = new Builder(tile);
            b.Quad(new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(h.x, -h.y, -h.z));    // pente
            b.Quad(new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, -h.y, h.z));  // dessous
            b.Quad(new Vector3(h.x, -h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z), new Vector3(-h.x, -h.y, h.z));      // arrière
            b.Triangle(new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, h.y, h.z), new Vector3(-h.x, -h.y, -h.z));                             // côté gauche
            b.Triangle(new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, h.y, h.z), new Vector3(h.x, -h.y, h.z));                                // côté droit
            return b.Build("Rampe");
        }

        public static Mesh Cylinder(Vector3 size, float tile, int sides)
        {
            float rx = size.x / 2f, rz = size.z / 2f, hy = size.y / 2f;
            var b = new Builder(tile);
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                var p0 = new Vector3(Mathf.Cos(a0) * rx, 0f, Mathf.Sin(a0) * rz);
                var p1 = new Vector3(Mathf.Cos(a1) * rx, 0f, Mathf.Sin(a1) * rz);
                b.Quad(p0 + Vector3.down * hy, p0 + Vector3.up * hy, p1 + Vector3.up * hy, p1 + Vector3.down * hy);
                b.Triangle(Vector3.up * hy, p1 + Vector3.up * hy, p0 + Vector3.up * hy);
                b.Triangle(Vector3.down * hy, p0 + Vector3.down * hy, p1 + Vector3.down * hy);
            }
            return b.Build("Pilier");
        }

        /// <summary>Faces plates (sommets non partagés) avec UV projetés selon la normale, en mètres / tile.</summary>
        class Builder
        {
            readonly float tile;
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Vector3> normals = new List<Vector3>();
            readonly List<Vector2> uvs = new List<Vector2>();
            readonly List<int> triangles = new List<int>();

            public Builder(float tile) => this.tile = tile;

            // Sommets dans le sens horaire vu de l'extérieur (convention de Unity).
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int start = Add(Vector3.Cross(b - a, c - a).normalized, a, b, c, d);
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

            public void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int start = Add(Vector3.Cross(b - a, c - a).normalized, a, b, c);
                triangles.AddRange(new[] { start, start + 1, start + 2 });
            }

            int Add(Vector3 normal, params Vector3[] points)
            {
                int start = vertices.Count;
                var n = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
                foreach (var p in points)
                {
                    vertices.Add(p);
                    normals.Add(normal);
                    var uv = n.y >= n.x && n.y >= n.z ? new Vector2(p.x, p.z)
                           : n.x >= n.z ? new Vector2(p.z, p.y)
                           : new Vector2(p.x, p.y);
                    uvs.Add(uv / tile);
                }
                return start;
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                return mesh;
            }
        }
    }
}
