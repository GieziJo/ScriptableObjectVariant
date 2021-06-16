// ===============================
// AUTHOR          : J. Giezendanner
// CREATE DATE     : 20.01.2020
// MODIFIED DATE   : 
// PURPOSE         : Scriptable object variant class
// SPECIAL NOTES   : 
// ===============================
// Change History:
//==================================


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.Serialization;
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

        public SOVariant(T targetObject)
        {
            LoadData(targetObject);
        }

        public bool SetParent(T parent)
        {
            if(_target is null)
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
            
            if (parent)
            {
                AddToChildrenData(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(parent)), targetGUID);

                InitialiseNewParent();
            }

            _overridden = new List<string>();
            SaveData(new List<string>());
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
                if(!_overridden.Contains(name))
                    _overridden.Add(name);
                
                var targetFieldInfo = FieldInfoHelper.GetFieldRecursively(_target.GetType(), name);
                var parentFieldInfo = FieldInfoHelper.GetFieldRecursively(_parent.GetType(), name);
                // handle copy of list/arrays
                if (typeof(IEnumerable).IsAssignableFrom(targetFieldInfo.FieldType) && targetFieldInfo.GetValue(_target) == parentFieldInfo.GetValue(_parent))
                {
                    object parentObject = parentFieldInfo.GetValue(_parent);
                    byte[] parentAsData = SerializationUtility.SerializeValueWeak(parentObject, DataFormat.Binary);
                    object parentObjectCopy = SerializationUtility.DeserializeValueWeak(parentAsData, DataFormat.Binary);
                    targetFieldInfo.SetValue(_target, parentObjectCopy);
                }
            }
            else if (!isOverriden)
            {
                if(_overridden.Contains(name))
                    _overridden.Remove(name);
                
                var parentValue = FieldInfoHelper.GetFieldRecursively(_parent.GetType(), name).GetValue(_parent);
                var targetFieldInfo = FieldInfoHelper.GetFieldRecursively(_target.GetType(), name);
                if(targetFieldInfo.GetValue(_target) != parentValue)
                    targetFieldInfo.SetValue(_target, parentValue);
            }
            
            SaveData(_overridden);
        }

        public void ChangeValue(string name, object value)
        {
            if(_parent && !_overridden.Contains(name))
                Debug.Log("<color>Field is not overridden</color>");
            
            var targetFieldInfo = FieldInfoHelper.GetFieldRecursively(_target.GetType(), name);
            if (targetFieldInfo.GetValue(_target) != value)
            {
                targetFieldInfo.SetValue(_target, value);
                SaveData(_overridden, new List<string>(){name});
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
            
            if(_target is null)
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
                _parent = extractedData.Item2;
                _overridden = extractedData.Item3 ?? new List<string>();
                _children = extractedData.Item4 ?? new List<string>();
            }
            catch
            {
                _parent = null;
                _overridden = new List<string>();
                _children = new List<string>();
            }
        }

        private Tuple<string, T, List<string>, List<string>> ExtractData(string data)
        {
            string[] datas = data.Split('*');
            if (datas.Length != 3)
                return new Tuple<string, T, List<string>, List<string>>(null, null, null, null);

            byte[] parentDataStream = datas[0].Split(',').ToList().Select(source => byte.Parse(source)).ToArray();
            string parentGUID = SerializationUtility.DeserializeValue<string>(parentDataStream, DataFormat.Binary);

            var parent = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(parentGUID));

            byte[] overridesDataStream = datas[1].Split(',').ToList().Select(source => byte.Parse(source)).ToArray();
            var overridden = SerializationUtility.DeserializeValue<List<string>>(overridesDataStream, DataFormat.Binary);

            var children = DeserializeChildrenData(datas[2]);

            return new Tuple<string, T, List<string>, List<string>>(parentGUID, parent, overridden, children);
        }

        private List<string> DeserializeChildrenData(string data)
        {
            byte[] childrenDataStream = data.Split(',').ToList().Select(source => byte.Parse(source)).ToArray();
            var children = SerializationUtility.DeserializeValue<List<string>>(childrenDataStream, DataFormat.Binary);
            return children;
        }

        private void InitialiseNewParent()
        {
            foreach (FieldInfo fieldInfo in FieldInfoHelper.GetAllFields(_parent.GetType()))
            {
                object value = FieldInfoHelper.GetFieldRecursively(_parent.GetType(), fieldInfo.Name).GetValue(_parent);
                FieldInfoHelper.GetFieldRecursively(_target.GetType(), fieldInfo.Name).SetValue(_target, value);
            }
        }

        public void SaveData(List<string> overriddenMembers, List<string> changedValues = null)
        {
            if(_import is null)
                return;
        
            string overridesData = SerializeOverrideData(overriddenMembers);

            string parentData = SerializeParentData(_parent);
            
            PropagateValuesToChildren(_target, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_target)), ref _children, _import, changedValues);

            string childrenData = SerializeChildrenData(_children);

            string data = parentData + "*" + overridesData + "*" + childrenData;

            _import.userData = data;
        
            EditorUtility.SetDirty(_target);
            AssetDatabase.SaveAssets();
        }

        private string SerializeChildrenData(List<string> children)
        {
            return string.Join(",",
                SerializationUtility.SerializeValue<List<string>>(children, DataFormat.Binary));
        }

        private string SerializeParentData(T parent)
        {
            return string.Join(",",
                SerializationUtility.SerializeValue<string>(AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(parent)).ToString(), DataFormat.Binary));
        }

        private string SerializeOverrideData(List<string> overrides)
        {
            if (_otherSerializationBackend != null && _otherSerializationBackend.Count > 0 && overrides.All(s => s != _otherSerializationBackend.First()))
                overrides.AddRange(_otherSerializationBackend);

            return string.Join(",",
                SerializationUtility.SerializeValue<List<string>>(overrides, DataFormat.Binary));
        }

        private void PropagateValuesToChildren(T target, string targetGUID, ref List<string> children, AssetImporter importer, List<string> changedValues)
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
                if(childImporter == null)
                {
                    children.Remove(child);
                    childrenUpdated = true;
                    continue;
                }
            
                var extractedData = ExtractData(childImporter.userData);
            
                if(extractedData.Item1 == null || extractedData.Item2 == null || extractedData.Item3 == null || extractedData.Item4 == null)
                {
                    children.Remove(child);
                    childrenUpdated = true;
                    continue;
                }
            
                string parentGUID = extractedData.Item1;

                if (parentGUID != targetGUID)
                {
                    children.Remove(child);
                    childrenUpdated = true;
                    continue;
                }
            
                List<string> overridden = extractedData.Item3;
                T childObject = AssetDatabase.LoadAssetAtPath<T>(childPath);
                bool childChanged = false;
                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    if(overridden.Contains(fieldInfo.Name))
                        continue;
                    object value = FieldInfoHelper.GetFieldRecursively(target.GetType(), fieldInfo.Name).GetValue(target);
                    var childFieldInfo = FieldInfoHelper.GetFieldRecursively(childObject.GetType(), fieldInfo.Name);
                    if (childFieldInfo.GetValue(childObject) != value)
                    {
                        childFieldInfo.SetValue(childObject, value);
                        childChanged = true;
                    }
                }
                if(childChanged)
                    EditorUtility.SetDirty(childObject);


                List<string> newChildrenList = extractedData.Item4;
                if(newChildrenList.Count > 0)
                    PropagateValuesToChildren(childObject, AssetDatabase.AssetPathToGUID(childPath), ref newChildrenList, importer, changedValues);
            }

            if (childrenUpdated)
            {
                WriteOnlyChildrenData(importer, children);
            }
        }

        private void AddToChildrenData(AssetImporter importer, string newChild)
        {
            string[] data = importer.userData.Split('*');
            if (data.Length != 3)
            {
                WriteOnlyChildrenData(importer, new List<string>(){newChild});
                return;
            }

            List<string> children = DeserializeChildrenData(data[2]);
            children.Add(newChild);
            WriteOnlyChildrenData(importer, children);
        }

        public void RemoveFromChildrenData(AssetImporter importer, string oldChild)
        {
            string[] data = importer.userData.Split('*');
            if(data.Length != 3)
                return;
            List<string> children = DeserializeChildrenData(data[2]);
            children.Remove(oldChild);
            WriteOnlyChildrenData(importer, children);
        }

        private void WriteOnlyChildrenData(AssetImporter importer, List<string> children)
        {
            string[] data = importer.userData.Split('*');
            if(data.Length == 3)
                importer.userData = data[0] + "*" + data[1] + "*" + SerializeChildrenData(children);
            else
                importer.userData = SerializeParentData(null) + "*" + SerializeOverrideData(new List<string>()) + "*" + SerializeChildrenData(children);
        }
    }
}