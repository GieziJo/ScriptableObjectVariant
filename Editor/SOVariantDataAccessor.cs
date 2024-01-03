using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;

namespace Giezi.Tools
{
    public static class SOVariantDataAccessor
    {
        private const string _SOVariantDataPath = "Assets/Editor/SOVariant/Editor/";
        private const string _SOVariantDataAssetName = "SOVariantData.asset";
        
        private static SOVariantDataLibrary _soVariantDataLibrary = null;
        
        public static SOVariantDataLibrary SoVariantDataLibrary
        {
            get
            {
                if (_soVariantDataLibrary != null)
                    return _soVariantDataLibrary;
                
                // _SOVariantData = AssetDatabase.LoadAssetAtPath<SOVariantData>(AssetDatabase.GUIDToAssetPath("5a5ebd48054ef4cdb87262173c062dc4"));
                _soVariantDataLibrary = AssetDatabase.LoadAssetAtPath<SOVariantDataLibrary>(_SOVariantDataPath+_SOVariantDataAssetName);
                if (_soVariantDataLibrary != null)
                    return _soVariantDataLibrary;
                
                return CreateSOVariantDataFile();
            }
        }
        
        public static SOVariantDataLibrary CreateSOVariantDataFile()
        {

            // string metdata =
            //     "fileFormatVersion: 2\nguid: 5a5ebd48054ef4cdb87262173c062dc4\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n";
            SOVariantDataLibrary soDataLibrary = ScriptableWizard.CreateInstance<SOVariantDataLibrary>();
            Directory.CreateDirectory(_SOVariantDataPath);
            AssetDatabase.CreateAsset(soDataLibrary, _SOVariantDataPath+_SOVariantDataAssetName);
            // File.WriteAllText(_SOVariantDataPath+_SOVariantDataAssetName + ".meta", metdata);
            EditorUtility.SetDirty(soDataLibrary);
            return soDataLibrary;
        }
    }
}