using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Naball.EditorTools
{
    /// <summary>
    /// Construit le prefab de Lumka à partir de lumka.fbx (repris du prototype lumka-player) :
    /// matériaux, Animator Controller, capsule physique, contrôleur, bruits de pas et projectile.
    /// Tout est regénéré dans Assets/Lumka/Generated à chaque construction du niveau.
    /// </summary>
    public static class LumkaBuilder
    {
        const string Root = "Assets/Lumka/";
        const string Generated = Root + "Generated";
        const string Fbx = Root + "Models/lumka.fbx";

        // Matériaux du prototype : shader graph "lumka" (texture + émission de la même texture × facteur)
        // pour la fourrure et les yeux, URP Lit pour le reste. Recréés en Standard.
        struct MaterialSpec
        {
            public string texture;
            public Color color;
            public float emission;       // facteur appliqué à la texture
            public Color emissionColor;  // émission sans texture
            public float smoothness;
        }

        static readonly Dictionary<string, MaterialSpec> Materials = new Dictionary<string, MaterialSpec>
        {
            ["LumkaFourrure1"] = new MaterialSpec { texture = "LumkaFourrureWhite", color = Color.white, emission = 0.699f },
            ["LumkaFourrure2"] = new MaterialSpec { texture = "LumkaFourrureJaune", color = Color.white, emission = 0.477f },
            ["LumkaEye"] = new MaterialSpec { texture = "LumkaEye", color = Color.white, emission = 1f },
            ["LumkaMetal"] = new MaterialSpec { color = new Color(0.519f, 0.517f, 0.12f), emissionColor = new Color(0.098f, 0.098f, 0.006f), smoothness = 0.956f },
            ["LumkaTongue"] = new MaterialSpec { color = Color.white, smoothness = 0.5f },
            ["WhiteLumka"] = new MaterialSpec { color = Color.white, emissionColor = new Color(0.0625f, 0f, 0f) },
        };

        // Emplacements de matériaux du FBX (noms Blender) → matériau, comme dans le prefab lumka_root du prototype.
        static readonly (string slot, string material)[] Slots =
        {
            ("Fourrure1", "LumkaFourrure2"),
            ("Fourrure2", "LumkaFourrure1"),
            ("Blanc", "WhiteLumka"),
            ("Métal1", "LumkaMetal"),
            ("Métal2", "LumkaMetal"),
            ("Noir", "LumkaFourrure2"),
            ("Langue", "LumkaTongue"),
            ("Intérieur-bouche", "LumkaTongue"),
            ("Eye2.001", "LumkaEye"),
            ("Eye2", "LumkaEye"),
        };

        public static GameObject BuildPrefab()
        {
            if (!AssetDatabase.IsValidFolder(Generated))
                AssetDatabase.CreateFolder("Assets/Lumka", "Generated");

            var materials = Materials.ToDictionary(m => m.Key, m => BuildMaterial(m.Key, m.Value));
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
            var controller = BuildAnimator(model);
            var shot = BuildShotPrefab();

            var root = new GameObject("Lumka");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "Modele";
            instance.transform.SetParent(root.transform, false);

            // Matériaux, par nom d'emplacement (ordre du prototype sinon).
            var skin = instance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin != null)
            {
                var shared = skin.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    string slot = shared[i] != null ? shared[i].name : null;
                    int index = System.Array.FindIndex(Slots, s => s.slot == slot);
                    if (index < 0)
                        index = i < Slots.Length ? i : -1;
                    if (index >= 0)
                        shared[i] = materials[Slots[index].material];
                }
                skin.sharedMaterials = shared;
                skin.updateWhenOffscreen = true;  // les bornes suivent l'animation (dash, salto)
            }

            // Orientation : Lumka doit regarder vers +Z (les yeux devant les hanches).
            var hips = FindDeep(instance.transform, "Hips");
            var eye = FindDeep(instance.transform, "LeftEye");
            if (hips != null && eye != null && root.transform.InverseTransformPoint(eye.position).z < root.transform.InverseTransformPoint(hips.position).z)
                instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Pieds à l'origine du prefab, corps centré.
            var bounds = skin != null ? skin.bounds : new Bounds(Vector3.up, new Vector3(0.7f, 2f, 0.5f));
            instance.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            float bodyHeight = Mathf.Clamp(bounds.size.y * 0.85f, 1.2f, 2.2f);  // les oreilles dépassent de la capsule

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = 0.35f;
            capsule.height = bodyHeight;
            capsule.center = new Vector3(0f, bodyHeight / 2f, 0f);
            capsule.sharedMaterial = SlipperyMaterial();

            var body = root.AddComponent<Rigidbody>();
            body.mass = 1f;

            var audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0.5f;

            if (!instance.TryGetComponent<Animator>(out var animator))
                animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var events = instance.AddComponent<LumkaAnimationEvents>();
            events.source = audio;
            events.steps = Enumerable.Range(1, 4)
                .Select(i => AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}Audio/step_lumka{i}.wav"))
                .Where(c => c != null).ToArray();

            var muzzle = new GameObject("Tir").transform;
            muzzle.SetParent(root.transform, false);
            muzzle.localPosition = new Vector3(0f, bodyHeight * 0.6f, 0.55f);

            var lumka = root.AddComponent<LumkaController>();
            lumka.animator = animator;
            lumka.muzzle = muzzle;
            lumka.shotPrefab = shot;
            lumka.shotSound = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/lumka-tir.WAV");

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Generated}/Lumka.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static Transform FindDeep(Transform parent, string name) =>
            parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        static Material BuildMaterial(string name, MaterialSpec spec)
        {
            var path = $"{Generated}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = Shader.Find("Standard");
            var texture = spec.texture == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}Textures/{spec.texture}.png");
            mat.color = spec.color;
            mat.mainTexture = texture;
            mat.SetFloat("_Glossiness", spec.smoothness);
            var emission = texture != null ? Color.white * spec.emission : spec.emissionColor;
            if (emission.maxColorComponent > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", emission);
                mat.SetTexture("_EmissionMap", texture);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static PhysicsMaterial SlipperyMaterial()
        {
            // Sans frottement : Lumka ne reste pas collée aux murs en plein saut.
            var path = $"{Generated}/Glissant.physicMaterial";
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (mat == null)
            {
                mat = new PhysicsMaterial("Glissant");
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.dynamicFriction = 0f;
            mat.staticFriction = 0f;
            mat.frictionCombine = PhysicsMaterialCombine.Minimum;
            mat.bounciness = 0f;
            mat.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static AnimationClip Clip(IEnumerable<AnimationClip> clips, string name) =>
            clips.FirstOrDefault(c => c.name == "lumka_bones|" + name)
            ?? clips.FirstOrDefault(c => c.name.EndsWith(name));

        /// <summary>
        /// Animator piloté uniquement par LumkaController : Speed, Grounded, VerticalSpeed et quatre déclencheurs.
        /// Couche de base : course, saut, salto, chute, dash. Couche "Tir" limitée au haut du corps.
        /// </summary>
        static AnimatorController BuildAnimator(GameObject model)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToList();
            var idle = Clip(clips, "idle");
            var run = Clip(clips, "course_lumka_18 (1)");  // copie en boucle avec les événements Step
            var jump = Clip(clips, "saut_lumka_1-34");
            var flip = Clip(clips, "saltot1-26");
            var dash = Clip(clips, "dash");
            var shoot = Clip(clips, "shoot");

            var path = $"{Generated}/Lumka.controller";
            AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
            foreach (var trigger in new[] { "Jump", "DoubleJump", "Dash", "Shoot" })
                controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;

            var locomotion = controller.CreateBlendTreeInController("Locomotion", out var tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(run, 1f);
            sm.defaultState = locomotion;

            var jumpState = sm.AddState("Jump");
            jumpState.motion = jump;
            var flipState = sm.AddState("DoubleJump");
            flipState.motion = flip;
            var fall = sm.AddState("Fall");
            fall.motion = jump;       // pose figée de la fin du saut
            fall.speed = 0f;
            fall.cycleOffset = 0.7f;
            var dashState = sm.AddState("Dash");
            dashState.motion = dash;

            static void Cond(AnimatorStateTransition t, AnimatorConditionMode mode, float threshold, string parameter) =>
                t.AddCondition(mode, threshold, parameter);

            AnimatorStateTransition Any(AnimatorState to, string trigger)
            {
                var t = sm.AddAnyStateTransition(to);
                t.canTransitionToSelf = true;
                t.hasExitTime = false;
                t.duration = 0.05f;
                Cond(t, AnimatorConditionMode.If, 0f, trigger);
                return t;
            }
            Any(jumpState, "Jump");
            Any(flipState, "DoubleJump");
            Any(dashState, "Dash");

            AnimatorStateTransition Link(AnimatorState from, AnimatorState to, float duration, float? exitTime = null)
            {
                var t = from.AddTransition(to);
                t.hasExitTime = exitTime.HasValue;
                t.exitTime = exitTime ?? 0f;
                t.duration = duration;
                return t;
            }

            // Quitter le sol sans sauter (rebord) : chute.
            Cond(Link(locomotion, fall, 0.15f), AnimatorConditionMode.IfNot, 0f, "Grounded");
            // Atterrissage, seulement une fois la montée finie pour ne pas retomber en course au décollage.
            foreach (var air in new[] { jumpState, flipState, fall })
            {
                var land = Link(air, locomotion, 0.1f);
                Cond(land, AnimatorConditionMode.If, 0f, "Grounded");
                Cond(land, AnimatorConditionMode.Less, 0.5f, "VerticalSpeed");
            }
            Link(jumpState, fall, 0.2f, 0.95f);
            Link(flipState, fall, 0.15f, 0.95f);
            Cond(Link(dashState, locomotion, 0.1f, 0.9f), AnimatorConditionMode.If, 0f, "Grounded");
            Cond(Link(dashState, fall, 0.1f, 0.9f), AnimatorConditionMode.IfNot, 0f, "Grounded");

            // Couche du tir : haut du corps seulement, la course continue en dessous.
            var mask = UpperBodyMask(model);
            controller.AddLayer("Tir");
            var layers = controller.layers;
            layers[1].avatarMask = mask;
            layers[1].defaultWeight = 1f;
            layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers = layers;
            var shootSm = controller.layers[1].stateMachine;
            var empty = shootSm.AddState("Rien");
            empty.writeDefaultValues = false;  // l'état vide laisse passer la couche de base
            shootSm.defaultState = empty;
            var shootState = shootSm.AddState("Shoot");
            shootState.motion = shoot;
            var toShoot = shootSm.AddAnyStateTransition(shootState);
            toShoot.hasExitTime = false;
            toShoot.duration = 0.05f;
            toShoot.canTransitionToSelf = true;
            toShoot.AddCondition(AnimatorConditionMode.If, 0f, "Shoot");
            var back = shootState.AddTransition(empty);
            back.hasExitTime = true;
            back.exitTime = 0.9f;
            back.duration = 0.15f;

            AssetDatabase.SaveAssets();
            return controller;
        }

        static AvatarMask UpperBodyMask(GameObject model)
        {
            var path = $"{Generated}/HautDuCorps.mask";
            AssetDatabase.DeleteAsset(path);
            var mask = new AvatarMask();
            var transforms = model.GetComponentsInChildren<Transform>(true).Where(t => t != model.transform).ToList();
            mask.transformCount = transforms.Count;
            for (int i = 0; i < transforms.Count; i++)
            {
                var t = transforms[i];
                mask.SetTransformPath(i, AnimationUtility.CalculateTransformPath(t, model.transform));
                bool upper = false;
                for (var p = t; p != null && p != model.transform; p = p.parent)
                    upper |= p.name == "Spine1";
                mask.SetTransformActive(i, upper);
            }
            AssetDatabase.CreateAsset(mask, path);
            return mask;
        }

        static LumkaShot BuildShotPrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Tir de Lumka";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.3f;
            var color = new Color(0.45f, 0.85f, 1f);

            var matPath = $"{Generated}/Tir.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 3f);
            EditorUtility.SetDirty(mat);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.2f;
            trail.startWidth = 0.25f;
            trail.endWidth = 0f;
            trail.sharedMaterial = mat;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 5f;
            light.intensity = 2f;

            go.AddComponent<LumkaShot>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{Generated}/Tir.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<LumkaShot>();
        }
    }
}
