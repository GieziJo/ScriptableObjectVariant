using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Giezi.Tools
{
    public class SOData : ScriptableObject
    {
        [SerializeField] private SOVariantCreationData _soVariantCreationData = null;

        public SOVariantCreationData SOVariantCreationData
        {
            get => _soVariantCreationData;
            set => _soVariantCreationData = value;
        }

        [Button]
        public void CreateFile()
        {
            var data = SOVariantDataAccessor.SOVariantData;
        }
    }
}