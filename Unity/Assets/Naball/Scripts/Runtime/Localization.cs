using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naball
{
    [Serializable]
    public class DialogLine
    {
        public string color = "Default";
        public string text = "";
    }

    [Serializable]
    public class DialogFile
    {
        public List<DialogLine> lines = new List<DialogLine>();
    }

    [Serializable]
    public class TextEntry
    {
        public string key, value;
    }

    [Serializable]
    public class TextFile
    {
        public List<TextEntry> entries = new List<TextEntry>();
    }

    /// <summary>Textes traduits (Resources/Lang/&lt;langue&gt;/*.json, convertis depuis Assets/lang/*.lg).</summary>
    public static class Localization
    {
        const string Fallback = "fr";

        static TextAsset Find(string file)
        {
            var lang = GameState.Data.language;
            return Resources.Load<TextAsset>($"Lang/{lang}/{file}") ?? Resources.Load<TextAsset>($"Lang/{Fallback}/{file}");
        }

        public static DialogFile Dialog(string name)
        {
            var asset = Find(name);
            if (asset == null)
            {
                Debug.LogWarning($"Dialogue introuvable : {name}");
                return null;
            }
            return JsonUtility.FromJson<DialogFile>(asset.text);
        }

        public static string Text(string file, string key, string fallback = "")
        {
            var asset = Find(file);
            if (asset == null)
                return fallback;
            foreach (var e in JsonUtility.FromJson<TextFile>(asset.text).entries)
                if (e.key == key)
                    return e.value;
            return fallback;
        }

        /// <summary>Couleurs du DialogsEngine 1.05 (dlgComponent.arrColor).</summary>
        public static Color DialogColor(string code)
        {
            switch (code)
            {
                case "G1": return Color.green;
                case "R": return Color.red;
                case "B1": return Color.blue;
                case "P": return Color.magenta;
                case "C": return Color.cyan;
                case "B2": return Color.black;
                case "G2": return new Color(0.1f, 0.1f, 0.1f);
                case "O": return new Color(1f, 0.25f, 0f);
                case "Y": return Color.yellow;
                default: return Color.white;
            }
        }
    }
}
