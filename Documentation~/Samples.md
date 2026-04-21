# Samples Included in the Package

## Common StreamingAssets Importing
Please import this before running other samples.<br />
Upon importing, the assets necessary for running other samples will be installed under the StreamingAssets folder.<br />
You can also install `Samples~/RCTP_StreamingAssetsData.unitypackage` directly without any issues.

## 01_SingleTextureLoad
A sample that directly loads astc, ktx, and dds files located in the StreamingAssets directory.<br />

### Runtime
![Sample01 in action](img/Sample01.gif) 

<details>
<summary>UI Description</summary>

![Sample01 ScreenShot](img/Sample01_ScreenShot.png) <br />
1. Specify a file located under StreamingAssets.<br/>
2. Loads the Texture file at the specified path.<br/>
3. Displays the loaded result.<br/>

</details>

### Editor
Call it from the menu: `Samples/RuntimeCompressedTexturePacker/SingleTextureLoadSampleEditor`.<br />
You can verify similar behavior to the runtime directly within an EditorWindow.


## 02_TextureListInStreamingAssets

### Runtime
A sample that loads all readable image files under StreamingAssets and displays them in a ScrollView.<br />

![Sample02 in action](img/Sample02.gif) 

<details>
<summary>UI Description</summary>

![Sample02 ScreenShot](img/Sample02_ScreenShot.png) <br />
1. Displays the compressed texture formats supported by the currently running platform.<br/>
2. Displays all readable texture files located under StreamingAssets.<br/>
</details>

### Editor
Call it from the menu: `Samples/RuntimeCompressedTexturePacker/TextureListSampleEditor`.<br />
You can verify similar behavior to the runtime directly within an EditorWindow.


## 03_AutoAtlasGenerate

### Runtime
A sample that reads multiple files placed in the StreamingAssets folder and automatically generates an Atlas texture and sprites.<br />

![Sample03 in action](img/Sample03.gif) 

### Editor
Call it from the menu: `Samples/RuntimeCompressedTexturePacker/AutoAtlasBuildSampleEditor`.<br />
You can verify similar behavior to the runtime directly within an EditorWindow.


<details>
<summary>UI Description</summary>
  
![Sample03 ScreenShot](img/Sample03_ScreenShot.png) <br />
1. Specify the texture file extension. You can choose from ASTC, KTX, or DDS.<br/>
2. Starts loading files with the specified extension.<br/>
3. A list of Sprites created by