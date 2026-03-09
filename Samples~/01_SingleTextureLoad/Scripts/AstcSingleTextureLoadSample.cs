using UnityEngine;
using UnityEngine.UI;
using UTJ.RuntimeCompressedTexturePacker;
using UTJ.RuntimeCompressedTexturePacker.Format;

namespace UTJ.Sample
{
    /// <summary>
    /// Astcファイルを直接読み込むサンプル。
    /// 
    /// ASTCの作成例
    /// https://github.com/ARM-software/astc-encoder
    /// astcenc -cs input.png output.astc 4x4 -exhaustive -yflip
    /// </summary>
    public class AstcSingleTextureLoadSample : MonoBehaviour
    {
        /// <summary>
        /// StreamingAssets以下のパスを指定
        /// </summary>
        [SerializeField]
        public string file;

        /// <summary>
        /// 対象のImage
        /// </summary>
        [SerializeField]
        private RawImage dstImage;

        /// <summary>
        /// Textureファイルをそのままロードします
        /// </summary>
        public void LoadAstcTexture()
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, file);

            var astcTexture = new AstcTextureFormat();
            using( var fileBinary = UnsafeFileReadUtility.LoadFileSync(path, Unity.Collections.Allocator.Temp) ){
                if (fileBinary.IsCreated)
                {
                    var texture = astcTexture.LoadTexture(fileBinary);

                    dstImage.texture = texture;
                    this.AdjustImageSize();
                }
            }
        }

        /// <summary>
        /// ImageオブジェクトをTextureサイズに合わせます
        /// </summary>
        private void AdjustImageSize()
        {
            if (dstImage != null && dstImage.texture)
            {
                float textureWidth = dstImage.texture.width;
                float textureHeight = dstImage.texture.height;

                var size = dstImage.rectTransform.sizeDelta;

                if (textureWidth > textureHeight)
                {
                    float width = Mathf.Min(size.x, textureWidth);
                    float height = width * textureHeight / textureWidth;
                    dstImage.rectTransform.sizeDelta = new Vector2(width, height);
                }
                else
                {

                    float height = Mathf.Min(size.y,textureHeight);
                    float width = height * textureWidth / textureHeight;
                    dstImage.rectTransform.sizeDelta = new Vector2(width, height);
                }
            }
        }
    }
}