using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

            var _library = SOVariantDataAccessor.SoVariantDataLibrary;
            foreach (ScriptableObject scriptableObject in scriptableObjects)
            {
                var oldSOV = new SOVariantOld<ScriptableObject>(scriptableObject);
                List<ScriptableObject> children = new();
                foreach (string stringChild in oldSOV._children)
                {
                    ScriptableObject child = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(stringChild));
                    children.Add(child);
                }
                _library.WriteToLibrary(oldSOV._target, oldSOV._parent, oldSOV._overridden, children);
            }
            
            EditorUtility.SetDirty(_library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    #region Old SOVariant class only for upgrade
    public class SOVariantOld<T> where T : ScriptableObject
    {
        public T _parent = null;
        public T _target = null;
        public AssetImporter _import = null;
        public List<string> _overridden = null;
        public List<string> _otherSerializationBackend = null;
        public List<string> _children = null;
        
        private bool _SOVariantOldProperlyLoaded = true;

        public bool SOVariantOldProperlyLoaded => _SOVariantOldProperlyLoaded;

        public SOVariantOld(T targetObject)
        {
            LoadData(targetObject);
        }

        public void LoadData(T targetObject)
        {
            try
            {
                _target = targetObject;

                string path = AssetDatabase.GetAssetPath(targetObject);
                _import = AssetImporter.GetAtPath(path);
            }
            catch
            {
                return;
            }

            if (_target is null)
            {
                Debug.Log("<color=red>SOVariantOld: Target is null</color>");
                return;
            }

            if (_import is null)
            {
                Debug.Log("<color=red>SOVariantOld: Import is null</color>");
                return;
            }

            try
            {
                string data = _import.userData;
                var extractedData = ExtractData(data);
                _parent = extractedData.parent;
                _overridden = extractedData.overridden ?? new List<string>();
                _children = extractedData.children ?? new List<string>();
            }
            catch
            {
                _parent = null;
                _overridden = new List<string>();
                _children = new List<string>();
            }
        }

        
        private (string parentGUID, T parent, List<string> overridden, List<string>children) ExtractData(string data)
        {
            if(!CheckForDataString(data))
                return (null, null, null, null);
            try
            {
                string dataJson = GetSOVariantOldData(data);
                if(! CheckForDataString(dataJson))
                    return (null, null, null, null);
                Dictionary<string, string> containedData =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(dataJson);

                string parentGUID = containedData["parentGUID"];
                
                var parent = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(parentGUID));

                var overridden = JsonConvert.DeserializeObject<List<string>>(containedData["overriddenFields"]);

                var children = JsonConvert.DeserializeObject<List<string>>(containedData["childrenGUIDs"]);

                return (parentGUID, parent, overridden, children);
            }
            catch
            {
                switch (EditorUtility.DisplayDialogComplex(
                    "Import user data",
                    $"While trying to load the SOVariantOld data object \"{_target}\", a previous UserData entry " +
                    $"was found which can not be loaded into a Json file: \"{data}\", conflicting with " +
                    $"loading the data at hand.\nWould you like to override it? " +
                    $"(Aborting will prevent the modified SOVariantOld data to be selected)\n\n" +
                    $"Note: you can try to manually change the data at the following path: \"{AssetDatabase.GetAssetPath(_target)}.meta\", and try again.\n\n" +
                    $"It is also possible that you didn't update to the new version of the SOVariantOld package which handles the data as Json text instead of binary data " +
                    $"and additionaly checks for conflicts. You can upgrade your data by going to: \"Tools/GieziTools/SOVariantOld/Upgrade user data to new version\" (in this case select \"No\" and perform the upgrade).",
                    "Go ahead",
                    "No",
                    "Try again"))
                {
                    case (0):
                        return (null, null, null, null);
                    case (1):
                        Debug.Log($"UserData not overwritten.");
                        _SOVariantOldProperlyLoaded = false;
                        return (null, null, null, null);;
                    case (2):
                        return ExtractData(ReadUpdatedMetaFile(data, AssetDatabase.GetAssetPath(_target), _import));
                }
            }
            return (null, null, null, null);
        }

        private static string GetSOVariantOldData(string data)
        {
            string dataJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(data)["SOVariantData"];
            return dataJson;
        }

        private bool CheckForDataString(string data)
        {
            return !string.IsNullOrEmpty(data) ;
        }


        private string ReadUpdatedMetaFile(string oldData, string targetPath, AssetImporter importer)
        {
            string[] lines = System.IO.File.ReadAllLines(targetPath + ".meta");
            foreach (string line in lines)
            {
                if (line.StartsWith("  userData: "))
                {
                    var newLine = line.Substring(13);
                    newLine = newLine.Substring(0, newLine.Length - 1);
                    if (_import != null)
                        _import.userData = newLine;
                    return newLine;
                }
            }

            return oldData;
        }
    }
    
    #endregion 
}