// ===============================
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
using UnityEditor;
using UnityEngine;

namespace Giezi.Tools
{
    public class SOVariant<T> where T : ScriptableObject
    {
        public T _target = null;
        public SOVariantData _SoVariantData = null;
        private SOVariantDataLibrary _library = SOVariantDataAccessor.SoVariantDataLibrary;
        
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
        
                if (parent == _target)
                {
                    Debug.Log("You can't select the same object as parent");
                    return false;
                }
        
                if (parent == _SoVariantData.Parent)
                {
                    Debug.Log("Selected object is already target's parent");
                    return false;
                }
            }

            if (_SoVariantData.Parent)
                RemoveFromChildrenData((T) _SoVariantData.Parent, _target);
        
            this._SoVariantData.Parent = parent;
            _SoVariantData.Overridden = new List<string>();
        
            if (parent)
            {
                AddToChildrenData(parent, _target);
        
                if (setToParentData)
                    SetAllFieldsToParent();
                else
                    InitialiseNewParentOverrides();
            }
        
            SaveData(_SoVariantData.Overridden);
            return true;
        }
        
        public void NotifyOverrideChangeInState(string name, bool isOverriden)
        {
            if (!_SoVariantData.Parent)
            {
                Debug.Log("<color>Parent needs to be defined first</color>");
                return;
            }
        
            if (isOverriden)
            {
                if (!_SoVariantData.Overridden.Contains(name))
                    _SoVariantData.Overridden.Add(name);
        
                var targetFieldInfo = FieldInfoHelper.GetFieldRecursively(_target.GetType(), name);
                var parentFieldInfo = FieldInfoHelper.GetFieldRecursively(_SoVariantData.Parent.GetType(), name);
                // handle copy of list/arrays
                if ((typeof(IEnumerable).IsAssignableFrom(targetFieldInfo.FieldType) &&
                     targetFieldInfo.GetValue(_target) == parentFieldInfo.GetValue(_SoVariantData.Parent))
                    || targetFieldInfo.FieldType.BaseType == typeof(System.Object)
                    )
                {
                    object parentObject = parentFieldInfo.GetValue(_SoVariantData.Parent);
                    
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
                if (_SoVariantData.Overridden.Contains(name))
                    _SoVariantData.Overridden.Remove(name);
        
                var parentValue = FieldInfoHelper.GetFieldRecursively(_SoVariantData.Parent.GetType(), name).GetValue(_SoVariantData.Parent);
                var targetFieldInfo = FieldInfoHelper.GetFieldRecursively(_target.GetType(), name);
                if (targetFieldInfo.GetValue(_target) != parentValue)
                    targetFieldInfo.SetValue(_target, parentValue);
            }
        
            SaveData(_SoVariantData.Overridden);
        }
        
        public void ChangeValue(string name, object value)
        {
            if (_SoVariantData.Parent && !_SoVariantData.Overridden.Contains(name))
                Debug.Log("<color>Field is not overridden</color>");
        
            var targetFieldInfo = FieldInfoHelper.GetFieldRecursively(_target.GetType(), name);
            if (targetFieldInfo.GetValue(_target) != value)
            {
                targetFieldInfo.SetValue(_target, value);
                SaveData(_SoVariantData.Overridden, new List<string>() {name});
            }
        }
        
        public void LoadData(T targetObject)
        {
            _target = targetObject;
        
            if (_target == null)
            {
                Debug.Log("<color=red>SOVariant: Target is null</color>");
                return;
            }
        
            _SoVariantData = ExtractData(_target);
        }
        
        
        private SOVariantData ExtractData(T target)
        {
            return _library.GetSOVariantDataForTarget(target);
        }
        
        public void ResetAllFieldsToParentValue()
        {
            List<string> oldOverrides = new List<string>(_SoVariantData.Overridden);
            _SoVariantData.Overridden.Clear();
            
            SetAllFieldsToParent();
            
            SaveData(_SoVariantData.Overridden, oldOverrides);
        }
        
        private void SetAllFieldsToParent()
        {
            foreach (FieldInfo fieldInfo in FieldInfoHelper.GetAllFields(_SoVariantData.Parent.GetType()))
            {
                object value = FieldInfoHelper.GetFieldRecursively(_SoVariantData.Parent.GetType(), fieldInfo.Name).GetValue(_SoVariantData.Parent);
                FieldInfoHelper.GetFieldRecursively(_target.GetType(), fieldInfo.Name).SetValue(_target, value);
            }
        }
        
        private void InitialiseNewParentOverrides()
        {
            foreach (FieldInfo fieldInfo in FieldInfoHelper.GetAllFields(_SoVariantData.Parent.GetType()))
            {
                object parentValue = FieldInfoHelper.GetFieldRecursively(_SoVariantData.Parent.GetType(), fieldInfo.Name).GetValue(_SoVariantData.Parent);
                object targetValue = FieldInfoHelper.GetFieldRecursively(_target.GetType(), fieldInfo.Name).GetValue(_target);
                
                if(parentValue != targetValue)
                    _SoVariantData.Overridden.Add(fieldInfo.Name);
            }
        }
        
        private void PropagateValuesToChildren(T target, ref List<ScriptableObject> children, List<string> changedValues)
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
        
            foreach (ScriptableObject child in new List<ScriptableObject>(children))
            {
                if (child == null)
                {
                    children.Remove(child);
                    childrenUpdated = true;
                    continue;
                }
        
                SOVariantData extractedData = ExtractData((T) child);
        
                if (extractedData.Parent == null || extractedData.Overridden == null ||
                    extractedData.Children == null)
                {
                    children.Remove(child);
                    childrenUpdated = true;
                    continue;
                }
        
                ScriptableObject parent = extractedData.Parent;
        
                if (parent != target)
                {
                    children.Remove(child);
                    childrenUpdated = true;
                    continue;
                }
        
                List<string> overridden = extractedData.Overridden;
                bool childChanged = false;
                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    if (overridden.Contains(fieldInfo.Name))
                        continue;
                    object value = FieldInfoHelper.GetFieldRecursively(target.GetType(), fieldInfo.Name)
                        .GetValue(target);
                    var childFieldInfo = FieldInfoHelper.GetFieldRecursively(child.GetType(), fieldInfo.Name);
                    if (childFieldInfo.GetValue(child) != value)
                    {
                        childFieldInfo.SetValue(child, value);
                        childChanged = true;
                    }
                }
        
                if (childChanged)
                    EditorUtility.SetDirty(child);
        
        
                List<ScriptableObject> newChildrenList = extractedData.Children;
                if (newChildrenList.Count > 0)
                    PropagateValuesToChildren((T) child, ref newChildrenList, changedValues);
            }
        
            if (childrenUpdated)
            {
                WriteOnlyChildrenData(target, children);
            }
        }
        
        private void AddToChildrenData(T parent, T newChild)
        {
            var parentData = _library.GetSOVariantDataForTarget(parent);
            
            parentData.Children.Add(newChild);
            WriteToDictionary(parent, (T) parentData.Parent, parentData.Overridden, parentData.Children);
        }
        
        public void RemoveFromChildrenData(T parent, T oldChild)
        {
            var parentData = _library.GetSOVariantDataForTarget(parent);
            
            parentData.Children.Remove(oldChild);
            WriteToDictionary(parent, (T) parentData.Parent, parentData.Overridden, parentData.Children);
        }
        
        private void WriteOnlyChildrenData(T target, List<ScriptableObject> children)
        {
            SOVariantData extractedData = _library.GetSOVariantDataForTarget(target);
            WriteToDictionary(target, (T) extractedData.Parent, extractedData.Overridden, children);
        }
        
        public void SaveData(List<string> overriddenMembers, List<string> changedValues = null)
        {
            var children = new List<ScriptableObject>(_SoVariantData.Children);
            PropagateValuesToChildren(_target, ref children, changedValues);
         
            WriteToDictionary(_target, (T) _SoVariantData.Parent, overriddenMembers, children);
            
            EditorUtility.SetDirty(_target);
        }
        
        private void WriteToDictionary(T target, T parent, List<string> overridden, List<ScriptableObject> children)
        {
            if (target == null)
                return;
            
            _library.WriteToLibrary(target, parent, overridden, children);
        }
    }
}