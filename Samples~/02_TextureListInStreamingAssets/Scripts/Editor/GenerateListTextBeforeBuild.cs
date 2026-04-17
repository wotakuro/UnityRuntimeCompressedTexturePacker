using System.IO;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;
using System.Text;

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
            // Web / Androidのみ
            if ( (buildPlayerContext.BuildPlayerOptions.target == UnityEditor.BuildTarget.Android ||
                buildPlayerContext.BuildPlayerOptions.target == UnityEditor.BuildTarget.WebGL)
                && Directory.Exists(streamingPath) )
            {
                var files = Directory.GetFiles(streamingPath, "*", SearchOption.AllDirectories);
                var sb = new StringBuilder(1024);
                foreach (var file in files)
                {
                    if (file.EndsWith(".meta"))
                    {
                        continue;
                    }
                    var str = file.Substring(streamingPath.Length + 1).Replace('\\', '/');
                    if(string.IsNullOrEmpty(str) || str == "list.txt")
                    {
                        continue;
                    }
                    if(sb.Length > 0)
                    {
                        sb.Append("\n");
                    }
                    sb.Append(str);
                }
                System.IO.File.WriteAllText(Path.Combine(streamingPath, "list.txt"), sb.ToString() );
            }
        }

    }
}