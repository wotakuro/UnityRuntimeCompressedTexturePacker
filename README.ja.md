# UnityRuntimeCompressedTexturePacker
VRAMとDrawCallを最適化するため、圧縮されたテクスチャからパックされたアトラスを動的に生成するUnityランタイム向けユーティリティです。

## サポートしているファイル
- astc(ASTC_4x4,ASTC_5x5,ASTC_6x6,ASTC_8x8,ASTC_10x10,ASTC_12x12)
- dds ( BC1,BC3,BC7)
- ktx (ETC2_RGB,ETC2_RGBA1,ETC_RGBA8,ASTC_4x4,ASTC_5x5,ASTC_6x6,ASTC_8x8,ASTC_10x10,ASTC_12x12)

KTX2やBasis Univrsalを扱いたい場合は、KTX for Unityなどを検討してください。

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


<details><summary>## その他の情報</summary>

### AMD Compressonator
https://gpuopen.com/compressonator/

AMD Compressonatorは BC1,BC3,BC7に圧縮されたテクスチャをDDS及びKTXに書き出すことが可能です。<br />
しかしY反転が行えないため、Unityで正常に読み込めるテクスチャを1つのコマンドで書き出すことはできません。

DDSに変換する
```
:: BC1 / BC3 /BC7
compressonatorcli -fd BC7 -Quality 1.0 test.png test.dds
```

KTXに変換する
```
:: BC1 / BC3 /BC7
compressonatorcli -fd BC7 -Quality 1.0 test.png test.ktx
```

### ImageMagick
https://imagemagick.org/#gsc.tab=0

画像変換用のオープンソフトウェアとして有名なImageMagickですが、ゲーム用途の変換はあまりありません。<br />
BC1、BC3のddsへの変更が可能です

```
:: dxt1(BC1) , dxt5(BC3)
magick input.png -flip -define dds:compression=dxt5 output.dds
```

</details>

## サンプルコード

### 注意事項
実行しているランタイムプラットフォームによって、サポートしているTextureフォーマットは異なります。<br />
SystemInfo.SupportsTextureFormat を利用して、作成しようとしているTextureAtlasが実行環境で作成できるか確認する必要があります。<br />
※Editor実行時にはDecompressorがあるので、未対応のTextureも読み込むことが出来ますが、ビルドしたアプリでは出来ないケースがあります。<br />
<br />
[詳細はコチラ](https://docs.unity3d.com/ja/6000.0/Manual/texture-choose-format-by-platform.html)



### 単体のファイルロード
#### Webランタイム以外は同期読み込みが可能です
```
public Texture2D LoadRawTexture(){
    string path = System.IO.Path.Combine(Application.streamingAssetsPath, "test.astc");
    // Fileに読み込んだ内容をNativeArray<byte>に格納します
    using( var fileBinary = UnsafeFileReadUtility.LoadFileSync(path, Unity.Collections.Allocator.Temp) ){
        // ファイルのバイナリを見て、適切なFileフォーマットオブジェクトを作ってもらいます
        var textureFormatFile = TextureFileFormatUtility.GetTextureFileFormatObject(fileBinary);
        // 作成したオブジェクトからロードTextureします
        var texture = textureFormatFile.LoadTexture(fileBinary);
        return texture;
    }
    return null;
}
```
#### Webランタイムも含む場合(WebRuntimeでは非同期読み込みの必要があります)

```
public async Awaitable<Texture2D> LoadRawTexture(){
    string url = System.IO.Path.Combine(Application.streamingAssetsPath, "test.astc");

    using (var fileBinary = await UnsafeFileReadUtility.LoadAsync(url)){
        var textureFormatFile = TextureFileFormatUtility.GetTextureFileFormatObject(fileBinary);
        var texture = textureFormatFile.LoadTexture(fileBinary);
        return texture;
    }
}
```


### テクスチャファイルのロードとAtlasへのパッキングを行う


### asyncでの非同期読み込み

```
// オブジェクト
private AutoAtlasBuilder autoAtlasBuilder;

// 非同期ロードスタート
public async void AsyncLoadStart(){
    this.autoAtlasBuilder = new AutoAtlasBuilder(1024, 1024, targetTextureFormat);
    // 読み込みするファイル指定
    string[] loadFiles = {
        System.IO.Path.Combine(Application.streamingAssetsPath, "test1.astc"),
        System.IO.Path.Combine(Application.streamingAssetsPath, "test2.astc"),
    };
    // 戻り値に読み込んだスプライト一覧が入ります
    var sprites = await autoAtlasBuilder.LoadAndPackAsync(loadFiles);
    foreach (var sprite in sprites){
        // 読み込み失敗はNullが入ります
        if(sprite == null){
            continue;
        }
        // spriteに対して何かします
    }
    this.autoAtlasBuilder.ReleaseBuffers();
}
// 破棄された時にはDisposeしてください
void OnDestroy(){
    this.autoAtlasBuilder.Dispose();
}
```


#### 同期読み込み（Webはサポートもしません※まだWebは全て非対応)

```
// オブジェクト
private AutoAtlasBuilder autoAtlasBuilder;

// 処理する部分
public void LoadAndPack(){
    this.autoAtlasBuilder = new AutoAtlasBuilder(1024, 1024, targetTextureFormat);
    // 読み込みするファイル指定
    string[] loadFiles = {
        System.IO.Path.Combine(Application.streamingAssetsPath, "test1.astc"),
        System.IO.Path.Combine(Application.streamingAssetsPath, "test2.astc"),
    };
    // 戻り値に読み込んだスプライト一覧が入ります
    var sprites = autoAtlasBuilder.LoadAndPack(loadFiles);
    foreach (var sprite in sprites){
        // 読み込み失敗はNullが入ります
        if(sprite == null){
            continue;
        }
        // spriteに対して何かします
    }
    // 追加でTextureをパッキングする必要がないなら、ファイル読み込み用のバッファをクリアします。
    this.autoAtlasBuilder.ReleaseBuffers();
}
// 破棄された時にはDisposeしてください
void OnDestroy(){
    this.autoAtlasBuilder.Dispose();
}
```

#### Coroutine での非同期読み込み
```
// Atlas作成用のオブジェクト
private AutoAtlasBuilder autoAtlasBuilder;

// コルーチンでの非同期読み込みスタート
public void AsyncLoadStart(){
    // Atlasテクスチャのサイズとフォーマットを指定します。
    this.autoAtlasBuilder = new AutoAtlasBuilder(1024, 1024, TextureFormat.ASTC_4x4);
    // 読み込みするファイル指定
    string[] loadFiles = {
        System.IO.Path.Combine(Application.streamingAssetsPath, "test1.astc"),
        System.IO.Path.Combine(Application.streamingAssetsPath, "test2.astc"),
    };
    // LoadAndPackAsyncCoroutineの戻り値でコルーチン開始します
    this.StartCoroutine(autoAtlasBuilder.LoadAndPackAsyncCoroutine(loadFiles, this.OnCompleteLoadAndPack, OnFailedLoadFile));
}

// パッキング完了時のコールバック
private void OnCompleteLoadAndPack(IEnumerable<Sprite> sprites){
    var texture = autoAtlasBuilder.texture;
    foreach (var sprite in sprites){
        // 読み込み失敗はNullが入ります
        if(sprite == null){
            continue;
        }
       // spriteに何かします
    }
    // 追加でTextureをパッキングする必要がないなら、ファイル読み込み用のバッファをクリアします。
    this.autoAtlasBuilder.ReleaseBuffers();
}

// ファイル読み込み失敗時の処理
private void OnFailedLoadFile(string file, AtlasFailedReason reason, int width, int height){
    Debug.LogError("Failed LoadFile " + file + "::" + reason+ "::" + width + "x" + height);
}

// 破棄された時にはDisposeしてください
void OnDestroy(){
    this.autoAtlasBuilder.Dispose();
}
```

### 大量のアイコンをスクロールビューに表示するとき等、沢山のSpriteを入れ替えながら同じAtlasを利用するサンプル

```
// アトラスなどを管理するオブジェクト
private RecycleAtlasForFixedSizeImages recycleAtlasForFixed;

// 読み込み対象のアイコンファイル一覧
private List<string> iconFiles{
    get{
        var iconFiles = new List<string>();
        for(int i = 0; i < 50; ++i){
            iconFiles.Add( System.IO.Path.Combine(Application.streamingAssetsPath,string.Format("icon{0:000}.astc", i) ) );
        }
        return iconNames;
    }
}
// 読み込みが必要なアイコン
private int index = 0;

// 初期化時
void Awake(){
    // テクスチャーAtlasのサイズ、フォーマット、そして読み込むアイコンのサイズを指定してオブジェクトを作成します
    this.recycleAtlasForFixed = new RecycleAtlasForFixedSizeImages(1024, 1024, textureFormat, 256, 256);
}

// 更新処理
void Update(){
   var icons = iconFiles;
   for(int i = index ; i < index + 5 && icons.Count; ++ i ){
       // 毎フレーム、ファイルパスを渡してリクエストして下さい。
       // Atlasがいっぱいになった場合、リクエストが古いものから破棄していきます。
       // ファイルに対応したSpriteが作られていればファイルを作ります。nullが返されれば、ロード中のものです。
       var sprite = this.recycleAtlasForFixedSizeImages.Request( icons[i] );
   }
}

// 破棄された時にはDisposeしてください
void OnDestroy(){
  recycleAtlasForFixed.Dipose();
}
```

### 独自フォーマット(暗号化されたファイルなど)を対応する

独自のフォーマットに対応するには以下の二点を行う事で対応が出来ます。
 - 1.ITextureFileFormatを継承したstructを実装します
 - 2.TextureFileFormatUtility.SetAppendFormatDelageteで、ファイルフォーマット判定処理を追加します

```
/// 1.ITextureFileFormatを継承したstructを実装

/// <summary>
/// 暗号化ファイルテクスチャフォーマット（独自）
/// </summary>
public class EncryptedTextureFileFormat : ITextureFileFormat{
    // 先頭4ByteのSignature
    private const uint Signature = 0x5446594DU;
    // XORの暗号キー
    private const uint EncryptKey = 0x20534444U;

    // Textureの幅
    private uint textureWidth;
    // Textureの高さ
    private uint textureHeight;
    // Textureフォーマット
    private TextureFormat format;

    /// <summary>
    /// [interface 実装] Textureの幅
    /// </summary>
    public int width => (int)textureWidth;

    /// <summary>
    /// [interface 実装] Textureの高さ
    /// </summary>
    public int height => (int)textureHeight;

    /// <summary>
    /// [interface 実装] Textureのフォーマット
    /// </summary>
    public TextureFormat textureFormat => format;


    /// <summary>
    /// [interface 実装] Textureとして正しいか？
    /// </summary>
    public bool IsValid => (width > 0 && height >0);

    /// <summary>
    /// [interface 実装]ファイルの中身からMipmapではない形でのテクスチャの実態を返します
    /// </summary>
    /// <param name="fileBinary">ファイルの中身</param>
    /// <returns>作成したテクスチャを返します</returns>
    public NativeArray<byte> GeImageDataWithoutMipmap(NativeArray<byte> fileBinary)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(fileBinary.Length - 16, Allocator.Temp);

        unsafe{
            uint* src = (uint*)(fileBinary.GetUnsafePtr()) + 4;
            uint*dst = (uint*)(bytes.GetUnsafePtr());
            int size = (fileBinary.Length-16 )/ 4;
            for (int i = 0; i < size; i++){
                *dst = *src ^ EncryptKey;
                ++dst;++src;
            }
        }
        return bytes;
    }

    /// <summary>
    /// [interface 実装]ヘッダーのロード
    /// </summary>
    /// <param name="fileBinary">ファイルの中身</param>
    /// <returns>ロードに失敗ならFalseを返します</returns>
    public bool LoadHeader(NativeArray<byte> fileBinary){
        if(fileBinary.Length < 16) { 
            return false;
        }
        this.textureWidth = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 4);
        this.textureHeight = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 8);
        this.format = (TextureFormat)BytesToOtherTypesUtility.ReadUintFast(fileBinary, 12);
        return true;
    }

    /// <summary>
    /// [interface 実装]Textureのロード
    /// </summary>
    /// <param name="fileBinary">ファイルの中身</param>
    /// <param name="isLinearColor">リニアカラーかどうか？</param>
    /// <param name="useMipmap">Mipmapも考慮するか？</param>
    /// <returns>作成されたTexture</returns>
    public Texture2D LoadTexture(NativeArray<byte> fileBinary, bool isLinearColor = false, bool useMipmap = false){
        // Mipmap考慮しないでテクスチャ作成します
        return TextureFileFormatUtility.CreateTextureWithoutMipmap(this,fileBinary,isLinearColor);
    }

    /// <summary>
    /// 先頭のバイトをみて、このフォーマットであるかを返します。
    /// </summary>
    /// <param name="fileBinary">ファイルデータのバイナリデータ</param>
    /// <returns>Signatureが一致しているならTrue</returns>
    public static bool SignatureValid(NativeArray<byte> fileBinary){
        var signature = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 0);
        return (signature == Signature);
    }
}

```

```
/// 2.TextureFileFormatUtility.appendFormatDetelctFuncction の設定

/// 初期化時に処理
[RuntimeInitializeOnLoadMethod]
static void RegisterFileFormats(){
    TextureFileFormatUtility.SetAppendFormatDelagete(GetAppTextureFileFormat);
}

/// ファイルの中身を見てTextuureのタイプを振り分けします
private static ITextureFileFormat GetAppTextureFileFormat(NativeArray<byte> fileBinary){
    if (EncryptedTextureFileFormat.SignatureValid(fileBinary)) {
        return new EncryptedTextureFileFormat();
    }
    // NullTextureFileを返すと、システムデフォルト判定を行います。
    return new NullTextureFile();
}
```


## パッケージ同梱サンプル
本パッケージには実行できるサンプルが複数同梱されています。<br />
[Webランタイムデモはコチラ](https://wotakuro.github.io/UnityRuntimeCompressedTexturePacker/)<br />
[詳細はコチラ](Documentation~/Samples.ja.md)
