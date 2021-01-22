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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SOVariantAttributeProcessor<T> : OdinPropertyProcessor<T> where T : ScriptableObject
{
    private T _parent;
    private T _target;
    private AssetImporter _import;
    private List<string> _overridden;
    private List<CheckBoxAttribute> _checkBoxAttributes;
    private bool _selectionChangedFlag = false;

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

        if (_overridden == null || _import == null)
        {
            _overridden = null;
            _checkBoxAttributes = new List<CheckBoxAttribute>();

            LoadData();

            BoxGroupAttribute bxa = new BoxGroupAttribute("Scriptable Object Variant", true, false, 2);

            if (_parent != null)
            {
                foreach (InspectorPropertyInfo propertyInfo in new List<InspectorPropertyInfo>(propertyInfos))
                {
                    CheckBoxAttribute checkBoxAttribute =
                        new CheckBoxAttribute(propertyInfo.GetMemberInfo().Name,
                            _overridden.Contains(propertyInfo.GetMemberInfo().Name), _target, _parent);
                    _checkBoxAttributes.Add(checkBoxAttribute);
                    propertyInfo.GetEditableAttributesList().Add(checkBoxAttribute);
                    propertyInfo.GetEditableAttributesList().Add(bxa);
                    // propertyInfo.GetEditableAttributesList().Add(new DisableIfAttribute( "@true" ));
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
            string[] datas = data.Split('*');

            byte[] parentDataStream = datas[0].Split(',').ToList().Select(source => byte.Parse(source)).ToArray();
            string parentPath = SerializationUtility.DeserializeValue<string>(parentDataStream, DataFormat.Binary);
            _parent = AssetDatabase.LoadAssetAtPath<T>(parentPath);

            byte[] overridesDataStream = datas[1].Split(',').ToList().Select(source => byte.Parse(source)).ToArray();
            _overridden = SerializationUtility.DeserializeValue<List<string>>(overridesDataStream, DataFormat.Binary);
        }
        catch (Exception e)
        {
            _parent = null;
            _overridden = new List<string>();
        }
    }

    private void SaveData()
    {
        if(_import is null)
            return;
        
        List<string> overriddenMembers = new List<string>();
        if (_checkBoxAttributes.Count > 0)
            overriddenMembers = _checkBoxAttributes.Where(attribute => attribute.IsOverriden)
                .Select(attribute => attribute.Name).ToList();

        string overridesData = string.Join(",",
            SerializationUtility.SerializeValue<List<string>>(overriddenMembers, DataFormat.Binary));

        string parentData = string.Join(",",
            SerializationUtility.SerializeValue<string>(AssetDatabase.GetAssetPath(_parent), DataFormat.Binary));

        string data = parentData + "*" + overridesData;

        _import.userData = data;
        
        EditorUtility.SetDirty(_target);
        AssetDatabase.SaveAssets();
    }


    private void OnSelectionChanged()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        _selectionChangedFlag = false;
        SaveData();
    }
}

public class CheckBoxAttribute : Attribute
{
    public bool IsOverriden;
    public string Name;
    public Object Parent;
    public Object TargetObject;

    public CheckBoxAttribute(string name, bool isOverriden, Object targetObject, Object parent)
    {
        this.IsOverriden = isOverriden;
        this.Name = name;
        this.TargetObject = targetObject;
        this.Parent = parent;
    }
}

public class CheckBoxDrawer : OdinAttributeDrawer<CheckBoxAttribute>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                                 | BindingFlags.Static;
        FieldInfo targetFieldInfo = GetFieldRecursively(Attribute.TargetObject.GetType(), Attribute.Name, bindFlags);
        FieldInfo parentFieldInfo = GetFieldRecursively(Attribute.Parent.GetType(), Attribute.Name, bindFlags);
        if (targetFieldInfo is null || parentFieldInfo is null)
        {
            this.CallNextDrawer(label);
            return;
        }
        
        GUILayout.BeginHorizontal();
        
        Rect rect = EditorGUILayout.GetControlRect();
        this.Attribute.IsOverriden = EditorGUI.Toggle(rect.Split(0,2), label.text, this.Attribute.IsOverriden);
        if (!this.Attribute.IsOverriden)
        {
            targetFieldInfo.SetValue(Attribute.TargetObject, parentFieldInfo.GetValue(Attribute.Parent));
        }

        GUIContent noLabel = new GUIContent(label);
        noLabel.text = "";
        
        this.CallNextDrawer(noLabel);

        if (!this.Attribute.IsOverriden)
        {
            rect.x += rect.width;
            EditorGUI.DrawRect(rect, new Color(.2f,.2f,.2f,.6f));
        }

        GUILayout.EndHorizontal();
    }

    FieldInfo GetFieldRecursively(Type type, string attributeName, BindingFlags bindFlags)
    {
        if (type == null)
            return null;
        FieldInfo fieldInfo = null;
        fieldInfo = type.GetField(attributeName, bindFlags);
        if (fieldInfo is null)
            return GetFieldRecursively(type.BaseType, attributeName, bindFlags);
        else
            return fieldInfo;
    }
}