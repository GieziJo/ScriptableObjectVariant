using System;
using UnityEngine;

namespace Giezi.Tools
{
    [Serializable]
    public class SOVariantCreationData
    {
        public SOVariantCreationData(ScriptableObject parent, ScriptableObject child)
        {
            _parent = parent;
            _child = child;
            _createSOVariant = true;
        }
        
        [SerializeField] private ScriptableObject _child;

        public ScriptableObject Child
        {
            get => _child;
            set => _child = value;
        }

        [SerializeField] private ScriptableObject _parent;

        public ScriptableObject Parent
        {
            get => _parent;
            set => _parent = value;
        }

        [SerializeField] private bool _createSOVariant;

        public bool CreateSoVariant
        {
            get => _createSOVariant;
            set => _createSOVariant = value;
        }
    }
}