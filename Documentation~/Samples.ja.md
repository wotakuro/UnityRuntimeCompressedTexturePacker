# Package同梱のサンプル

## Common StreamingAssets Importing
他のサンプルを実行する前にインポートしてください。<br />
インポートすると、StreamingAsset以下に他のサンプル実行に必要なサンプルをインストールします。<br />
Samples~/RCTP_StreamingAssetsData.unitypackage を直接インストールしても問題ありません。

## 01_SingleTextureLoad
StreamingAssets ディレクトリにある astc,ktx,ddsファイルを直接読み込むサンプルです。<br />

### ランタイム
![Sample01 動作の様子](img/Sample01.gif) 

<details>
<summary>画面解説</summary>

![Sample01 スクリーンショット](img/Sample01_ScreenShot.png) <br />
1.StreamingAssets以下にあるファイルを指定してください<br/>
2.指定されたパスにあるTextureファイルをロードします<br/>
3.ロードされた結果を表示します<br/>

</details>

### Editor
メニュー「Samples/RuntimeCompressedTexturePacker/SingleTextureLoadSampleEditor」より呼び出しします。<br />
実行時と同じような動作をEditorWindow上で確認できます。


## 02_TextureListInStreamingAssets

### ランタイム
StreamingAsset以下にある全ての読み込み可能な画像ファイルを読み込み、スクロールビューに表示するサンプルです。<br />

![Sample02 動作の様子](img/Sample02.gif) 

<details>
<summary>画面解説</summary>

![Sample02 スクリーンショット](img/Sample02_ScreenShot.png) <br />
1.実行中のプラットフォームがサポートしている圧縮テクスチャフォーマットを表示します<br/>
2.StreamingAsset以下にあるTextureで読み込み可能なファイルを全て表示します<br/>
</details>

### Editor
メニュー「Samples/RuntimeCompressedTexturePacker/TextureListSampleEditor」より呼び出しします。<br />
実行時と同じような動作をEditorWindow上で確認できます。


## 03_AutoAtlasGenerate

### ランタイム
StreamingAssetsフォルダ内に配置された複数のファイルを読み込んで、Atlasテクスチャとスプライトを自動的に生成するサンプルです。<br />

![Sample03 動作の様子](img/Sample03.gif) 

### Editor
メニュー「Samples/RuntimeCompressedTexturePacker/AutoAtlasBuildSampleEditor」」より呼び出しします。<br />
実行時と同じような動作をEditorWindow上で確認できます。


<details>
<summary>画面解説</summary>
  
![Sample03 スクリーンショット](img/Sample03_ScreenShot.png) <br />
1.テクスチャファイル拡張子を指定します。ASTC/KTX/DDSの三つから選べます<br/>
2.指定された拡張子のファイルで読み込みを開始します<br/>
3.ファイルからロードされて作成されたSpriteの一覧です<br/>
4.SpriteがパックされているAtlasテクスチャです<br/>
</details>

## 04_IncrementalAtlasGeneration

### ランタイム
このサンプルは、「03_AutoAtlasGenerate」と同様の処理を行います。違いは、アトラスを段階的に生成するため、生成プロセスを確認できる点です。<br />

![Sample04 動作の様子](img/Sample04.gif) 


<details>
<summary>画面解説</summary>
  
![Sample04 スクリーンショット](img/Sample04_ScreenShot.png) <br />
1.テクスチャファイル拡張子を指定します。ASTC/KTX/DDSの三つから選べます<br/>
2.指定された拡張子のファイルで読み込みを開始します<br/>
3.ファイルからロードされて作成されたSpriteの一覧です<br/>
4.SpriteがパックされているAtlasテクスチャです<br/>
</details>

## 05_ReuseAtlasForFixedSizeImages

### ランタイム
スクロールビュー等で、多数のアイコンを表示する機能を示しています。
読み込まれたアイコンスプライトがアトラスに収まらない場合、LRU（Least Recently Used）アルゴリズムを用いて、古いスプライトが自動的に削除されます。<br />

![Sample05 動作の様子](img/Sample05.gif) 

<details>
<summary>画面解説</summary>
  
![Sample05 スクリーンショット](img/Sample05_ScreenShot.png) <br />
1.アイコン一覧が並んでいるスクロールビューです。スクロールしたら動的に読み込みを行います<br/>
2.現在のAtlasテクスチャの圧縮フォーマットを表示します<br/>
3.現在のAtlasテクスチャです<br/>

</details>

## 05Alternative_UITK

### ランタイム
「05_ReuseAtlasForFixedSizeImages」のUI Toolkit版サンプルです

### Editor
メニュー「Samples/RuntimeCompressedTexturePacker/ReuseAtlasUITKEditorSample」より呼び出しします。<br />
実行時と同じような動作をEditorWindow上で確認できます。


## 06_EncryptedDataLoad

### ランタイム
暗号化されたファイルの読み込みのサンプルです。<br />

![Sample05 動作の様子](img/Sample06.gif)
 
<details>
<summary>画面解説</summary>
  
![Sample06 スクリーンショット](img/Sample06_ScreenShot.png) <br />
1.テクスチャファイルの種類を指定します。ASTC/KTX/DDSの三つから選べます<br/>
2.複数のファイルを読み込み、Atlasテクスチャ作成並びにSprite作成を行います<br />
3.生成されたSpriteを表示します<br />
4.読み込んで生成されたSprite用のAtlasテクスチャを表示します<br />
5.単一テクスチャ読み込み<br />
6.読み込んだ単一テクスチャ<br />

</details>


### Editor
メニュー「Samples/RuntimeCompressedTexturePacker/GenerateEncryptTexture/SelectTargetFile」より呼び出します。<br />
ファイルを選択し、選択したファイルの暗号化されたファイルを生成します。選択ファイルと同じディレクトリ以下にファイルを生成します。<br/>
<br />
メニュー「Samples/RuntimeCompressedTexturePacker/GenerateEncryptTexture/SelectDirectory」より呼び出します。<br />
フォルダを選択し、選択したフォルダ内にあるテクスチャーを見つけ、暗号化されたファイルを生成します。選択ディレクトリ以下に暗号化されたファイルを生成します。<br/>

