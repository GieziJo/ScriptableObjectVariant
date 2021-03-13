// ===============================
// AUTHOR          : J. Giezendanner
// CREATE DATE     : 20.01.2020
// MODIFIED DATE   : 
// PURPOSE         : Allows to create ScriptableObject variants
// SPECIAL NOTES   : 
// ===============================
// Change History:
//==================================


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GieziTools
{
    public class SOVariantAttributeProcessor<T> : OdinPropertyProcessor<T> where T : ScriptableObject
    {
        private T _parent;
        private T _target;
        private AssetImporter _import;
        private List<string> _overridden;
        private List<string> _otherSerializationBackend;
        private List<CheckBoxAttribute> _checkBoxAttributes;
        private bool _selectionChangedFlag = false;
        private List<string> _children;

        private SOVariant<T> _soVariant = null;

        void ParentSetter(T parent)
        {
            if(_target is null)
                return;
        
            if (parent)
            {
                if (parent.GetType() != _target.GetType())
                {
                    Debug.Log("Only equal types can be selected as parent");
                    return;
                }

                if (AssetDatabase.GetAssetPath(parent) == AssetDatabase.GetAssetPath(_target))
                {
                    Debug.Log("You can't select the same object as parent");
                    return;
                }
            }

            string targetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_target));
            if (_parent)
                RemoveFromChildrenData(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(_parent)), targetGUID);
            AddToChildrenData(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(parent)), targetGUID);
        
            this._parent = parent;
            _checkBoxAttributes = new List<CheckBoxAttribute>();
            SaveData();
            _overridden = null;
            Property.RefreshSetup();
        }
    

        public override void ProcessMemberProperties(List<InspectorPropertyInfo> propertyInfos)
        {
            if(!Property.Attributes.Select(attribute => attribute.GetType()).Contains(typeof(SOVariantAttribute)))
                return;

            if (!_selectionChangedFlag)
            {
                Selection.selectionChanged += OnSelectionChanged;
                _selectionChangedFlag = true;
            }

            if (_overridden == null || _import == null || _children == null)
            {
                _overridden = null;
                _checkBoxAttributes = new List<CheckBoxAttribute>();

                LoadData();

                BoxGroupAttribute bxa = new BoxGroupAttribute("Scriptable Object Variant", true, false, 2);

                if (_parent != null)
                {
                    _otherSerializationBackend = new List<string>();
                    foreach (InspectorPropertyInfo propertyInfo in new List<InspectorPropertyInfo>(propertyInfos))
                    {   
                        if (propertyInfo.SerializationBackend == SerializationBackend.None)
                        {
                            _otherSerializationBackend.Add(propertyInfo.GetMemberInfo().Name);
                            continue;
                        }

                        CheckBoxAttribute checkBoxAttribute =
                            new CheckBoxAttribute(propertyInfo.GetMemberInfo().Name,
                                _overridden.Contains(propertyInfo.GetMemberInfo().Name), _target, _parent);
                        _checkBoxAttributes.Add(checkBoxAttribute);
                        propertyInfo.GetEditableAttributesList().Add(checkBoxAttribute);
                        propertyInfo.GetEditableAttributesList().Add(bxa);
                        // ? enable to debug
                        // propertyInfo.GetEditableAttributesList().Add(new ShowDrawerChainAttribute());
                    }
                }

                propertyInfos.AddValue<T>("Original", () => _parent, ParentSetter);

                InspectorPropertyInfo parentPropertyInfo = propertyInfos.Last();


                propertyInfos.Insert(0, parentPropertyInfo);
                propertyInfos.RemoveAt(propertyInfos.Count - 1);

                parentPropertyInfo.GetEditableAttributesList().Add(new PropertyOrderAttribute(-1));
                parentPropertyInfo.GetEditableAttributesList().Add(new PropertySpaceAttribute(0, 10));
            }
        }

        private void LoadData()
        {
            try
            {
                Object targetObject = Property.Tree.UnitySerializedObject.targetObject;
                _target = (T) targetObject;

                string path = AssetDatabase.GetAssetPath(targetObject);
                _import = AssetImporter.GetAtPath(path);
            }
            catch (Exception e)
            {
                return;
            }
        
            if(_target is null || _import is null)
                return;
        
            try
            {
                string data = _import.userData;
                var extractedData = ExtractData(data);
                _parent = extractedData.Item2;
                _overridden = extractedData.Item3 ?? new List<string>();
                _children = extractedData.Item4 ?? new List<string>();
            }
            catch (Exception e)
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

        private static List<string> DeserializeChildrenData(string data)
        {
            byte[] childrenDataStream = data.Split(',').ToList().Select(source => byte.Parse(source)).ToArray();
            var children = SerializationUtility.DeserializeValue<List<string>>(childrenDataStream, DataFormat.Binary);
            return children;
        }

        private void SaveData()
        {
            if(_import is null)
                return;
        
            List<string> overriddenMembers = new List<string>();
            if (_checkBoxAttributes.Count > 0)
                overriddenMembers = _checkBoxAttributes.Where(attribute => attribute.IsOverriden)
                    .Select(attribute => attribute.Name).ToList();

            string overridesData = SerializeOverrideData(overriddenMembers);

            string parentData = SerializeParentData(_parent);

            PropagateValuesToChildren(_target, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_target)), ref _children, _import);
        
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

        private void PropagateValuesToChildren(T target, string targetGUID, ref List<string> children, AssetImporter importer)
        {
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

                foreach (FieldInfo fieldInfo in FieldInfoHelper.GetAllFields(target.GetType()))
                {
                    if(overridden.Contains(fieldInfo.Name))
                        continue;
                    object value = FieldInfoHelper.GetFieldRecursively(target.GetType(), fieldInfo.Name).GetValue(target);
                    FieldInfoHelper.GetFieldRecursively(childObject.GetType(), fieldInfo.Name).SetValue(childObject, value);
                }
                EditorUtility.SetDirty(childObject);


                List<string> newChildrenList = extractedData.Item4;
                if(newChildrenList.Count > 0)
                    PropagateValuesToChildren(childObject, AssetDatabase.AssetPathToGUID(childPath), ref newChildrenList, importer);
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

        private void RemoveFromChildrenData(AssetImporter importer, string oldChild)
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


        private void OnSelectionChanged()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            _selectionChangedFlag = false;
            SaveData();
        }
    }
}

public class CheckBoxAttribute : Attribute
{
    public bool IsOverriden;
    public string Name;
    public Object Parent;
    public Object Target;

    public CheckBoxAttribute(string name, bool isOverriden, Object target, Object parent)
    {
        this.IsOverriden = isOverriden;
        this.Name = name;
        this.Target = target;
        this.Parent = parent;
    }
}

[DrawerPriority(0,0,3000)]
public class CheckBoxDrawer : OdinAttributeDrawer<CheckBoxAttribute>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        if (label is null)
        {
            GUI.enabled = Attribute.IsOverriden;
            this.CallNextDrawer(label);
            GUI.enabled = true;
            return;
        }
        
        FieldInfo targetFieldInfo = FieldInfoHelper.GetFieldRecursively(Attribute.Target.GetType(), Attribute.Name);
        FieldInfo parentFieldInfo = FieldInfoHelper.GetFieldRecursively(Attribute.Parent.GetType(), Attribute.Name);
        if (targetFieldInfo is null || parentFieldInfo is null)
        {
            this.CallNextDrawer(label);
            return;
        }
        
        GUILayout.BeginHorizontal();
        
        Rect rect = EditorGUILayout.GetControlRect();
        Rect subRect = new Rect(rect);
        if (Attribute.IsOverriden)
            subRect = subRect.Split(0, 2);
        this.Attribute.IsOverriden = EditorGUI.ToggleLeft(subRect, label.text, this.Attribute.IsOverriden);
        
        if (!this.Attribute.IsOverriden)
        {
            targetFieldInfo.SetValue(Attribute.Target, parentFieldInfo.GetValue(Attribute.Parent));
        }
        else
        {
            // handle copy of list/arrays
            if (typeof(IEnumerable).IsAssignableFrom(targetFieldInfo.FieldType) && targetFieldInfo.GetValue(Attribute.Target) == parentFieldInfo.GetValue(Attribute.Parent))
            {
                object parentObject = parentFieldInfo.GetValue(Attribute.Parent);
                byte[] parentAsData = SerializationUtility.SerializeValueWeak(parentObject, DataFormat.Binary);
                object parentObjectCopy = SerializationUtility.DeserializeValueWeak(parentAsData, DataFormat.Binary);
                targetFieldInfo.SetValue(Attribute.Target, parentObjectCopy);
            }
        }
        
        GUIContent noLabel = new GUIContent(label);
        noLabel.text = "";
        if (this.Attribute.IsOverriden)
        {
            object value = parentFieldInfo.GetValue(Attribute.Parent);
            Object unityObject = value as Object;
            string parentFieldName = (unityObject != null) ? unityObject.name : (value != null ? value.ToString() : "None"); 
            
        
            Rect labelRect = new Rect(rect.Split(1,2));
        
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = new Color(.5f,.5f,.5f);
            labelStyle.alignment = TextAnchor.MiddleRight;
        
            EditorGUI.LabelField(labelRect, parentFieldName, labelStyle);
        }
        
        GUI.enabled = Attribute.IsOverriden;
        this.CallNextDrawer(noLabel);
        GUI.enabled = true;
        
        GUILayout.EndHorizontal();
    }
}


internal static class FieldInfoHelper
{
    private static BindingFlags bindFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Static | BindingFlags.Instance;
    internal static FieldInfo GetFieldRecursively(Type type, string attributeName)
    {
        if (type == null)
            return null;
        FieldInfo fieldInfo = null;
        fieldInfo = type.GetField(attributeName, bindFlags);
        if (fieldInfo is null)
            return GetFieldRecursively(type.BaseType, attributeName);
        else
            return fieldInfo;
    }
    
    internal static IEnumerable<FieldInfo> GetAllFields(Type t)
    {
        if (t == null || t == typeof(ScriptableObject) || t == typeof(SerializedScriptableObject))
            return Enumerable.Empty<FieldInfo>();
        return t.GetFields(bindFlags).Concat(GetAllFields(t.BaseType));
    }
}