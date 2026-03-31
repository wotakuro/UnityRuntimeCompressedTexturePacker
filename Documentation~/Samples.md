# Samples Included in the Package

## Common StreamingAssets Importing
Please import this before running the other samples.<br />
Importing this will install the necessary sample data required for running the other samples into the StreamingAssets folder.<br />
There is no problem if you install `Samples~/RCTP_StreamingAssetsData.unitypackage` directly.

## 01_SingleTextureLoad
A sample that directly loads astc, ktx, and dds files located in the StreamingAssets directory.<br />

![Sample01 Behavior](img/Sample01.gif) 

<details>
<summary>Screen Explanation</summary>

![Sample01 Screenshot](img/Sample01_ScreenShot.png) <br />
1. Specify a file located under StreamingAssets.<br/>
2. Loads the Texture file at the specified path.<br/>
3. Displays the loaded result.<br/>

</details>

## 02_TextureListInStreamingAssets
A sample that loads all readable image files under StreamingAssets and displays them in a scroll view.<br />

![Sample02 Behavior](img/Sample02.gif) 

<details>
<summary>Screen Explanation</summary>

![Sample02 Screenshot](img/Sample02_ScreenShot.png) <br />
1. Displays the compressed texture formats supported by the currently running platform.<br/>
2. Displays all readable texture files located under StreamingAssets.<br/>
</details>

## 03_AutoAtlasGenerate
A sample that loads multiple files placed in the StreamingAssets folder and automatically generates an Atlas texture and sprites.<br />

![Sample03 Behavior](img/Sample03.gif) 

<details>
<summary>Screen Explanation</summary>
  
![Sample03 Screenshot](img/Sample03_ScreenShot.png) <br />
1. Specify the texture file extension. You can choose from ASTC, KTX, or DDS.<br/>
2. Starts loading files with the specified extension.<br/>
3. A list of Sprites created from the loaded files.<br/>
4. The Atlas texture where the Sprites are packed.<br/>
</details>

## 04_IncrementalAtlasGeneration
This sample performs similar processing to "03_AutoAtlasGenerate". The difference is that it generates the atlas incrementally, allowing you to observe the generation process.<br />

![Sample04 Behavior](img/Sample04.gif) 

<details>
<summary>Screen Explanation</summary>
  
![Sample04 Screenshot](img/Sample04_ScreenShot.png) <br />
1. Specify the texture file extension. You can choose from ASTC, KTX, or DDS.<br/>
2. Starts loading files with the specified extension.<br/>
3. A list of Sprites created from the loaded files.<br/>
4. The Atlas texture where the Sprites are packed.<br/>
</details>

## 05_ReuseAtlasForFixedSizeImages
This demonstrates the functionality to display a large number of icons in a scroll view or similar UI.
If the loaded icon sprites do not fit into the atlas, the oldest sprites are automatically removed using the LRU (Least Recently Used) algorithm.<br />

![Sample05 Behavior](img/Sample05.gif) 

<details>
<summary>Screen Explanation</summary>
  
![Sample05 Screenshot](img/Sample05_ScreenShot.png) <br />
1. A scroll view listing icons. Images are loaded dynamically as you scroll.<br/>
2. Displays the compression format of the current Atlas texture.<br/>
3. The current Atlas texture.<br/>

</details>