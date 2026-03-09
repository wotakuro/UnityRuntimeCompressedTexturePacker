using UnityEngine;
using UnityEditor;

namespace UTJ.Sample
{
    [FilePath("UserSettings/RCTP_StreamingAssetsProcess.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class ImportSampleStreamingAssets : ScriptableSingleton<ImportSampleStreamingAssets>
    {
        public bool isImportPackage = false;

        [InitializeOnLoadMethod]
        public static void ImportStreamigAssetPackage()
        {
            if (!ImportSampleStreamingAssets.instance.isImportPackage)
            {
                AssetDatabase.importPackageCompleted += OnCompletePackage;
                AssetDatabase.ImportPackage("Packages/com.utj.runtimecomptexturepacker/Samples~/RCTP_StreamingAssetsData.unitypackage", true);
            }
        }

        private static void OnCompletePackage(string package)
        {
            if(package == "RCTP_StreamingAssetsData")
            {
                ImportSampleStreamingAssets.instance.isImportPackage = true;
                ImportSampleStreamingAssets.instance.Save(true);
            }
        }
    }
}