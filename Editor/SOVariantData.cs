using System;
using System.Collections.Generic;
using UnityEngine;

namespace Giezi.Tools
{
    [Serializable]
    public class SOVariantData
    {
        [SerializeField] private ScriptableObject _parent = null;
        [SerializeField] private List<ScriptableObject> _children = new ();
        
        [SerializeField] private List<string> _overridden = new();
        [SerializeField] private List<string> _otherSerializationBackend = new ();

        public ScriptableObject Parent
        {
            get => _parent;
            set => _parent = value;
        }

        public List<ScriptableObject> Children
        {
            get => _children;
            set => _children = value;
        }

        public List<string> Overridden
        {
            get => _overridden;
            set => _overridden = value;
        }

        public List<string> OtherSerializationBackend
        {
            get => _otherSerializationBackend;
            set => _otherSerializationBackend = value;
        }
    }
}