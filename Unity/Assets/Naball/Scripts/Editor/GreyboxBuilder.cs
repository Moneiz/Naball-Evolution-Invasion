using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Naball.EditorTools
{
    /// <summary>
    /// Construit une scène jouable en blocs gris (greybox) à partir d'un fichier de salle
    /// Assets/Naball/Greybox/&lt;nom&gt;.json : pièces, objets 4D, fruits, Clims, Atomiums, points de contrôle.
    /// On décrit la salle en texte, on la joue, on corrige le texte, on reconstruit : le décor définitif
    /// ne vient qu'une fois le parcours validé.
    /// Menu : Naball &gt; Construire les salles greybox.
    /// </summary>
    public static class GreyboxBuilder
    {
        const string Folder = "Assets/Naball/Greybox";
        const string Generated = Folder + "/Generated";
        const string Scenes = "Assets/Naball/Scenes/Greybox";
        const float Tile = 2f;  // la grille des textures fait 2 m, la taille des futures pièces de kit

        [MenuItem("Naball/Construire les salles greybox")]
        public static void BuildAll()
        {
            var files = Directory.GetFiles(Folder, "*.json");
            foreach (var file in files)
                Build(file.Replace('\\', '/'));
            Debug.Log($"[Naball] {files.Length} salle(s) greybox construite(s) dans {Scenes}.");
        }

        public static void Build(string jsonPath)
        {
            var name = Path.GetFileNameWithoutExtension(jsonPath);
            var room = JsonUtility.FromJson<GreyboxJson>(File.ReadAllText(jsonPath));
            EnsureFolder(Generated);
            EnsureFolder(Scenes);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(name).transform;

            // Systèmes
            var game = new GameObject("Jeu");
            game.AddComponent<Hud>();
            game.AddComponent<DialogSystem>();
            game.AddComponent<DimensionTint>();
            var dimensions = game.AddComponent<DimensionSystem>();
            dimensions.startDimension = Dimension(room.dimensionDepart);
            dimensions.shiftDuration = room.dureeBascule > 0f ? room.dureeBascule : 0.5f;

            var sunGo = new GameObject("Soleil", typeof(Light));
            var sun = sunGo.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);

            // Lumka et caméra
            var player = ((GameObject)PrefabUtility.InstantiatePrefab(LumkaBuilder.BuildPrefab())).GetComponent<LumkaController>();
            var cameraGo = new GameObject("Camera", typeof(Camera), typeof(AudioListener), typeof(FollowCamera));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
            camera.farClipPlane = 1000f;
            var follow = cameraGo.GetComponent<FollowCamera>();
            follow.target = player.transform;
            player.cameraTransform = cameraGo.transform;
            player.fallLimit = -20f;

            var spawn = new GameObject("Apparition").transform;
            spawn.SetParent(root, false);
            spawn.SetPositionAndRotation(V(room.apparition, Vector3.up), Quaternion.Euler(0f, room.orientation, 0f));
            player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

            var director = game.AddComponent<GreyboxDirector>();
            director.player = player;
            director.followCamera = follow;
            director.spawn = spawn;
            director.startPowers = room.pouvoirs ?? new string[0];
            director.title = room.titre;

            // Pièces
            var pieces = new GameObject("Pieces").transform;
            pieces.SetParent(root, false);
            int index = 0;
            foreach (var p in room.pieces)
                BuildPiece(p, pieces, index++);

            var objects = new GameObject("Objets").transform;
            objects.SetParent(root, false);

            foreach (var f in room.fruits)
            {
                float d = Dimension(f.vers);
                var go = Sphere("Fruit 4D " + f.vers, objects, V(f.position, Vector3.zero), 0.6f, Emissive(DimensionSystem.ColorOf(d), 1.2f));
                go.AddComponent<Fruit4D>().dimension = d;
            }

            int climIndex = 0;
            foreach (var c in room.clims)
            {
                var go = Sphere($"Clim {climIndex + 1}", objects, V(c.position, Vector3.zero), 0.5f, Emissive(new Color(0.3f, 0.8f, 1f), 2f));
                go.GetComponent<SphereCollider>().radius = 1.2f;  // un peu plus large que la sphère : plus facile à ramasser
                go.AddComponent<Rigidbody>().isKinematic = true;
                go.AddComponent<ClimPickup>().climId = $"greybox.{name}.clim{++climIndex}";
                go.AddComponent<Rotator>().radiansPerSecond = new Vector3(0f, 2f, 0f);
                Dimensional(go, c.dimensions, null, null, null, null, null, null);
            }

            foreach (var a in room.atomiums)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Atomium " + a.pouvoir;
                go.transform.SetParent(objects, false);
                go.transform.position = V(a.position, Vector3.zero);
                go.transform.localScale = Vector3.one * 0.7f;
                go.GetComponent<Renderer>().sharedMaterial = Emissive(new Color(1f, 0.85f, 0.3f), 1.5f);
                var atomium = go.AddComponent<Atomium>();
                atomium.power = a.pouvoir;
                atomium.message = a.message;
            }

            foreach (var c in room.controles)
            {
                var go = new GameObject("Point de controle", typeof(BoxCollider), typeof(Checkpoint));
                go.transform.SetParent(objects, false);
                go.transform.position = V(c.position, Vector3.zero);
                go.GetComponent<BoxCollider>().size = new Vector3(8f, 4f, 2f);
                go.GetComponent<BoxCollider>().center = new Vector3(0f, 2f, 0f);
            }

            foreach (var s in room.panneaux)
                Sign(s.texte, objects, V(s.position, Vector3.zero));

            if (room.fin != null && room.fin.position != null && room.fin.position.Length >= 3)
            {
                var go = new GameObject("Fin", typeof(BoxCollider), typeof(RoomExit));
                go.transform.SetParent(objects, false);
                go.transform.position = V(room.fin.position, Vector3.zero);
                go.GetComponent<BoxCollider>().size = new Vector3(6f, 4f, 2f);
                go.GetComponent<BoxCollider>().center = new Vector3(0f, 2f, 0f);
                if (!string.IsNullOrEmpty(room.fin.texte))
                    go.GetComponent<RoomExit>().message = room.fin.texte;
                Sign("FIN", objects, go.transform.position + Vector3.up * 3f);
            }

            var path = $"{Scenes}/{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            var list = EditorBuildSettings.scenes.ToList();
            if (list.All(s => s.path != path))
            {
                list.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = list.ToArray();
            }
        }

        static void BuildPiece(GreyboxPiece p, Transform parent, int index)
        {
            var size = V(p.taille, new Vector3(2f, 1f, 2f));
            var rotation = Quaternion.Euler(V(p.rotation, Vector3.zero));
            var go = new GameObject(string.IsNullOrEmpty(p.nom) ? $"{p.forme} {index + 1}" : p.nom);
            go.transform.SetParent(parent, false);
            // "position" désigne le centre du dessous ; l'objet tourne autour de son centre.
            go.transform.SetPositionAndRotation(V(p.position, Vector3.zero) + rotation * new Vector3(0f, size.y / 2f, 0f), rotation);

            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            switch (p.forme)
            {
                case "rampe":
                    filter.sharedMesh = GreyboxMeshes.Wedge(size, Tile);
                    var mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = filter.sharedMesh;
                    mc.convex = true;
                    break;
                case "pilier":
                    filter.sharedMesh = GreyboxMeshes.Cylinder(size, Tile, 20);
                    var cc = go.AddComponent<CapsuleCollider>();
                    cc.radius = Mathf.Max(size.x, size.z) / 2f;
                    cc.height = Mathf.Max(size.y, cc.radius * 2f);
                    break;
                default:
                    filter.sharedMesh = GreyboxMeshes.Box(size, Tile);
                    go.AddComponent<BoxCollider>().size = size;
                    break;
            }

            var membership = Membership(p.dimensions);
            renderer.sharedMaterial = SurfaceMaterial(p.surface, membership);
            if (p.surface == "lave")
                go.AddComponent<Hazard>();

            bool dimensional = Dimensional(go, p.dimensions, p.bleu, p.neutre, p.rouge, p.rotationBleu, p.rotationNeutre, p.rotationRouge);
            go.isStatic = !dimensional;
        }

        /// <summary>Ajoute un DimensionalObject si la description le demande. Renvoie true si l'objet change avec la dimension.</summary>
        static bool Dimensional(GameObject go, string dimensions, float[] blue, float[] neutral, float[] red,
            float[] rotBlue, float[] rotNeutral, float[] rotRed)
        {
            var (inBlue, inNeutral, inRed) = Membership(dimensions);
            bool moves = new[] { blue, neutral, red, rotBlue, rotNeutral, rotRed }.Any(a => a != null && a.Length >= 3);
            if (inBlue && inNeutral && inRed && !moves)
                return false;
            var d = go.AddComponent<DimensionalObject>();
            d.inBlue = inBlue;
            d.inNeutral = inNeutral;
            d.inRed = inRed;
            d.blueOffset = V(blue, Vector3.zero);
            d.neutralOffset = V(neutral, Vector3.zero);
            d.redOffset = V(red, Vector3.zero);
            d.blueRotation = V(rotBlue, Vector3.zero);
            d.neutralRotation = V(rotNeutral, Vector3.zero);
            d.redRotation = V(rotRed, Vector3.zero);
            return true;
        }

        static (bool blue, bool neutral, bool red) Membership(string dimensions)
        {
            if (string.IsNullOrWhiteSpace(dimensions))
                return (true, true, true);
            var parts = dimensions.Split(',').Select(s => s.Trim().ToLowerInvariant()).ToArray();
            return (parts.Contains("bleu"), parts.Contains("neutre"), parts.Contains("rouge"));
        }

        static float Dimension(string name) => name switch
        {
            "bleu" => DimensionSystem.Blue,
            "rouge" => DimensionSystem.Red,
            _ => DimensionSystem.Neutral,
        };

        static Vector3 V(float[] a, Vector3 fallback) =>
            a != null && a.Length >= 3 ? new Vector3(a[0], a[1], a[2]) : fallback;

        // Matériaux ------------------------------------------------------------------------------

        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        static Material SurfaceMaterial(string surface, (bool blue, bool neutral, bool red) m)
        {
            if (surface == "lave")
                return Emissive(new Color(1f, 0.35f, 0.05f), 1.5f);
            // Couleur de base selon la surface, teintée selon les dimensions où la pièce existe.
            var color = surface == "mur" ? new Color(0.5f, 0.5f, 0.53f) : new Color(0.68f, 0.68f, 0.7f);
            if (m.blue && !m.red)
                color = Color.Lerp(color, new Color(0.3f, 0.55f, 1f), m.neutral ? 0.35f : 0.65f);
            else if (m.red && !m.blue)
                color = Color.Lerp(color, new Color(1f, 0.4f, 0.3f), m.neutral ? 0.35f : 0.65f);
            return Grid($"{surface}_{(m.blue ? "B" : "")}{(m.neutral ? "N" : "")}{(m.red ? "R" : "")}", color);
        }

        static Material Grid(string key, Color color)
        {
            if (materials.TryGetValue(key, out var cached) && cached != null)
                return cached;
            var path = $"{Generated}/{key}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            mat.mainTexture = GridTexture();
            mat.SetFloat("_Glossiness", 0.15f);
            EditorUtility.SetDirty(mat);
            return materials[key] = mat;
        }

        static Material Emissive(Color color, float intensity)
        {
            string key = $"emissif_{ColorUtility.ToHtmlStringRGB(color)}";
            if (materials.TryGetValue(key, out var cached) && cached != null)
                return cached;
            var path = $"{Generated}/{key}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            EditorUtility.SetDirty(mat);
            return materials[key] = mat;
        }

        static Texture2D GridTexture()
        {
            var path = $"{Generated}/Grille.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
                return texture;
            // Carreau de 2 m : fond clair, lignes fines tous les 1 m, ligne marquée au bord.
            const int n = 128;
            texture = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "Grille", wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    bool edge = x < 2 || y < 2;
                    bool half = Mathf.Abs(x - n / 2) < 1 || Mathf.Abs(y - n / 2) < 1;
                    float v = edge ? 0.55f : half ? 0.8f : 1f;
                    texture.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            texture.Apply(true);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        // Objets simples -------------------------------------------------------------------------

        static GameObject Sphere(string name, Transform parent, Vector3 position, float diameter, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static void Sign(string text, Transform parent, Vector3 position)
        {
            if (string.IsNullOrEmpty(text))
                return;
            var go = new GameObject("Panneau", typeof(TextMesh));
            go.transform.SetParent(parent, false);
            go.transform.position = position + Vector3.up * 2.2f;
            var mesh = go.GetComponent<TextMesh>();
            mesh.text = text;
            mesh.font = Ui.Font;
            mesh.GetComponent<MeshRenderer>().sharedMaterial = Ui.Font.material;
            mesh.fontSize = 48;
            mesh.characterSize = 0.06f;
            mesh.anchor = TextAnchor.LowerCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
            // Rotation nulle : le texte se lit en regardant vers +z, le sens de progression des salles.
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
