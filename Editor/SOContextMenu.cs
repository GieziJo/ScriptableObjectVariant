using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Giezi.Tools
{
    public static class SOContextMenu
    {
        private static SOData _soData = null;

        public static SOData SOData
        {
            get
            {
                if (_soData == null)
                {
                    Debug.Log(AssetDatabase.GUIDToAssetPath("50b31bd74d19a40b293ceaeefd1c650e"));
                    _soData = AssetDatabase.LoadAssetAtPath<SOData>(
                        AssetDatabase.GUIDToAssetPath("50b31bd74d19a40b293ceaeefd1c650e"));
                    Debug.Log(_soData);
                }
                return _soData;
            }
        }
        
        // private const string GieziToolsCreateSOvariantChildKey = "Giezi.Tools.CreateSoVariant.Child";
        // private const string GieziToolsCreateSOvariantParentKey = "Giezi.Tools.CreateSoVariant.Parent";

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
            
            // PlayerPrefs.SetString(GieziToolsCreateSOvariantChildKey, newAssetPath);
            // PlayerPrefs.SetString(GieziToolsCreateSOvariantParentKey, assetPath);
            SOData.SOVariantCreationData = new SOVariantCreationData((ScriptableObject) activeObject, newAsset);
            
            EditorUtility.SetDirty(SOData);
            
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
            if(SOData.SOVariantCreationData == null || !SOData.SOVariantCreationData.CreateSoVariant)
                return;
            
            if(SOData.SOVariantCreationData.Child == null)
                return;

            ScriptableObject parent = SOData.SOVariantCreationData.Parent;
            ScriptableObject child = SOData.SOVariantCreationData.Child;
            
            
            SOVariantHelper<ScriptableObject>.SetParent(child, parent);

            SOData.SOVariantCreationData = null;
            
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