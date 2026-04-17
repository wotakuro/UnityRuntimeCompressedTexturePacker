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

## 03_AutoAtlasGenerate

### ランタイム
StreamingAssetsフォルダ内に配置された複数のファイルを読み込んで、Atlasテクスチャとスプライトを自動的に生成するサンプルです。<br />

![Sample03 動作の様子](img/Sample03.gif) 

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

## 06_EncryptedDataLoad
### ランタイム
暗号化されたファイルの読み込みのサンプルです。

