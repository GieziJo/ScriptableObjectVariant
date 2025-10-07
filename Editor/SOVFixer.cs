using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Giezi.Tools
{
    public class SOVFixer
    {
        [MenuItem("Tools/GieziTools/SOVariant/Fix SOVs")]
        public static void FixSOVs()
        {
            IEnumerable<ScriptableObject> scriptableObjects =
                AssetDatabase.GetAllAssetPaths()
                    .Where(s => s.EndsWith(".asset") && s.StartsWith("Assets/"))
                    .Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
                    .Where(o => o != null)
                    .Where(o => o.GetType().IsDefined(typeof(SOVariantAttribute), true));

            var _library = SOVariantDataAccessor.SoVariantDataLibrary;
            Dictionary<ScriptableObject, SOVariantData> _localLibrary = new();
            foreach (ScriptableObject scriptableObject in scriptableObjects)
            {
                _localLibrary.Add(scriptableObject, _library.GetSOVariantDataForTarget(scriptableObject));
            }

            foreach (SOVariantData soVariantData in _localLibrary.Values)
            {
                soVariantData.Children = new List<ScriptableObject>();
            }

            foreach (KeyValuePair<ScriptableObject,SOVariantData> keyValuePair in _localLibrary)
            {
                if (keyValuePair.Value.Parent != null)
                {
                    _localLibrary[keyValuePair.Value.Parent].Children.Add(keyValuePair.Key);
                }
            }

            foreach (KeyValuePair<ScriptableObject,SOVariantData> keyValuePair in _localLibrary)
            {
                _library.WriteToLibrary(keyValuePair.Key, keyValuePair.Value.Parent,
                    keyValuePair.Value.Overridden, keyValuePair.Value.Children);
            }

            foreach (KeyValuePair<ScriptableObject,SOVariantData> keyValuePair in _localLibrary)
            {
                if (keyValuePair.Value.Children is { Count: > 0 })
                {
                    SOVariant<ScriptableObject> sov = new SOVariant<ScriptableObject>(keyValuePair.Key);
                    sov.SaveData(keyValuePair.Value.Overridden);
                }
            }
            
            EditorUtility.SetDirty(_library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}