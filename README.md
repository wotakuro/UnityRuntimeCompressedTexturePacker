# UnityRuntimeCompressedTexturePacker
A utility for the Unity runtime that dynamically generates packed atlases from compressed textures to optimize VRAM and DrawCalls.
<br />
[日本語はコチラ](README.ja.md)<br />
## Supported Files
- astc (ASTC_4x4, ASTC_5x5, ASTC_6x6, ASTC_8x8, ASTC_10x10, ASTC_12x12)
- dds (BC1, BC5, BC7)
- ktx (ETC2_RGB, ETC2_RGBA1, ETC_RGBA8, ASTC_4x4, ASTC_5x5, ASTC_6x6, ASTC_8x8, ASTC_10x10, ASTC_12x12)

## Command Examples for Creating Compressed Textures
When using these in Unity, YFlip is required.

### Generating .astc Textures
- Supported Formats: ASTC format only
- Tool Used: [https://github.com/ARM-software/astc-encoder](https://github.com/ARM-software/astc-encoder)

```cmd
:: 4x4, 5x5, 6x6, 8x8, 10x10, 12x12
astcenc -cs input.png output.astc 4x4 -exhaustive -yflip
```

### KTX version 1 Texture Files
- Supported Formats: ETC2 format, ASTC format
- Tool Used: [https://developer.imaginationtech.com/solutions/pvrtextool/](https://developer.imaginationtech.com/solutions/pvrtextool/)

```cmd
:: ETC2_RGB_A1, ETC2_RGB, ETC2_RGBA
PVRTexToolCLI -i test.png -o test.ktx -flip y -f ETC2_RGB_A1,UBN,sRGB -ics sRGB -q etcslow

:: ASTC_4x4, ASTC_5x5, ASTC_6x6, ASTC_8x8, ASTC_10x10, ASTC_12x12
PVRTexToolCLI -i test.png -o test.ktx -flip y -f ASTC_4x4,UBN,sRGB -ics sRGB -q astcexhaustive
```

### DDS Files
- Supported Formats: BC1 (DXT1), BC3 (DXT5), BC7
- Tool Used: [https://github.com/microsoft/DirectXTex/releases](https://github.com/microsoft/DirectXTex/releases)

```cmd
:: BC7_UNORM, BC3_UNORM, BC1_UNORM
Texcov -f BC7_UNORM test.png -nogpu -y -srgi -srgbo -vflip
```

## Sample Code
### Important Notes
The supported texture formats vary depending on the runtime platform being used.<br />
You must use `SystemInfo.SupportsTextureFormat` to verify that the texture atlas you are attempting to create can be generated in the runtime environment.<br/>
*Note: Since the Editor includes a decompressor, it can load unsupported textures, but this may not be possible in the built app. <br />
<br />
[Click here for details](https://docs.unity3d.com/6000.0/Documentation/Manual/texture-choose-format-by-platform.html)


### Loading a Single File 

#### Excluding Web Runtime
```csharp
public Texture2D LoadAstcTexture(){
    string path = System.IO.Path.Combine(Application.streamingAssetsPath, "test.astc");
    // Stores the contents read from the file into NativeArray<byte>
    using( var fileBinary = UnsafeFileReadUtility.LoadFileSync(path, Unity.Collections.Allocator.Temp) ){
        // When the file is created successfully
        if (fileBinary.IsCreated){
            // Determines the appropriate file format object based on the file binary
            var textureFormatFile = TextureFileFormatUtility.GetTextureFileFormatObject(fileBinary);
            // Loads the Texture from the created object
            var texture = textureFormatFile.LoadTexture(fileBinary);
            return texture;
        }
    }
    return null;
}
```

#### WebRuntime

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

### Loading Texture Files and Packing them into an Atlas

#### Asynchronous Loading with async

```csharp
// Object
private AutoAtlasBuilder autoAtlasBuilder;

// Start asynchronous load
public async void AsyncLoadStart(){
    this.autoAtlasBuilder = new AutoAtlasBuilder(1024, 1024, targetTextureFormat);
    // Specify files to load
    string[] loadFiles = {
        System.IO.Path.Combine(Application.streamingAssetsPath, "test1.astc"),
        System.IO.Path.Combine(Application.streamingAssetsPath, "test2.astc"),
    };
    // The return value will contain the list of loaded sprites
    var sprites = await autoAtlasBuilder.LoadAndPackAsync(loadFiles);
    foreach (var sprite in sprites){
        // Null will be assigned if loading fails
        if(sprite == null){
            continue;
        }
        // Do something with the sprite
    }
    this.autoAtlasBuilder.ReleaseBuffers();
}

// Please call Dispose when it is destroyed
void OnDestroy(){
    this.autoAtlasBuilder.Dispose();
}
```

#### Synchronous Loading (Not supported on Web *Web is currently entirely unsupported)

```csharp
// Object
private AutoAtlasBuilder autoAtlasBuilder;

// Processing part
public void LoadAndPack(){
    this.autoAtlasBuilder = new AutoAtlasBuilder(1024, 1024, targetTextureFormat);
    // Specify files to load
    string[] loadFiles = {
        System.IO.Path.Combine(Application.streamingAssetsPath, "test1.astc"),
        System.IO.Path.Combine(Application.streamingAssetsPath, "test2.astc"),
    };
    // The return value will contain the list of loaded sprites
    var sprites = autoAtlasBuilder.LoadAndPack(loadFiles);
    foreach (var sprite in sprites){
        // Null will be assigned if loading fails
        if(sprite == null){
            continue;
        }
        // Do something with the sprite
    }
    // If there is no need to pack additional Textures, clear the file loading buffer.
    this.autoAtlasBuilder.ReleaseBuffers();
}

// Please call Dispose when it is destroyed
void OnDestroy(){
    this.autoAtlasBuilder.Dispose();
}
```

#### Asynchronous Loading with Coroutine
```csharp
// Object for Atlas creation
private AutoAtlasBuilder autoAtlasBuilder;

// Start asynchronous load with Coroutine
public void AsyncLoadStart(){
    // Specify the size and format of the Atlas texture.
    this.autoAtlasBuilder = new AutoAtlasBuilder(1024, 1024, TextureFormat.ASTC_4x4);
    // Specify files to load
    string[] loadFiles = {
        System.IO.Path.Combine(Application.streamingAssetsPath, "test1.astc"),
        System.IO.Path.Combine(Application.streamingAssetsPath, "test2.astc"),
    };
    // Start the coroutine using the return value of LoadAndPackAsyncCoroutine
    this.StartCoroutine(autoAtlasBuilder.LoadAndPackAsyncCoroutine(loadFiles, this.OnCompleteLoadAndPack, OnFailedLoadFile));
}

// Callback for when packing is complete
private void OnCompleteLoadAndPack(IEnumerable<Sprite> sprites){
    var texture = autoAtlasBuilder.texture;
    foreach (var sprite in sprites){
        // Null will be assigned if loading fails
        if(sprite == null){
            continue;
        }
       // Do something with the sprite
    }
    // If there is no need to pack additional Textures, clear the file loading buffer.
    this.autoAtlasBuilder.ReleaseBuffers();
}

// Handling for when file loading fails
private void OnFailedLoadFile(string file, AutoAtlasBuilder.AtlasFailReason reason, int width, int height){
    Debug.LogError("Failed LoadFile " + file + "::" + reason+ "::" + width + "x" + height);
}

// Please call Dispose when it is destroyed
void OnDestroy(){
    this.autoAtlasBuilder.Dispose();
}
```

### Sample: Reusing the Same Atlas while Swapping Many Sprites 
*(e.g., displaying a large number of icons in a ScrollView)*

```csharp
// Object that manages the atlas, etc.
private RecycleAtlasForFixedSizeImages recycleAtlasForFixed;

// List of icon files to be loaded
private List<string> iconFiles{
    get{
        var iconFiles = new List<string>();
        for(int i = 0; i < 50; ++i){
            iconFiles.Add( System.IO.Path.Combine(Application.streamingAssetsPath,string.Format("icon{0:000}.astc", i) ) );
        }
        return iconFiles;
    }
}
// Icon index that needs to be loaded
private int index = 0;

// Upon initialization
void Awake(){
    // Create the object by specifying the Texture Atlas size, format, and the size of the icons to be loaded
    this.recycleAtlasForFixed = new RecycleAtlasForFixedSizeImages(1024, 1024, textureFormat, 256, 256);
}

// Update process
void Update(){
   var icons = iconFiles;
   for(int i = index ; i < index + 5 && i < icons.Count; ++i ){
       // Request by passing the file path every frame.
       // If the Atlas becomes full, older requests will be discarded first.
       // Returns the Sprite corresponding to the file if it has been created. If null is returned, it is currently loading.
       var sprite = this.recycleAtlasForFixed.Request( icons[i] );
   }
}

// Please call Dispose when it is destroyed
void OnDestroy(){
  recycleAtlasForFixed.Dispose();
}
```

## Included Package Samples
This package includes multiple runnable samples.<br />
[Web runtime demo is available here](https://wotakuro.github.io/UnityRuntimeCompressedTexturePacker/) *Under development<br />
[Click here for details](Documentation~/Samples.md)
