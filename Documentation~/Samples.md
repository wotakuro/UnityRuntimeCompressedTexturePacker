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
3. A list of Sprites created by loading from the files.<br/>
4. The Atlas texture where the Sprites are packed.<br/>
</details>

## 04_IncrementalAtlasGeneration

### Runtime
This sample performs the same process as "03_AutoAtlasGenerate". The difference is that it generates the atlas incrementally, allowing you to observe the generation process.<br />

![Sample04 in action](img/Sample04.gif) 


<details>
<summary>UI Description</summary>
  
![Sample04 ScreenShot](img/Sample04_ScreenShot.png) <br />
1. Specify the texture file extension. You can choose from ASTC, KTX, or DDS.<br/>
2. Starts loading files with the specified extension.<br/>
3. A list of Sprites created by loading from the files.<br/>
4. The Atlas texture where the Sprites are packed.<br/>
</details>

## 05_ReuseAtlasForFixedSizeImages

### Runtime
Demonstrates the functionality of displaying a large number of icons in a ScrollView or similar UI.
If the loaded icon sprites do not fit into the atlas, older sprites are automatically removed using the LRU (Least Recently Used) algorithm.<br />

![Sample05 in action](img/Sample05.gif) 

<details>
<summary>UI Description</summary>
  
![Sample05 ScreenShot](img/Sample05_ScreenShot.png) <br />
1. A ScrollView listing icons. It loads them dynamically as you scroll.<br/>
2. Displays the compression format of the current Atlas texture.<br/>
3. The current Atlas texture.<br/>

</details>

## 05Alternative_UITK

### Runtime
This is the UI Toolkit version of the "05_ReuseAtlasForFixedSizeImages" sample.

### Editor
Call it from the menu: `Samples/RuntimeCompressedTexturePacker/ReuseAtlasUITKEditorSample`.<br />
You can verify similar behavior to the runtime directly within an EditorWindow.


## 06_EncryptedDataLoad

### Runtime
A sample for loading encrypted files.<br />

![Sample06 in action](img/Sample06.gif)
 
<details>
<summary>UI Description</summary>
  
![Sample06 ScreenShot](img/Sample06_ScreenShot.png) <br />
1. Specify the texture file type. You can choose from ASTC, KTX, or DDS.<br/>
2. Loads multiple files and creates an Atlas texture along with Sprites.<br />
3. Displays the generated Sprites.<br />
4. Displays the Atlas texture used for the loaded and generated Sprites.<br />
5. Single texture load.<br />
6. The loaded single texture.<br />

</details>


### Editor
Call it from the menu: `Samples/RuntimeCompressedTexturePacker/GenerateEncryptTexture/SelectTargetFile`.<br />
Select a file to generate an encrypted version of it. The encrypted file will be generated in the same directory as the selected file.<br/>
<br />
Call it from the menu: `Samples/RuntimeCompressedTexturePacker/GenerateEncryptTexture/SelectDirectory`.<br />
Select a folder to find textures within it and generate encrypted versions. The encrypted files will be generated under the selected directory.<br/>
