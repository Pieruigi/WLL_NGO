using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WLL_NGO.AI.Scriptables;

public class AssetBuilder : MonoBehaviour
{

    public const string ResourceFolder = "Assets/Resources";

    [MenuItem("Assets/Create/WLL_NGO/Action")]
    public static void CreateActionAI()
    {
        ActionAsset asset = ScriptableObject.CreateInstance<ActionAsset>();

        string name = "Action.asset";

        string folder = System.IO.Path.Combine(ResourceFolder, ActionAsset.ResourceFolder);

        CreateAsset(asset, name, folder);
    }

    static void CreateAsset(ScriptableObject asset, string name, string folder)
    {
        if (!System.IO.Directory.Exists(folder))
            System.IO.Directory.CreateDirectory(folder);

        AssetDatabase.CreateAsset(asset, System.IO.Path.Combine(folder, name));

        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();

        Selection.activeObject = asset;
    }
}
