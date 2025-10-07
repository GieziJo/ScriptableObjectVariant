using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Giezi.Tools
{
    [CreateAssetMenu]
    public class SOVariantDataLibrary : SerializedScriptableObject
    {
        [SerializeField]
        private Dictionary<ScriptableObject, SOVariantData> _library = new();

        public SOVariantData GetSOVariantDataForTarget(ScriptableObject target)
        {
            if (_library.TryGetValue(target, out SOVariantData data))
                return data;
            data = new SOVariantData();
            _library.Add(target, data);
            EditorUtility.SetDirty(this);
            return data;
        }

        public void WriteToLibrary(ScriptableObject target, ScriptableObject parent, List<string> overridden, List<ScriptableObject> children)
        {
            SOVariantData soData = GetSOVariantDataForTarget(target);
            soData.Parent = parent;
            soData.Overridden = overridden;
            soData.Children = children;
            EditorUtility.SetDirty(this);
        }
    }
}