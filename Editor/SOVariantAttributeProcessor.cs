// ===============================
// AUTHOR          : J. Giezendanner
// CREATE DATE     : 20.01.2020
// MODIFIED DATE   : 
// PURPOSE         : Scriptable object variants inspector processor
// SPECIAL NOTES   : 
// ===============================
// Change History:
//==================================


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Giezi.Tools;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Giezi.Tools
{
    public class SOVariantAttributeProcessor<T> : OdinPropertyProcessor<T> where T : ScriptableObject
    {
        private bool _selectionChangedFlag = false;
        private SOVariant<T> _soVariant = null;


        void ParentSetter(T parent)
        {
            if(!_soVariant.SetParent(parent, false))
                return;
            
            _soVariant._overridden = null;
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

                AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReloads;
            }

            if (_soVariant == null || _soVariant._overridden == null || _soVariant._import == null || _soVariant._children == null){
                _soVariant = new SOVariant<T>((T)Property.Tree.UnitySerializedObject.targetObject);
                if (!_soVariant.SoVariantProperlyLoaded)
                {
                    Selection.selectionChanged -= OnSelectionChanged;
                    _selectionChangedFlag = false;
                    AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReloads;
                    
                    Selection.activeObject = null;
                    
                    return;
                }

                BoxGroupAttribute bxa = new BoxGroupAttribute("Scriptable Object Variant", true, false, 2);

                if (_soVariant._parent != null)
                {
                    _soVariant._otherSerializationBackend = new List<string>();
                    foreach (InspectorPropertyInfo propertyInfo in new List<InspectorPropertyInfo>(propertyInfos))
                    {
                        if (propertyInfo.SerializationBackend == SerializationBackend.None)
                        {
                            _soVariant._otherSerializationBackend.Add(propertyInfo.GetMemberInfo().Name);
                            continue;
                        }

                        CheckBoxAttribute checkBoxAttribute =
                            new CheckBoxAttribute(propertyInfo.GetMemberInfo().Name,
                                _soVariant._overridden.Contains(propertyInfo.GetMemberInfo().Name),
                                _soVariant._target, _soVariant._parent, _soVariant.NotifyOverrideChangeInState);
                        propertyInfo.GetEditableAttributesList().Add(checkBoxAttribute);
                        propertyInfo.GetEditableAttributesList().Add(bxa);
                        
                        // ! enable to debug
                        // propertyInfo.GetEditableAttributesList().Add(new ShowDrawerChainAttribute());
                    }
                }
                
                propertyInfos.AddValue("Original", () => _soVariant._parent, ParentSetter);

                InspectorPropertyInfo parentPropertyInfo = propertyInfos.Last();


                propertyInfos.Insert(0, parentPropertyInfo);
                propertyInfos.RemoveAt(propertyInfos.Count - 1);

                parentPropertyInfo.GetEditableAttributesList().Add(new PropertyOrderAttribute(-1));
                parentPropertyInfo.GetEditableAttributesList().Add(new PropertySpaceAttribute(0, 10));
            }
        }

        private void OnBeforeAssemblyReloads() => OnSelectionChanged();


        private void OnSelectionChanged()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReloads;
            Selection.selectionChanged -= OnSelectionChanged;
            _selectionChangedFlag = false;
            
            _soVariant.SaveData(_soVariant._overridden);
        }
    }
}

public class CheckBoxAttribute : Attribute
{
    public bool IsOverriden;
    public string Name;
    public Object Parent;
    public Object Target;
    public Action<string, bool> NotifyOverrideChange;

    public CheckBoxAttribute(string name, bool isOverriden, Object target, Object parent, Action<string, bool> notifyOverrideChange)
    {
        this.IsOverriden = isOverriden;
        this.Name = name;
        this.Target = target;
        this.Parent = parent;
        this.NotifyOverrideChange = notifyOverrideChange;
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
        bool newIsOverriden = EditorGUI.ToggleLeft(subRect, label.text, this.Attribute.IsOverriden);
        if (newIsOverriden != this.Attribute.IsOverriden)
        {
            this.Attribute.NotifyOverrideChange(this.Attribute.Name, newIsOverriden);
            this.Attribute.IsOverriden = newIsOverriden;
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