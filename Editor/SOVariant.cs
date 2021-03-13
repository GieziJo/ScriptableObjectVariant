using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Giezi.Tools
{
    public class SOVariant<T> where T : ScriptableObject
    {
        public T _parent;
        public T _target;
        public AssetImporter _import;
        public List<string> _overridden;
        public List<string> _otherSerializationBackend;
        public List<CheckBoxAttribute> _checkBoxAttributes;
        public List<string> _children;
    }
}