# UnityRuntimeCompressedTexturePacker
VRAMとDrawCallを最適化するため、圧縮されたテクスチャからパックされたアトラスを動的に生成するUnityランタイム向けユーティリティです。

## サポートしているファイル
- astc(ASTC_4x4,ASTC_5x5,ASTC_6x6,ASTC_8x8,ASTC_10x10,ASTC_12x12)
- dds ( BC1,BC5,BC7)
- ktx (ETC2_RGB,ETC2_RGBA1,ETC_RGBA8,ASTC_4x4,ASTC_5x5,ASTC_6x6,ASTC_8x8,ASTC_10x10,ASTC_12x12)

## 圧縮テクスチャを作るためのコマンドの例
Unityで利用する場合、YFlipが必要になります。
### .astcテクスチャの生成 
- 対応フォーマット：ASTC形式のみ
- 利用ツール：https://github.com/ARM-software/astc-encoder

```
:: 4x4,5x5,6x6,8x8,10x10,12x12
astcenc -cs input.png output.astc 4x4 -exhaustive -yflip
```

### KTX version1 テクスチャファイル
- 対応フォーマット： ETC2形式、 ASTC形式
- 利用ツール：https://developer.imaginationtech.com/solutions/pvrtextool/

```
:: ETC2_RGB_A1,ETC2_RGB,ETC2_RGBA
PVRTexToolCLI -i test.png -o test.ktx -flip y -f ETC2_RGB_A1,UBN,sRGB -ics sRGB -q etcslow

:: ASTC_4x4,ASTC_5x5,ASTC_6x6,ASTC_8x8,ASTC_10x10,ASTC_12x12
PVRTexToolCLI -i test.png -o test.ktx -flip y -f ASTC_4x4,UBN,sRGB -ics sRGB -q astcexhaustive
```

### DDSファイル
- 対応フォーマット：BC1(DXT1),BC3(DXT5),BC7
- 利用ツール：https://github.com/microsoft/DirectXTex/releases

```
:: BC7_UNORM , BC3_UNORM , BC1_UNORM
Texcov -f BC7_UNORM test.png -nogpu -y -srgi -srgbo -vflip
```

## サンプルコード

### 単体のファイルロード
```
public Texture2D LoadAstcTexture()
{
    string path = System.IO.Path.Combine(Application.streamingAssetsPath, "test.astc");

    using( var fileBinary = UnsafeFileReadUtility.LoadFileSync(path, Unity.Collections.Allocator.Temp) ){
        if (fileBinary.IsCreated)
        {
            var textureFormatFile = TextureFileFormatUtility.GetTextureFileFormatObject(fileBinary);
            var texture = textureFormatFile.LoadTexture(fileBinary);
            return texture;
        }
    }
    return null;
}
```
### テクスチャファイルのロードとAtlasへのパッキングを行う

#### 同期読み込み

### asyncでの非同期読み込み

#### Coroutine での非同期読み込み
```
public void AsyncLoadStart()
{
    this.autoAtlasBuilder = new AutoAtlasBuilder(1024, 1024, TextureFormat.ASTC_4x4);
    string[] loadFiles = {
        System.IO.Path.Combine(Application.streamingAssetsPath, "test1.astc"),
        System.IO.Path.Combine(Application.streamingAssetsPath, "test2.astc"),
    };
    this.StartCoroutine(autoAtlasBuilder.LoadAndPackAsyncCoroutine(loadFiles, this.OnCompleteLoadAndPack, OnFailedLoadFile));
}

// callback when the loading and packing process is completed
private void OnCompleteLoadAndPack(IEnumerable<Sprite> sprites)
{
    var texture = autoAtlasBuilder.texture;
    foreach (var sprite in sprites)
    {
       // something to do for generated sprite
    }
    // if you don't need to append textures. reelase buffers.
    this.autoAtlasBuilder.ReleaseBuffers();
}

// callback when the load failed or packing failed
// if loading file is failed the width and height will be negative value.
private void OnFailedLoadFile(string file, int width, int height)
{
    Debug.LogError("Failed LoadFile " + file + "::" + width + "x" + height);
}
```

### 大量のアイコンをスクロールビューに表示するとき等、沢山のSpriteを入れ替えながら同じAtlasを利用するサンプル




## パッケージ同梱サンプル
本パッケージには実行できるサンプルが複数同梱されています。<br />
[詳細はコチラ](Samples.ja.md)
