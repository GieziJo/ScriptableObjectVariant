﻿// ===============================
// AUTHOR          : J. Giezendanner
// CREATE DATE     : 20.01.2020
// MODIFIED DATE   : 
// PURPOSE         : Scriptable object variant class
// SPECIAL NOTES   : 
// ===============================
// Change History:
//==================================

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
    public class SOVariant<T> where T : ScriptableObject
    {
        public T _parent = null;
        public T _target = null;
        public AssetImporter _import = null;
        public List<string> _overridden = null;
        public List<string> _otherSerializationBackend = null;
        public List<string> _children = null;
        
        private bool _SOVariantProperlyLoaded = true;

        public bool SoVariantProperlyLoaded => _SOVariantProperlyLoaded;

        public SOVariant(T targetObject)
        {
            LoadData(targetObject);
        }

        public bool SetParent(T parent, bool setToParentData = true)
        {
            if (_target == null)
                return false;

            if (parent)
            {
                if (parent.GetType() != _target.GetType())
                {
                    Debug.Log("Only equal types can be selected as parent");
                    return false;
                }

                if (AssetDatabase.GetAssetPath(parent) == AssetDatabase.GetAssetPath(_target))
                {
                    Debug.Log("You can't select the same object as parent");
                    return false;
                }

                if (AssetDatabase.GetAssetPath(parent) == AssetDatabase.GetAssetPath(_parent))
                {
                    Debug.Log("Selected object is already target's parent");
                    return false;
                }
            }

            string targetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_target));
            if (_parent)
                RemoveFromChildrenData(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(_parent)), targetGUID);

            this._parent = parent;
            _overridden = new List<string>();

            if (parent)
            {
                AddToChildrenData(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(parent)), targetGUID);

                if (setToParentData)
                    SetAllFieldsToParent();
                else
                    InitialiseNewParentOverrides();
            }

            SaveData(_overridden);
            return true;
        }

        public void NotifyOverrideChangeInState(string name, bool isOverriden)
        {
            if (!_parent)
            {
                Debug.Log("<color>Parent needs to be defined first</color>");
                return;
            }

            if (isOverriden)
            {
                if (!_overridden.Contains(name))
                    _overridden.Add(name);

                var targetFieldInfo = FieldInfoHelper.GetFieldRecursively(_target.GetType(), name);
                var parentFieldInfo = FieldInfoHelper.GetFieldRecursively(_parent.GetType(), name);
                // handle copy of list/arrays
                if (typeof(IEnumerable).IsAssignableFrom(targetFieldInfo.FieldType) &&
                    targetFieldInfo.GetValue(_target) == parentFieldInfo.GetValue(_parent))
                {
                    object parentObject = parentFieldInfo.GetValue(_parent);
                    
                    var jsonSettings = new Newtonsoft.Json.JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore, 
                        TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All
                    };
                    var serialized = JsonConvert.SerializeObject(parentObject, jsonSettings);
                    object parentObjectCopy = JsonConvert.DeserializeObject<object>(serialized, jsonSettings);
                    targetFieldInfo.SetValue(_target, parentObjectCopy);
                }
            }
            else if (!isOverriden)
            {
                if (_overridden.Contains(name))
                    _overridden.Remove(name);

                var parentValue = FieldInfoHelper.GetFieldRecursively(_parent.GetType(), name).GetValue(_parent);
                var targetFieldInfo = FieldInfoHelper.GetFieldRecursively(_target.GetType(), name);
                if (targetFieldInfo.GetValue(_target) != parentValue)
                    targetFieldInfo.SetValue(_target, parentValue);
            }

            SaveData(_overridden);
        }

        public void ChangeValue(string name, object value)
        {
            if (_parent && !_overridden.Contains(name))
                Debug.Log("<color>Field is not overridden</color>");

            var targetFieldInfo = FieldInfoHelper.GetFieldRecursively(_target.GetType(), name);
            if (targetFieldInfo.GetValue(_target) != value)
            {
                targetFieldInfo.SetValue(_target, value);
                SaveData(_overridden, new List<string>() {name});
            }
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
                Debug.Log("<color=red>SOVariant: Target is null</color>");
                return;
            }

            if (_import is null)
            {
                Debug.Log("<color=red>SOVariant: Import is null</color>");
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
                string dataJson = GetSOVariantData(data);
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
                    $"While trying to load the SOVariant data object \"{_target}\", a previous UserData entry " +
                    $"was found which can not be loaded into a Json file: \"{data}\", conflicting with " +
                    $"loading the data at hand.\nWould you like to override it? " +
                    $"(Aborting will prevent the modified SOVariant data to be selected)\n\n" +
                    $"Note: you can try to manually change the data at the following path: \"{AssetDatabase.GetAssetPath(_target)}.meta\", and try again.\n\n" +
                    $"It is also possible that you didn't update to the new version of the SOVariant package which handles the data as Json text instead of binary data " +
                    $"and additionaly checks for conflicts. You can upgrade your data by going to: \"Tools/GieziTools/SOVariant/Upgrade user data to new version\" (in this case select \"No\" and perform the upgrade).",
                    "Go ahead",
                    "No",
                    "Try again"))
                {
                    case (0):
                        return (null, null, null, null);
                    case (1):
                        Debug.Log($"UserData not overwritten.");
                        _SOVariantProperlyLoaded = false;
                        return (null, null, null, null);;
                    case (2):
                        return ExtractData(ReadUpdatedMetaFile(data, AssetDatabase.GetAssetPath(_target), _import));
                }
            }
            return (null, null, null, null);
        }

        private static string GetSOVariantData(string data)
        {
            string dataJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(data)["SOVariantData"];
            return dataJson;
        }

        private bool CheckForDataString(string data)
        {
            return !string.IsNullOrEmpty(data) ;
        }

        private void SetAllFieldsToParent()
        {
            foreach (FieldInfo fieldInfo in FieldInfoHelper.GetAllFields(_parent.GetType()))
            {
                object value = FieldInfoHelper.GetFieldRecursively(_parent.GetType(), fieldInfo.Name).GetValue(_parent);
                FieldInfoHelper.GetFieldRecursively(_target.GetType(), fieldInfo.Name).SetValue(_target, value);
            }
        }

        public void ResetAllFieldsToParentValue()
        {
            List<string> oldOverrides = new List<string>(_overridden);
            _overridden.Clear();
            
            SetAllFieldsToParent();
            
            SaveData(_overridden, oldOverrides);
        }

        private void InitialiseNewParentOverrides()
        {
            foreach (FieldInfo fieldInfo in FieldInfoHelper.GetAllFields(_parent.GetType()))
            {
                object parentValue = FieldInfoHelper.GetFieldRecursively(_parent.GetType(), fieldInfo.Name).GetValue(_parent);
                object targetValue = FieldInfoHelper.GetFieldRecursively(_target.GetType(), fieldInfo.Name).GetValue(_target);
                
                if(parentValue != targetValue)
                    _overridden.Add(fieldInfo.Name);
            }
        }

        private void PropagateValuesToChildren(T target, string targetGUID, ref List<string> children,
            AssetImporter importer, List<string> changedValues)
        {
            List<FieldInfo> fieldInfos = null;
            if (children.Count > 0)
            {
                if (changedValues == null)
                    fieldInfos = FieldInfoHelper.GetAllFields(_target.GetType()).ToList();
                else
                {
                    fieldInfos = new List<FieldInfo>();
                    foreach (string changedValue in changedValues)
                    {
                        FieldInfo info = FieldInfoHelper.GetFieldRecursively(_target.GetType(), changedValue);
                        fieldInfos.Add(info);
                    }
                }
            }

            bool childrenUpdated = false;

            foreach (string child in new List<string>(children))
            {
                string childPath = AssetDatabase.GUIDToAssetPath(child);
                AssetImporter childImporter = AssetImporter.GetAtPath(childPath);
                if (childImporter == null)
                {
                    children.Remove(child);
                    childrenUpdated = true;
                    continue;
                }

                var extractedData = ExtractData(childImporter.userData);

                if (extractedData.parentGUID == null || extractedData.parent == null || extractedData.overridden == null ||
                    extractedData.children == null)
                {
                    children.Remove(child);
                    childrenUpdated = true;
                    continue;
                }

                string parentGUID = extractedData.parentGUID;

                if (parentGUID != targetGUID)
                {
                    children.Remove(child);
                    childrenUpdated = true;
                    continue;
                }

                List<string> overridden = extractedData.overridden;
                T childObject = AssetDatabase.LoadAssetAtPath<T>(childPath);
                bool childChanged = false;
                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    if (overridden.Contains(fieldInfo.Name))
                        continue;
                    object value = FieldInfoHelper.GetFieldRecursively(target.GetType(), fieldInfo.Name)
                        .GetValue(target);
                    var childFieldInfo = FieldInfoHelper.GetFieldRecursively(childObject.GetType(), fieldInfo.Name);
                    if (childFieldInfo.GetValue(childObject) != value)
                    {
                        childFieldInfo.SetValue(childObject, value);
                        childChanged = true;
                    }
                }

                if (childChanged)
                    EditorUtility.SetDirty(childObject);


                List<string> newChildrenList = extractedData.children;
                if (newChildrenList.Count > 0)
                    PropagateValuesToChildren(childObject, AssetDatabase.AssetPathToGUID(childPath),
                        ref newChildrenList, importer, changedValues);
            }

            if (childrenUpdated)
            {
                WriteOnlyChildrenData(importer, children);
            }
        }

        private void AddToChildrenData(AssetImporter importer, string newChild)
        {
            var extractedData = ExtractData(importer.userData);

            List<string> children = extractedData.children ?? new List<string>();
            children.Add(newChild);
            WriteToImporter(importer, SerializeParentData(extractedData.parent), SerializeOverrideData(extractedData.overridden), SerializeChildrenData(children));
        }

        public void RemoveFromChildrenData(AssetImporter importer, string oldChild)
        {
            var extractedData = ExtractData(importer.userData);

            List<string> children = extractedData.children ?? new List<string>();
            
            children.Remove(oldChild);
            WriteToImporter(importer, SerializeParentData(extractedData.parent), SerializeOverrideData(extractedData.overridden), SerializeChildrenData(children));
        }

        private void WriteOnlyChildrenData(AssetImporter importer, List<string> children)
        {
            var extractedData = ExtractData(importer.userData);
            WriteToImporter(importer, SerializeParentData(extractedData.parent), SerializeOverrideData(extractedData.overridden), SerializeChildrenData(children));
        }

        public void SaveData(List<string> overriddenMembers, List<string> changedValues = null)
        {
            if (_import is null)
                return;

            string overridesData = SerializeOverrideData(overriddenMembers);

            string parentData = SerializeParentData(_parent);

            PropagateValuesToChildren(_target, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_target)),
                ref _children, _import, changedValues);

            string childrenData = SerializeChildrenData(_children);

            WriteToImporter(_import, parentData, overridesData, childrenData);

            EditorUtility.SetDirty(_target);
            AssetDatabase.SaveAssets();
        }

        private string SerializeChildrenData(List<string> children)
        {
            return JsonConvert.SerializeObject(children);
        }

        private string SerializeParentData(T parent)
        {
            return AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(parent)).ToString();
        }

        private string SerializeOverrideData(List<string> overrides)
        {
            if (_otherSerializationBackend != null && _otherSerializationBackend.Count > 0 &&
                overrides.All(s => s != _otherSerializationBackend.First()))
                overrides.AddRange(_otherSerializationBackend);

            return JsonConvert.SerializeObject(overrides);
        }

        private void WriteToImporter(AssetImporter importer, string parent, string overrideData, string children)
        {
            string data = JsonConvert.SerializeObject(new Dictionary<string, string>()
            {
                {"parentGUID", parent},
                {"overriddenFields", overrideData},
                {"childrenGUIDs", children}
            });

            if (CheckForUserDataAndOverride(importer, importer.userData, data))
                importer.userData = JsonConvert.SerializeObject(new Dictionary<string, string>()
                    {{"SOVariantData", data}});
        }

        private bool CheckForUserDataAndOverride(AssetImporter importer, string oldData, string newData)
        {
            if (!string.IsNullOrEmpty(oldData))
            {
                try
                {
                    JObject jObject = (JObject) JsonConvert.DeserializeObject(oldData);

                    if (jObject == null)
                        throw (null);

                    if (jObject.ContainsKey("SOVariantData"))
                        jObject["SOVariantData"] = newData;
                    else
                        jObject.Add("SOVariantData", newData);

                    importer.userData = JsonConvert.SerializeObject(jObject);
                    return false;
                }
                catch
                {
                    switch (EditorUtility.DisplayDialogComplex(
                        "Replace user data",
                        $"While trying to save the SOVariant object \"{importer.assetPath}\", a previous UserData entry " +
                        $"was found which can not be loaded into a Json file: \"{importer.userData}\", conflicting with " +
                        $"saving the data at hand.\nWould you like to override it? " +
                        $"(Aborting will prevent the modified SOVariant data to be saved)\n\n" +
                        $"Note: you can try to manually change the data at the following path: \"{importer.assetPath}.meta\", and try again.",
                        "Go ahead",
                        "No",
                        "Try again"))
                    {
                        case (0):
                            return true;
                        case (1):
                            Debug.Log($"UserData in File \"{importer.assetPath}.meta\" not overwritten.");
                            return false;
                        case (2):
                            string newOldData = ReadUpdatedMetaFile(oldData, importer.assetPath, importer);
                            return CheckForUserDataAndOverride(importer, newOldData, newData);
                    }
                }
            }

            return true;
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
}