using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Giezi.Tools
{
    
    public class UpgradeSOVariantHelper
    {
        [MenuItem("Tools/GieziTools/SOVariant/Upgrade user data to new version")]
        public static void UpgradeSOVariantUserData()
        {
            IEnumerable<ScriptableObject> scriptableObjects =
                AssetDatabase.GetAllAssetPaths()
                    .Where(s => s.EndsWith(".asset") && s.StartsWith("Assets/"))
                    .Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
                    .Where(o => o != null)
                    .Where(o => o.GetType().IsDefined(typeof(SOVariantAttribute), true));

            foreach (ScriptableObject scriptableObject in scriptableObjects)
            {
                AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(scriptableObject));
                
                var extractedData = ExtractData(importer.userData);
                var _parent = extractedData.Item1;
                if(_parent == null)
                    continue;
                var _overridden = extractedData.Item3 ?? new List<string>();
                var _children = extractedData.Item4 ?? new List<string>();

                string data = JsonConvert.SerializeObject(new Dictionary<string, string>()
                {
                    {"parentGUID", _parent},
                    {"overriddenFields", JsonConvert.SerializeObject(_overridden)},
                    {"childrenGUIDs", JsonConvert.SerializeObject(_children)}
                });
                
                importer.userData = JsonConvert.SerializeObject(new Dictionary<string, string>(){{"SOVariantData", data}});
                
                EditorUtility.SetDirty(scriptableObject);
            }
            
            AssetDatabase.SaveAssets();
            
        }

        private static Tuple<string, ScriptableObject, List<string>, List<string>> ExtractData(string data)
        {
            string[] datas = data.Split('*');
            if (datas.Length != 3)
                return new Tuple<string, ScriptableObject, List<string>, List<string>>(null, null, null, null);

            byte[] parentDataStream = datas[0].Split(',').ToList().Select(source => byte.Parse(source)).ToArray();
            string parentGUID = SerializationUtility.DeserializeValue<string>(parentDataStream, DataFormat.Binary);

            var parent = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(parentGUID));

            byte[] overridesDataStream = datas[1].Split(',').ToList().Select(source => byte.Parse(source)).ToArray();
            var overridden = SerializationUtility.DeserializeValue<List<string>>(overridesDataStream, DataFormat.Binary);

            var children = DeserializeChildrenData(datas[2]);

            return new Tuple<string, ScriptableObject, List<string>, List<string>>(parentGUID, parent, overridden, children);
        }

        private static List<string> DeserializeChildrenData(string data)
        {
            byte[] childrenDataStream = data.Split(',').ToList().Select(source => byte.Parse(source)).ToArray();
            var children = SerializationUtility.DeserializeValue<List<string>>(childrenDataStream, DataFormat.Binary);
            return children;
        }
        
        
    }
}