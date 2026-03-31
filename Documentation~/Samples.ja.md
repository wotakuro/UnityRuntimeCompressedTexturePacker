# Package同梱のサンプル

## Common StreamingAssets Importing
他のサンプルを実行する前にインポートしてください。<br />
インポートすると、StreamingAsset以下に他のサンプル実行に必要なサンプルをインストールします。<br />
Samples~/RCTP_StreamingAssetsData.unitypackage を直接インストールしても問題ありません。

## 01_SingleTextureLoad
StreamingAssets ディレクトリにある astc,ktx,ddsファイルを直接読み込むサンプルです。

### 画面解説
![Sample01 スクリーンショット](img/Sample01_ScreenShot.png) <br />
1.<br/>
2.<br/>
3.<br/>

## 02_TextureListInStreamingAssets
StreamingAsset以下にある全ての読み込み可能な画像ファイルを読み込み、スクロールビューに表示するサンプルです。

### 画面解説
![Sample02 スクリーンショット](img/Sample02_ScreenShot.png) <br />
1.<br/>
2.<br/>
3.<br/>

## 03_AutoAtlasGenerate
StreamingAssetsフォルダ内に配置された複数のファイルを読み込んで、Atlasテクスチャとスプライトを自動的に生成するサンプルです。


### 画面解説
![Sample03 スクリーンショット](img/Sample03_ScreenShot.png) <br />
1.<br/>
2.<br/>
3.<br/>

## 04_IncrementalAtlasGeneration
このサンプルは、「03_AutoAtlasGenerate」と同様の処理を行います。違いは、アトラスを段階的に生成するため、生成プロセスを確認できる点です。

### 画面解説
![Sample04 スクリーンショット](img/Sample04_ScreenShot.png) <br />
1.<br/>
2.<br/>
3.<br/>


## 05_ReuseAtlasForFixedSizeImages
スクロールビュー等で、多数のアイコンを表示する機能を示しています。
読み込まれたアイコンスプライトがアトラスに収まらない場合、LRU（Least Recently Used）アルゴリズムを用いて、古いスプライトが自動的に削除されます。

### 画面解説
![Sample05 スクリーンショット](img/Sample05_ScreenShot.png) <br />
1.<br/>
2.<br/>
3.<br/>

