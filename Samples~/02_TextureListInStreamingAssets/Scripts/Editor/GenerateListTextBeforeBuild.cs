using System.IO;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

namespace UTJ.Sample
{
    /// <summary>
    /// ビルド時にStreamingAsset以下にあるファイルのリストテキストを作成
    /// </summary>
    public class GenerateListTextBeforeBuild : BuildPlayerProcessor
    {
        /// <summary>
        /// ビルド前の処理
        /// </summary>
        /// <param name="buildPlayerContext"></param>
        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            string streamingPath = Application.streamingAssetsPath;
            // Androidのみ
            if (buildPlayerContext.BuildPlayerOptions.target == UnityEditor.BuildTarget.Android && Directory.Exists(streamingPath) )
            {
                var files = Directory.GetFiles(streamingPath, "*", SearchOption.AllDirectories);
                List<string> outputFiles = new List<string>();
                foreach (var file in files)
                {
                    if (file.EndsWith(".meta"))
                    {
                        continue;
                    }
                    outputFiles.Add(file.Substring(streamingPath.Length + 1).Replace('\\', '/'));
                }
                System.IO.File.WriteAllLines(Path.Combine(streamingPath, "list.txt"), outputFiles);
            }
        }

    }
}