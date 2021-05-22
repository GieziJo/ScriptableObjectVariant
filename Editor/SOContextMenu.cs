using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Giezi.Tools
{
    public static class SOContextMenu
    {
        private const string GieziToolsCreateSOvariantChildKey = "Giezi.Tools.CreateSoVariant.Child";
        private const string GieziToolsCreateSOvariantParentKey = "Giezi.Tools.CreateSoVariant.Parent";

        [MenuItem("Assets/Create/Create SO Variant", false, 2000)]
        static void CreateSOVariant()
        {
            Object activeObject = Selection.activeObject;
            Type soType = activeObject.GetType();
            
            string assetPath = AssetDatabase.GetAssetPath(activeObject);
            string newAssetPath = GetNewAssetName(assetPath);

            ScriptableObject newAsset = ScriptableObject.CreateInstance(soType);
            
            AssetDatabase.CreateAsset(newAsset, newAssetPath);
            EditorUtility.SetDirty(newAsset);
            
            PlayerPrefs.SetString(GieziToolsCreateSOvariantChildKey, newAssetPath);
            PlayerPrefs.SetString(GieziToolsCreateSOvariantParentKey, assetPath);
            
            AssetDatabase.SaveAssets();
            
            AssetDatabase.Refresh();
            EditorUtility.RequestScriptReload();
        }

        [MenuItem("Assets/Create/Create SO Variant", true)]
        static bool ValidateCreateSOVariant()
        {
            return (Selection.activeObject is ScriptableObject &&
                    Selection.activeObject.GetType().IsDefined(typeof(SOVariantAttribute), true));
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void SetParentAfterReload()
        {
            if (!PlayerPrefs.HasKey(GieziToolsCreateSOvariantChildKey))
                return;
            
            if(AssetImporter.GetAtPath(PlayerPrefs.GetString(GieziToolsCreateSOvariantChildKey)) == null)
            {
                Debug.Log("<color=orange>SOVariant: </color>Asset Importer does not exist yet, waiting for Unity to reload.");
                return;
            }
            
            ScriptableObject parent =
                AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    PlayerPrefs.GetString(GieziToolsCreateSOvariantParentKey));
            
            ScriptableObject child =
                AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    PlayerPrefs.GetString(GieziToolsCreateSOvariantChildKey));
            
            SOVariantHelper<ScriptableObject>.SetParent(child, parent);
            
            PlayerPrefs.DeleteKey(GieziToolsCreateSOvariantParentKey);
            PlayerPrefs.DeleteKey(GieziToolsCreateSOvariantChildKey);
            
            Debug.Log($"<color=orange>SOVariant: </color>Created new SO Variant {child.name} with parent.");
        }

        private static string GetNewAssetName(string assetPath)
        {
            string newAssetPath = Path.Combine(Path.GetDirectoryName(assetPath),
                Path.GetFileNameWithoutExtension(assetPath) + " Variant" + Path.GetExtension(assetPath));

            int counter = 0;

            while (AssetDatabase.LoadAssetAtPath<ScriptableObject>(newAssetPath) != null)
            {
                counter++;
                newAssetPath = Path.Combine(Path.GetDirectoryName(assetPath),
                    Path.GetFileNameWithoutExtension(assetPath) + " Variant_" + counter + Path.GetExtension(assetPath));
            }

            return newAssetPath;
        }
    }
}