using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Giezi.Tools
{
    public static class SOContextMenu
    {
        
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
            
            SOVariantHelper<ScriptableObject>.SetParent(newAsset, (ScriptableObject)activeObject);
            
            AssetDatabase.SaveAssets();
        }
        
        [MenuItem("Assets/Create/Create SO Variant", true)]
        static bool ValidateCreateSOVariant()
        {
            return (Selection.activeObject is ScriptableObject &&
                    Selection.activeObject.GetType().IsDefined(typeof(SOVariantAttribute), true));
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