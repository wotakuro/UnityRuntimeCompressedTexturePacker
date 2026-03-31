using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;
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
    public class SingleTextureLoadSample : MonoBehaviour
    {
        /// <summary>
        /// StreamingAssets以下のパスを指定
        /// </summary>
        [SerializeField]
        public InputField inputField;


        /// <summary>
        /// 対象のImage
        /// </summary>
        [SerializeField]
        private RawImage dstImage;

        // Start処理
        private void Start()
        {
            // サポートしているTextureFormatに応じてInputFieldの値を変更しておきます
            if (inputField)
            {
                if (SystemInfo.SupportsTextureFormat(TextureFormat.ASTC_4x4))
                {
                    inputField.text = "astc/Banner/banner_beer_4x4.astc";
                }else if (SystemInfo.SupportsTextureFormat(TextureFormat.ETC2_RGBA8))
                {
                    inputField.text = "ktxEtc2RGBA8/Banner/banner_beer_ETC2_RGBA.ktx";
                }
                else if (SystemInfo.SupportsTextureFormat(TextureFormat.BC7))
                {
                    inputField.text = "ddsBC7/Banner/banner_beer_BC7_UNORM.dds";
                }
            }
        }
#if UNITY_WEBGL
        /// <summary>
        /// Textureファイルをそのままロードします
        /// </summary>
        public async void LoadAstcTexture()
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, inputField.text);
 
            using (UnityWebRequest request = UnityWebRequest.Get(path))
            {
                // リクエストを送信
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }

                // エラーハンドリング
                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    dstImage.texture = null;
                    return;
                }
                using (var fileBinary = UnsafeFileReadUtility.GetDataFromWebRequest(request, Allocator.Temp))
                {
                    var textureFormatFile = TextureFileFormatUtility.GetTextureFileFormatObject(fileBinary);

                    var texture = textureFormatFile.LoadTexture(fileBinary);
                    dstImage.texture = texture;
                    if (texture)
                    {
                        this.AdjustImageSize();
                    }
                    else
                    {
                        Debug.LogError("Not support file " + inputField.text);
                    }
                }
            }
        }

#else
        /// <summary>
        /// Textureファイルをそのままロードします
        /// </summary>
        public void LoadAstcTexture()
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, inputField.text);

            using( var fileBinary = UnsafeFileReadUtility.LoadFileSync(path, Unity.Collections.Allocator.Temp) ){
                if (fileBinary.IsCreated)
                {
                    var textureFormatFile = TextureFileFormatUtility.GetTextureFileFormatObject(fileBinary); ;
                    
                    var texture = textureFormatFile.LoadTexture(fileBinary);
                    dstImage.texture = texture;
                    if (texture)
                    {
                        this.AdjustImageSize();
                    }
                    else
                    {
                        Debug.LogError("Not support file " + inputField.text);
                    }
                }
                else
                {
                    dstImage.texture = null;
                    Debug.LogError("Not Found FIle " + path);
                }
            }
        }
#endif

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