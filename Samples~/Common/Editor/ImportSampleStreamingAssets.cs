using UnityEngine;
using UnityEditor;

namespace UTJ.Sample
{
    [FilePath("UserSettings/RCTP_StreamingAssetsProcess.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ImportSampleStreamingAssets : ScriptableSingleton<ImportSampleStreamingAssets>
    {
        private const int currentVersion = 3;
        [SerializeField]
        public bool isImportPackage = false;

        [SerializeField]
        public int version = 0;


        [InitializeOnLoadMethod]
        public static void ImportStreamigAssetPackage()
        {
            if (!ImportSampleStreamingAssets.instance.isImportPackage ||
                ImportSampleStreamingAssets.instance.version < currentVersion )
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
                ImportSampleStreamingAssets.instance.version = currentVersion;
                ImportSampleStreamingAssets.instance.Save(true);
            }
        }
    }
}