using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UTJ.RuntimeCompressedTexturePacker;
using UTJ.RuntimeCompressedTexturePacker.Format;

public class LoadAndDisplayComponent : MonoBehaviour
{

    /// <summary>
    /// 生成したSprite一覧を出すところ
    /// </summary>
    [SerializeField]
    private ScrollRect scrollRect;

    [SerializeField]
    private RawImage singleTexture;

    [SerializeField]
    private RawImage atlasTexture;


    /// <summary>
    /// デバッグ表示で利用するフォント
    /// </summary>
    [SerializeField]
    private Font debugTextFont;

    private float spritePositionY = 0;

    // Dropダウンのテクスチャタイプ
    private enum DropdownTextureType : int
    {
        ASTC = 0,
        KTX = 1,
        DDS = 2,
    }

    /// <summary>
    /// TextureTypeのドロップダウン
    /// </summary>
    [SerializeField]
    private Dropdown textureTypeDropdown;

    /// <summary>
    /// 生成したSpriteをInspector上で確認するためだけに用意
    /// </summary>
    [Header("For debug in Inspector")]
    [SerializeField]
    public List<Sprite> spriteListForDebug = new List<Sprite>();




    /// <summary>
    /// ロードするファイル(Androidでは、StreamingAssetsはDirectory.GetFilesで取得できないのもあって直書き
    /// </summary>
    private string[] loadFilesInStreamingAssets = {
        "encrypted/banner_beer",
        "encrypted/banner_fruits",
        "encrypted/banner_lunchBox",
    };

    // テクスチャファイルの読み込みと、Packingを自動で任せます
    private AutoAtlasBuilder autoAtlasBuilder;

    /// <summary>
    /// Dropdownを考慮して、実際に読み込むTextureファイル
    /// </summary>
    private string[] spritePackTargetFiles
    {
        get
        {
            if (textureTypeDropdown)
            {
                switch (textureTypeDropdown.value)
                {
                    case (int)DropdownTextureType.ASTC:
                        return GetLoadFileList(loadFilesInStreamingAssets, Application.streamingAssetsPath, "_4x4.astc.enc");
                    case (int)DropdownTextureType.KTX:
                        return GetLoadFileList(loadFilesInStreamingAssets, Application.streamingAssetsPath, "_ETC2_RGBA.ktx.enc");
                    case (int)DropdownTextureType.DDS:
                        return GetLoadFileList(loadFilesInStreamingAssets, Application.streamingAssetsPath, "_BC7_UNORM.dds.enc");
                }
            }
            return GetLoadFileList(loadFilesInStreamingAssets, Application.streamingAssetsPath, "_4x4.astc.enc");
        }
    }

    private string singleTextureFile
    {
        get
        {
            if (textureTypeDropdown)
            {
                switch (textureTypeDropdown.value)
                {
                    case (int)DropdownTextureType.ASTC:
                        return System.IO.Path.Combine(Application.streamingAssetsPath, loadFilesInStreamingAssets[0]) + "_4x4.astc.enc";
                    case (int)DropdownTextureType.KTX:
                        return System.IO.Path.Combine(Application.streamingAssetsPath, loadFilesInStreamingAssets[0]) + "_ETC2_RGBA.ktx.enc";
                    case (int)DropdownTextureType.DDS:
                        return System.IO.Path.Combine(Application.streamingAssetsPath, loadFilesInStreamingAssets[0]) + "_BC7_UNORM.dds.enc";
                }
            }
            return System.IO.Path.Combine(Application.streamingAssetsPath, loadFilesInStreamingAssets[0]) + "_4x4.astc.enc";
        }
    }

    /// 実際にロードするファイルを求めます
    private string[] GetLoadFileList(string[] files, string baseDir, string tail)
    {
        string[] output = new string[files.Length];
        for (int i = 0; i < files.Length; ++i)
        {
            output[i] = System.IO.Path.Combine(baseDir, files[i]) + tail;
        }
        return output;
    }

    /// <summary>
    /// 対象のTextureFormat
    /// </summary>
    private TextureFormat targetTextureFormat
    {
        get
        {
            if (textureTypeDropdown)
            {
                switch (textureTypeDropdown.value)
                {
                    case (int)DropdownTextureType.ASTC:
                        return TextureFormat.ASTC_4x4;
                    case (int)DropdownTextureType.KTX:
                        return TextureFormat.ETC2_RGBA8;
                    case (int)DropdownTextureType.DDS:
                        return TextureFormat.BC7;
                }
            }
            return TextureFormat.ASTC_4x4;
        }
    }

    // Awake時にTextureFormatサポート状況を見て Dropdownの値を決めます
    private void Awake()
    {
        if (textureTypeDropdown)
        {
            if (SystemInfo.SupportsTextureFormat(TextureFormat.ASTC_4x4))
            {
                textureTypeDropdown.value = 0;
            }
            else if (SystemInfo.SupportsTextureFormat(TextureFormat.ETC2_RGBA8))
            {
                textureTypeDropdown.value = 1;
            }
            else if (SystemInfo.SupportsTextureFormat(TextureFormat.BC7))
            {
                textureTypeDropdown.value = 2;
            }
        }
    }

    /// <summary>
    /// 破棄時の処理
    /// </summary>
    private void OnDestroy()
    {
        if (this.autoAtlasBuilder != null)
        {
            this.autoAtlasBuilder.Dispose();
        }
    }

    /// <summary>
    /// 単独のテクスチャロード
    /// </summary>
    public async void LoadSingleTexture()
    {
        using (var fileBinary = await UnsafeFileReadUtility.LoadFileAsync(singleTextureFile))
        {
            var textureFormtObject = TextureFileFormatUtility.GetTextureFileFormatObject(fileBinary);
            var loadedTexture = textureFormtObject.LoadTexture(fileBinary);
            singleTexture.texture = loadedTexture;
        }
    }

    /// <summary>
    /// 複数のファイルを読み込んでAtlasの作成、Spriteの作成
    /// </summary>
    public async void CreateSprites()
    {
        // this.spritePackTargetFiles;
        if(this.autoAtlasBuilder != null)
        {
            this.autoAtlasBuilder.Dispose();
        }
        this.autoAtlasBuilder = new AutoAtlasBuilder(1024,1024,this.targetTextureFormat);

        var sprites = await this.autoAtlasBuilder.LoadAndPackAsync(this.spritePackTargetFiles);
        foreach(var sprite in sprites)
        {
            this.AddSpriteToUI(sprite);
        }
        this.atlasTexture.texture = this.autoAtlasBuilder.texture;
    }


    #region SPRITE_INFO_UI_GENERATION
    /// <summary>
    /// デバッグ用UIにSpriteを追加
    /// </summary>
    /// <param name="sprite">追加されたスプライト</param>

    private void AddSpriteToUI(Sprite sprite)
    {
        var spriteGmo = new GameObject("spirte", typeof(RectTransform));
        var spriteRectTransform = spriteGmo.GetComponent<RectTransform>();
        spriteRectTransform.SetParent(scrollRect.content);


        float height = 0.0f;
        float width = 0.0f;
        if (sprite.rect.height > sprite.rect.width)
        {
            height = Mathf.Min(100.0f, sprite.rect.height);
            width = height * sprite.rect.width / sprite.rect.height;
        }
        else
        {
            width = Mathf.Min(100.0f, sprite.rect.width);
            height = width * sprite.rect.height / sprite.rect.width;
        }
        SetupRectTransformForDebugUI(spriteRectTransform, width, height, spritePositionY);
        var img = spriteGmo.AddComponent<Image>();
        img.sprite = sprite;

        // Add Text
        var textGmo = new GameObject("info", typeof(RectTransform));
        var textRectTransform = textGmo.GetComponent<RectTransform>();
        textRectTransform.SetParent(spriteRectTransform);
        SetupRectTransformForDebugUI(textRectTransform, 180.0f, 20.0f, -height - 5);
        var text = textGmo.AddComponent<Text>();
        text.text = sprite.name;
        text.font = debugTextFont;
        text.color = Color.black;
        // Add border line
        var lineGmo = new GameObject("line", typeof(RectTransform));
        var lineRectTransform = lineGmo.GetComponent<RectTransform>();
        lineRectTransform.SetParent(spriteRectTransform);
        SetupRectTransformForDebugUI(lineRectTransform, 180.0f, 2.0f, -height - 26);
        var lineImg = lineGmo.AddComponent<Image>();
        lineImg.color = Color.black;

        spritePositionY -= (height + 30);

    }
    // Debug用Sprite表示のRectTransformのセットアップ
    private void SetupRectTransformForDebugUI(RectTransform rectTransform, float width, float height, float positionY)
    {
        rectTransform.anchoredPosition = new Vector2(0, 0);
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.localPosition = new Vector2(5, positionY);
        rectTransform.sizeDelta = new Vector2(width, height);
        rectTransform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    #endregion SPRITE_INFO_UI_GENERATION
}
