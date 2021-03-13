using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Giezi.Tools
{
    public class SOVariant<T> where T : ScriptableObject
    {
        private T _parent;
        private T _target;
        private AssetImporter _import;
        private List<string> _overridden;
        private List<string> _otherSerializationBackend;
        private List<CheckBoxAttribute> _checkBoxAttributes;
        private List<string> _children;
    }
}