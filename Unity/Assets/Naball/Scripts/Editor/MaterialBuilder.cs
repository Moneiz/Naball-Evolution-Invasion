using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Naball.EditorTools
{
    /// <summary>
    /// Recrée les matériaux Blender Internal (couleur, texture, répétition, transparence, émission)
    /// en matériaux Standard, puis les branche sur les FBX à la place de ceux que Unity a générés.
    /// </summary>
    public static class MaterialBuilder
    {
        const string Root = "Assets/Naball/";
        const string Folder = Root + "Materials";

        public static Dictionary<string, Material> BuildAll()
        {
            var json = File.ReadAllText(Root + "Data/Materials.json");
            var data = JsonUtility.FromJson<MaterialsJson>(json);
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Naball", "Materials");

            var result = new Dictionary<string, Material>();
            foreach (var m in data.materials)
                result[m.name] = Build(m);
            AssetDatabase.SaveAssets();
            return result;
        }

        static Material Build(MaterialJson m)
        {
            var path = $"{Folder}/{Safe(m.name)}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var albedo = Texture(m.albedo, normalMap: false);
            bool cutout = albedo != null && (m.alphaFromTexture || (m.transparent && HasAlpha(m.albedo)));
            bool fade = !cutout && m.transparent && m.alpha > 0f && m.alpha < 1f;
            var shader = m.shadeless
                ? Shader.Find(albedo == null ? "Unlit/Color" : cutout ? "Unlit/Transparent Cutout" : "Unlit/Texture")
                : Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;

            var color = new Color(m.color[0], m.color[1], m.color[2], fade ? m.alpha : 1f);
            mat.color = albedo != null ? new Color(1f, 1f, 1f, color.a) : color;
            mat.mainTexture = albedo;
            mat.mainTextureScale = new Vector2(m.tiling[0], m.tiling[1]);
            mat.mainTextureOffset = new Vector2(m.offset[0], m.offset[1]);

            if (!m.shadeless)
            {
                mat.SetFloat("_Glossiness", 0.1f);
                var normal = Texture(m.normalMap, normalMap: true);
                if (normal != null)
                {
                    mat.SetTexture("_BumpMap", normal);
                    mat.EnableKeyword("_NORMALMAP");
                }
                if (m.emission > 0f)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    mat.SetColor("_EmissionColor", (albedo != null ? Color.white : color) * Mathf.Min(m.emission, 1f));
                    if (albedo != null)
                        mat.SetTexture("_EmissionMap", albedo);
                }
                SetBlendMode(mat, cutout ? 1 : fade ? 2 : 0);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static string Safe(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Replace('=', '_');
        }

        static bool HasAlpha(string file) => file.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase);

        static Texture2D Texture(string file, bool normalMap)
        {
            if (string.IsNullOrEmpty(file))
                return null;
            var path = Root + "Textures/" + file;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return null;
            var type = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            bool alpha = !normalMap && HasAlpha(file);
            if (importer.textureType != type || importer.alphaIsTransparency != alpha)
            {
                importer.textureType = type;
                importer.alphaIsTransparency = alpha;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>0 opaque, 1 découpe, 2 fondu : les réglages que fait l'inspecteur du shader Standard.</summary>
        static void SetBlendMode(Material mat, int mode)
        {
            mat.SetFloat("_Mode", mode);
            mat.SetOverrideTag("RenderType", mode == 0 ? "" : mode == 1 ? "TransparentCutout" : "Transparent");
            mat.SetInt("_SrcBlend", (int)(mode == 2 ? BlendMode.SrcAlpha : BlendMode.One));
            mat.SetInt("_DstBlend", (int)(mode == 2 ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            mat.SetInt("_ZWrite", mode == 2 ? 0 : 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            if (mode == 1)
                mat.EnableKeyword("_ALPHATEST_ON");
            if (mode == 2)
                mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = mode == 0 ? -1 : mode == 1 ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Transparent;
        }

        /// <summary>Remplace les matériaux importés d'un FBX par ceux construits ici (même nom).</summary>
        public static void Remap(string modelPath, Dictionary<string, Material> materials)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            bool changed = false;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is Material embedded && materials.TryGetValue(embedded.name, out var mat))
                {
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), mat);
                    changed = true;
                }
            }
            if (changed)
                importer.SaveAndReimport();
        }
    }
}
