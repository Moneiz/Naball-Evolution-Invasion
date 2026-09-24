using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Naball.EditorTools
{
    /// <summary>
    /// Construit la scène Unity d'un niveau à partir du FBX et de Data/&lt;scène&gt;.level.json.
    /// Menu : Naball &gt; Construire Ger_FieldSwamp. Lancé tout seul à la première ouverture du projet.
    /// </summary>
    [InitializeOnLoad]
    public static class LevelBuilder
    {
        const string Root = "Assets/Naball/";
        const string LevelName = "Ger_FieldSwamp";
        const float SkyThreshold = 800f;  // Ger.back / Ger.backSpa : sphères de ciel, pas de collision

        static LevelBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath(LevelName)) && File.Exists(Root + "Data/" + LevelName + ".level.json"))
                    Build(LevelName);
            };
        }

        static string ScenePath(string level) => $"{Root}Scenes/{level}.unity";

        [MenuItem("Naball/Construire Ger_FieldSwamp")]
        static void BuildGer() => Build(LevelName);

        public static void Build(string levelName)
        {
            var level = JsonUtility.FromJson<LevelJson>(File.ReadAllText($"{Root}Data/{levelName}.level.json"));

            var materials = MaterialBuilder.BuildAll();
            foreach (var model in new[] { levelName, "Naball", "Clim_white" })
                MaterialBuilder.Remap($"{Root}Models/{model}.fbx", materials);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Niveau
            var levelRoot = Instantiate(levelName);
            var byName = new Dictionary<string, Transform>();
            foreach (var t in levelRoot.GetComponentsInChildren<Transform>(true))
                if (!byName.ContainsKey(t.name))
                    byName[t.name] = t;
            Transform Find(string name) => name != null && byName.TryGetValue(name, out var t) ? t : null;

            var axes = BlenderAxes.Calibrate(
                from r in level.references
                let t = Find(r.objectName)
                where t != null && t.parent == levelRoot.transform
                select (t.position, r.blenderPosition));
            Debug.Log($"[Naball] Axes Blender → Unity : signes {axes.sign}, échelle {axes.scale:0.###}, écart moyen {axes.error:0.###}");
            if (axes.error > 0.1f)
                Debug.LogWarning("[Naball] La conversion d'axes ne colle pas aux positions du FBX : vérifier les points d'apparition.");

            foreach (var name in level.hidden)
                if (Find(name) is Transform t && t.TryGetComponent<MeshRenderer>(out var r))
                    r.enabled = false;

            foreach (var p in level.physics)
            {
                var t = Find(p.objectName);
                if (t == null || p.bodyType == 0 || p.ghost || !t.TryGetComponent<MeshFilter>(out var mf) || mf.sharedMesh == null)
                    continue;
                if (t.TryGetComponent<Renderer>(out var r) && r.bounds.extents.magnitude > SkyThreshold)
                    continue;
                t.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                t.gameObject.isStatic = true;
            }

            // Joueur, caméra, lumière, systèmes
            var playerPrefab = BuildPlayerPrefab();
            var player = ((GameObject)PrefabUtility.InstantiatePrefab(playerPrefab)).GetComponent<NaballController>();

            var cameraGo = new GameObject("Camera", typeof(Camera), typeof(AudioListener), typeof(FollowCamera));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.farClipPlane = 6000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var follow = cameraGo.GetComponent<FollowCamera>();
            follow.target = player.transform;

            var sunGo = new GameObject("Soleil", typeof(Light));
            var sun = sunGo.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);
            RenderSettings.skybox = null;

            var game = new GameObject("Jeu");
            var hud = game.AddComponent<Hud>();
            hud.climSound = Clip("Magic61.wav");
            game.AddComponent<DialogSystem>();
            var director = game.AddComponent<LevelDirector>();
            director.player = player;
            director.followCamera = follow;
            director.sun = sun;

            // Clims
            var climPrefab = BuildClimPrefab(axes.Rotation(level.climSpin));
            foreach (var c in level.clims)
            {
                var t = Find(c.objectName);
                if (t == null)
                    continue;
                var spawner = t.gameObject.AddComponent<ClimSpawner>();
                spawner.climId = c.objectName;
                spawner.climPrefab = climPrefab;
                spawner.collectRadius = c.collectRadius;
                spawner.magnetSpeed = c.magnetSpeed;
            }

            // Portails
            foreach (var p in level.portals)
            {
                var t = Find(p.objectName);
                if (t == null)
                    continue;
                var portal = t.gameObject.AddComponent<Portal>();
                portal.targetScene = p.targetScene;
                portal.requiredProgress = p.requiredProgress;
                portal.locked = p.locked;
                portal.delay = p.delay;
                portal.loadingImage = p.loadingImage;
                portal.indicator = Find(p.indicatorName);
                portal.indicatorRadiansPerSecond = axes.Rotation(new[] { 0f, 0f, 0.0873f * 60f });
            }

            // Sons
            foreach (var s in level.soundFields)
            {
                var t = Find(s.objectName);
                var clip = Clip(s.clip);
                if (t == null || clip == null)
                    continue;
                var source = t.gameObject.AddComponent<AudioSource>();
                source.clip = clip;
                var field = t.gameObject.AddComponent<SoundField>();
                field.radius = s.radius;
                field.listener = player.transform;
            }
            foreach (var s in level.ambientSounds)
            {
                var t = Find(s.objectName);
                var clip = Clip(s.clip);
                if (t == null || clip == null)
                    continue;
                var source = t.gameObject.AddComponent<AudioSource>();
                source.clip = clip;
                source.loop = true;
                source.playOnAwake = true;
                source.volume = s.volume;
                bool isGround = t.TryGetComponent<Renderer>(out var r) && r.bounds.extents.magnitude > 100f;
                source.spatialBlend = isGround ? 0f : 1f;
                source.minDistance = 5f;
                source.maxDistance = 80f;
            }

            // Dialogues, rotations
            foreach (var d in level.dialogTriggers)
                if (Find(d.objectName) is Transform t)
                    t.gameObject.AddComponent<DialogTrigger>().dialog = d.dialog;
            director.welcomeTrigger = Find("Ger.normal")?.GetComponent<DialogTrigger>();
            var indicators = new HashSet<string>(level.portals.Select(p => p.indicatorName));
            foreach (var r in level.rotators)
                if (!indicators.Contains(r.objectName) && Find(r.objectName) is Transform t)
                    t.gameObject.AddComponent<Rotator>().radiansPerSecond = axes.Rotation(r.radiansPerSecond);

            // Avancement du hub : points d'apparition et positions de caméra
            var progressRoot = new GameObject("Avancement").transform;
            foreach (var p in level.progress)
            {
                var spawn = new GameObject($"Apparition {p.progress}").transform;
                spawn.SetParent(progressRoot);
                spawn.position = axes.Position(p.playerSpawn);
                var cam = new GameObject($"Camera {p.progress}").transform;
                cam.SetParent(progressRoot);
                cam.position = axes.Position(p.cameraPosition);
                director.states.Add(new LevelDirector.ProgressState
                {
                    progress = p.progress,
                    playerSpawn = spawn,
                    cameraStart = cam,
                    welcomeDialog = p.welcomeDialog,
                    // Ger.lightVariation : énergie du soleil selon l'avancement
                    sunIntensity = p.progress == 1 ? 0.1f : p.progress == 2 ? 0.2f : p.progress == 3 ? 1.5f : -1f,
                });
            }
            if (director.states.Count > 0)
            {
                player.transform.position = director.states[0].playerSpawn.position;
                player.respawnPoint = director.states[0].playerSpawn;
                cameraGo.transform.position = director.states[0].cameraStart.position;
            }

            Directory.CreateDirectory(Root + "Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath(levelName));
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(s => s.path != ScenePath(levelName)))
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath(levelName), true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            Debug.Log($"[Naball] Scène {ScenePath(levelName)} construite : {level.clims.Length} Clims, {level.portals.Length} portails.");
        }

        static GameObject Instantiate(string model)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}Models/{model}.fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            return go;
        }

        static AudioClip Clip(string file) =>
            string.IsNullOrEmpty(file) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/" + file);

        static void SavePrefab(GameObject go, string name, out GameObject prefab)
        {
            Directory.CreateDirectory(Root + "Prefabs");
            prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{Root}Prefabs/{name}.prefab");
            Object.DestroyImmediate(go);
        }

        /// <summary>La boule : collision en boîte (Cube, invisible), peau animée, contrôleur.</summary>
        static GameObject BuildPlayerPrefab()
        {
            var go = Instantiate("Naball");
            go.name = "Naball";
            var cube = go.transform.Find("Cube");
            var bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            if (cube != null && cube.TryGetComponent<MeshRenderer>(out var cubeRenderer))
            {
                bounds = cubeRenderer.bounds;
                cubeRenderer.enabled = false;
            }
            var box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(bounds.center);
            box.size = bounds.size;
            var body = go.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.linearDamping = 0.04f;
            body.angularDamping = 1f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var controller = go.AddComponent<NaballController>();
            controller.skin = go.GetComponentInChildren<Animation>();
            // Effets rattachés à la boule dans l'original (Empty.004, ger.eta) : sans rôle ici.
            SavePrefab(go, "Naball", out var prefab);
            return prefab;
        }

        static GameObject BuildClimPrefab(Vector3 spin)
        {
            var go = Instantiate("Clim_white");
            go.name = "Clim";
            float radius = 1f;
            var renderer = go.GetComponentInChildren<Renderer>();
            if (renderer != null)
                radius = Mathf.Max(1f, renderer.bounds.extents.magnitude);
            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = radius;
            go.AddComponent<Rigidbody>().isKinematic = true;
            go.AddComponent<ClimPickup>();
            var model = go.transform.childCount > 0 ? go.transform.GetChild(0).gameObject : go;
            model.AddComponent<Rotator>().radiansPerSecond = spin;
            SavePrefab(go, "Clim", out var prefab);
            return prefab;
        }
    }
}
