using UnityEditor;
using UnityEngine;

namespace Naball.EditorTools
{
    /// <summary>Réglages d'import des FBX exportés depuis le .blend.</summary>
    public class NaballImport : AssetPostprocessor
    {
        const string Models = "Assets/Naball/Models/";

        // Découpage de ArmatureAction.004 d'après les actuators Action de la boule.
        static readonly (string name, int first, int last, bool loop)[] NaballClips =
        {
            ("Idle", 0, 1, true),     // Action1 : frame 0
            ("Roll", 10, 30, true),   // flèche haut
            ("Jump", 40, 42, false),  // espace
            ("Boost", 60, 70, true),  // Always, état secondaire
        };

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Models))
                return;
            var importer = (ModelImporter)assetImporter;
            importer.importCameras = false;
            importer.importLights = false;
            importer.preserveHierarchy = true;  // racine = nom du fichier, objets Blender en enfants
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.animationType = assetPath.EndsWith("Naball.fbx")
                ? ModelImporterAnimationType.Legacy
                : ModelImporterAnimationType.None;
            importer.importAnimation = assetPath.EndsWith("Naball.fbx");
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.EndsWith(Models + "Naball.fbx"))
                return;
            var importer = (ModelImporter)assetImporter;
            var defaults = importer.defaultClipAnimations;
            if (defaults.Length == 0)
                return;
            var clips = new ModelImporterClipAnimation[NaballClips.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                var (name, first, last, loop) = NaballClips[i];
                clips[i] = new ModelImporterClipAnimation
                {
                    name = name,
                    takeName = defaults[0].takeName,
                    firstFrame = first,
                    lastFrame = last,
                    loopTime = loop,
                    wrapMode = loop ? WrapMode.Loop : WrapMode.Once,
                };
            }
            importer.clipAnimations = clips;
        }
    }
}
