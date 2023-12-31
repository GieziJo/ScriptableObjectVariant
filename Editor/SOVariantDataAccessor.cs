using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;

namespace Giezi.Tools
{
    public static class SOVariantDataAccessor
    {
        private const string _SOVariantDataPath = "Assets/Editor/SOVariant/Editor/";
        private const string _SOVariantDataAssetName = "SOVariantData.asset";
        
        private static SOVariantData _SOVariantData = null;
        public static SOVariantData SOVariantData
        {
            get
            {
                if (_SOVariantData != null)
                    return _SOVariantData;
                
                // _SOVariantData = AssetDatabase.LoadAssetAtPath<SOVariantData>(AssetDatabase.GUIDToAssetPath("5a5ebd48054ef4cdb87262173c062dc4"));
                _SOVariantData = AssetDatabase.LoadAssetAtPath<SOVariantData>(_SOVariantDataPath+_SOVariantDataAssetName);
                if (_SOVariantData != null)
                    return _SOVariantData;
                
                return CreateSOVariantDataFile();
            }
        }
        
        public static SOVariantData CreateSOVariantDataFile()
        {

            // string metdata =
            //     "fileFormatVersion: 2\nguid: 5a5ebd48054ef4cdb87262173c062dc4\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n";
            SOVariantData soData = ScriptableWizard.CreateInstance<SOVariantData>();
            Directory.CreateDirectory(_SOVariantDataPath);
            AssetDatabase.CreateAsset(soData, _SOVariantDataPath+_SOVariantDataAssetName);
            // File.WriteAllText(_SOVariantDataPath+_SOVariantDataAssetName + ".meta", metdata);
            EditorUtility.SetDirty(soData);
            return soData;
        }
    }
}