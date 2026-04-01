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

### 注意事項
実行しているランタイムプラットフォームによって、サポートしているTextureフォーマットは異なります。<br />
SystemInfo.SupportsTextureFormat を利用して、作成しようとしているTextureAtlasが実行環境で作成できるか確認する必要があります。<br />
※Editor実行時にはDecompressorがあるので、未対応のTextureも読み込むことが出来ますが、ビルドしたアプリでは出来ないケースがあります。<br />
<br />
[詳細はコチラ](https://docs.unity3d.com/ja/6000.0/Manual/texture-choose-format-by-platform.html)



### 単体のファイルロード
#### Webランタイム以外
```
public Texture2D LoadRawTexture(){
    string path = System.IO.Path.Combine(Application.streamingAssetsPath, "test.astc");
    // Fileに読み込んだ内容をNativeArray<byte>に格納します
    using( var fileBinary = UnsafeFileReadUtility.LoadFileSync(path, Unity.Collections.Allocator.Temp) ){
        // ファイル作成時
        if (fileBinary.IsCreated){
            // ファイルのバイナリを見て、適切なFileフォーマットオブジェクトを作ってもらいます
            var textureFormatFile = TextureFileFormatUtility.GetTextureFileFormatObject(fileBinary);
            // 作成したオブジェクトからロードTextureします
            var texture = textureFormatFile.LoadTexture(fileBinary);
            return texture;
        }
    }
    return null;
}
```
#### Webランタイム

```
public async Awaitable<Texture2D> LoadRawTexture(){
    string url = System.IO.Path.Combine(Application.streamingAssetsPath, "test.astc");

    using (var fileBinary = await UnsafeFileReadUtility.LoadWithWebRequest(url, Allocator.Temp)){
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
private void OnFailedLoadFile(string file, int width, int height){
    Debug.LogError("Failed LoadFile " + file + "::" + width + "x" + height);
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


## パッケージ同梱サンプル
本パッケージには実行できるサンプルが複数同梱されています。<br />
[詳細はコチラ](Documentation~/Samples.ja.md)
