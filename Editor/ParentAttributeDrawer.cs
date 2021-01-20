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

public class ParentAttributeInjector : OdinPropertyProcessor<TestScriptable>
{
    private TestScriptable parent;
    private TestScriptable target;
    private AssetImporter _import;
    private List<string> overridden;
    private List<CheckBoxAttribute> _checkBoxAttributes;

    void ParentSetter(TestScriptable parent)
    {
        this.parent = parent;
        _checkBoxAttributes = new List<CheckBoxAttribute>();
        SaveData();
        Property.RefreshSetup();
    }

    public override void ProcessMemberProperties(List<InspectorPropertyInfo> propertyInfos)
    {
        overridden = null;

        Selection.selectionChanged += OnSelectionChanged;
        _checkBoxAttributes = new List<CheckBoxAttribute>();

        LoadData();

        BoxGroupAttribute bxa = new BoxGroupAttribute("Data", true, false, 2);

        if (parent != null)
        {
            foreach (InspectorPropertyInfo propertyInfo in new List<InspectorPropertyInfo>(propertyInfos))
            {
                CheckBoxAttribute checkBoxAttribute =
                    new CheckBoxAttribute(propertyInfo.GetMemberInfo().Name,
                        overridden.Contains(propertyInfo.GetMemberInfo().Name), target, parent);
                _checkBoxAttributes.Add(checkBoxAttribute);
                propertyInfo.GetEditableAttributesList().Add(new HideLabelAttribute());
                propertyInfo.GetEditableAttributesList().Add(checkBoxAttribute);
                propertyInfo.GetEditableAttributesList().Add(bxa);
                // propertyInfo.GetEditableAttributesList().Add(new DisableIfAttribute( "@true" ));
            }
        }
        
        propertyInfos.AddValue<TestScriptable>("Parent", () => parent, ParentSetter);

        InspectorPropertyInfo parentPropertyInfo = propertyInfos.Last();
        
        propertyInfos.Insert(0, parentPropertyInfo);
        propertyInfos.RemoveAt(propertyInfos.Count - 1);
        
        parentPropertyInfo.GetEditableAttributesList().Add(new PropertyOrderAttribute(-1));
        parentPropertyInfo.GetEditableAttributesList().Add(new PropertySpaceAttribute(0, 10));
    }

    private void LoadData()
    {
        Object targetObject = Property.Tree.UnitySerializedObject.targetObject;
        target = (TestScriptable) targetObject;

        string path = AssetDatabase.GetAssetPath(targetObject);
        _import = AssetImporter.GetAtPath(path);

        string data = _import.userData;

        try
        {
            string[] datas = data.Split('*');

            byte[] parentDataStream = datas[0].Split(',').ToList().Select(source => byte.Parse(source)).ToArray();
            string parentPath = SerializationUtility.DeserializeValue<string>(parentDataStream, DataFormat.Binary);
            parent = AssetDatabase.LoadAssetAtPath<TestScriptable>(parentPath);

            byte[] overridesDataStream = datas[1].Split(',').ToList().Select(source => byte.Parse(source)).ToArray();
            overridden = SerializationUtility.DeserializeValue<List<string>>(overridesDataStream, DataFormat.Binary);
            // overridden.ForEach(pair => Debug.Log(pair));
        }
        catch (Exception e)
        {
            Debug.Log(e);
            parent = null;
            overridden = new List<string>();
        }
    }

    private void SaveData()
    {
        List<string> overriddenMembers = new List<string>();
        if (_checkBoxAttributes.Count > 0)
            overriddenMembers = _checkBoxAttributes.Where(attribute => attribute.IsOverriden)
                .Select(attribute => attribute.Name).ToList();

        string overridesData = string.Join(",",
            SerializationUtility.SerializeValue<List<string>>(overriddenMembers, DataFormat.Binary));

        string parentData = string.Join(",",
            SerializationUtility.SerializeValue<string>(AssetDatabase.GetAssetPath(parent), DataFormat.Binary));

        string data = parentData + "*" + overridesData;

        _import.userData = data;
        
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
    }


    private void OnSelectionChanged()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        SaveData();
    }
}

public class CheckBoxAttribute : Attribute
{
    public bool IsOverriden;
    public string Name;
    public TestScriptable Parent;
    public TestScriptable TargetObject;

    public CheckBoxAttribute(string name, bool isOverriden, TestScriptable targetObject, TestScriptable parent)
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
        // base.DrawPropertyLayout(label);
        GUILayout.BeginHorizontal();
        
        Rect rect = EditorGUILayout.GetControlRect();
        this.Attribute.IsOverriden = EditorGUI.Toggle(rect.Split(0,2), Attribute.Name, this.Attribute.IsOverriden);
        if (!this.Attribute.IsOverriden)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                                     | BindingFlags.Static;
            Attribute.TargetObject.GetType().GetField(Attribute.Name, bindFlags).SetValue(Attribute.TargetObject,
                Attribute.Parent.GetType().GetField(Attribute.Name, bindFlags).GetValue(Attribute.Parent));
        }
        
        this.CallNextDrawer(label);

        if (!this.Attribute.IsOverriden)
        {
            rect.x += rect.width;
            EditorGUI.DrawRect(rect, new Color(.2f,.2f,.2f,.6f));
        }

        GUILayout.EndHorizontal();
    }
}