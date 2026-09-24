using System;

namespace Naball.EditorTools
{
    // Format de Data/*.level.json et Data/Materials.json, écrits par Unity/Tools/build_unity_data.py.
    // Les positions et rotations sont en axes Blender (Z vers le haut) : voir BlenderAxes.

    [Serializable]
    public class LevelJson
    {
        public string scene;
        public string[] hidden;
        public ClimJson[] clims;
        public PortalJson[] portals;
        public SoundFieldJson[] soundFields;
        public AmbientJson[] ambientSounds;
        public DialogTriggerJson[] dialogTriggers;
        public RotatorJson[] rotators;
        public ProgressJson[] progress;
        public PhysicsJson[] physics;
        public ReferenceJson[] references;
        public float[] climSpin;
    }

    [Serializable]
    public class ClimJson
    {
        public string objectName, id;
        public float collectRadius, magnetSpeed;
    }

    [Serializable]
    public class PortalJson
    {
        public string objectName, targetScene, loadingImage, indicatorName;
        public int requiredProgress;
        public bool locked;
        public float delay;
    }

    [Serializable]
    public class SoundFieldJson
    {
        public string objectName, clip;
        public float radius;
    }

    [Serializable]
    public class AmbientJson
    {
        public string objectName, clip;
        public float volume;
    }

    [Serializable]
    public class DialogTriggerJson
    {
        public string objectName, dialog;
    }

    [Serializable]
    public class RotatorJson
    {
        public string objectName;
        public float[] radiansPerSecond;
        public bool local;
    }

    [Serializable]
    public class ProgressJson
    {
        public int progress;
        public float[] playerSpawn, cameraPosition;
        public string welcomeDialog;
    }

    [Serializable]
    public class PhysicsJson
    {
        public string objectName;
        public int bodyType, bound;
        public bool ghost;
    }

    [Serializable]
    public class ReferenceJson
    {
        public string objectName;
        public float[] blenderPosition;
    }

    [Serializable]
    public class MaterialsJson
    {
        public MaterialJson[] materials;
    }

    [Serializable]
    public class MaterialJson
    {
        public string name, albedo, normalMap;
        public float[] color, tiling, offset;
        public float alpha, emission;
        public bool shadeless, transparent, alphaFromTexture;
    }
}
