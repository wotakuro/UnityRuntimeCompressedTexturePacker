# UnityRuntimeCompressedTexturePacker
A utility for the Unity runtime that dynamically generates packed atlases from compressed textures to optimize VRAM and DrawCalls.


[日本語はコチラ](README.ja.md)<br />

## Supported Files
- astc (ASTC_4x4, ASTC_5x5, ASTC_6x6, ASTC_8x8, ASTC_10x10, ASTC_12x12)
- dds (BC1, BC5, BC7)
- ktx (ETC2_RGB, ETC2_RGBA1, ETC_RGBA8, ASTC_4x4, ASTC_5x5, ASTC_6x6, ASTC_8x8, ASTC_10x10, ASTC_12x12)

If you want to handle KTX2 or Basis Universal, please consider using KTX for Unity or similar tools.

## Command Examples for Creating Compressed Textures
When using these in Unity, YFlip is required.

### Generating .astc Textures
- Supported Formats: ASTC format only
- Tool Used: https://github.com/ARM-software/astc-encoder

```cmd
:: 4x4, 5x5, 6x6, 8x8, 10x10, 12x12
astcenc -cs input.png output.astc 4x4 -exhaustive -yflip
```

### KTX version 1 Texture Files
- Supported Formats: ETC2 format, ASTC format
- Tool Used: https://developer.imaginationtech.com/solutions/pvrtextool/

```cmd
:: ETC2_RGB_A1, ETC2_RGB, ETC2_RGBA
PVRTexToolCLI -i test.png -o test.ktx -flip y -f ETC2_RGB_A1,UBN,sRGB -ics sRGB -q etcslow

:: ASTC_4x4, ASTC_5x5, ASTC_6x6, ASTC_8x8, ASTC_10x10, ASTC_12x12
PVRTexToolCLI -i test.png -o test.ktx -flip y -f ASTC_4x4,UBN,sRGB -ics sRGB -q astcexhaustive
```

### DDS Files
- Supported Formats: BC1 (DXT1), BC3 (DXT5), BC7
- Tool Used: https://github.com/microsoft/DirectXTex/releases

```cmd
:: BC7_UNORM, BC3_UNORM, BC1_UNORM
Texcov -f BC7_UNORM test.png -nogpu -y -srgi -srgbo -vflip
```

<details><summary>## Other Information</summary>

### AMD Compressonator
https://gpuopen.com/compressonator/

AMD Compressonator can export textures compressed to BC1, BC3, and BC7 into DDS and KTX formats.<br />
However, since it cannot perform Y-flip, it is not possible to export textures that can be correctly loaded in Unity with a single command.

Convert to DDS:
```cmd
:: BC1 / BC3 / BC7
compressonatorcli -fd BC7 -Quality 1.0 test.png test.dds
```

Convert to KTX:
```cmd
:: BC1 / BC3 / BC7
compressonatorcli -fd BC7 -Quality 1.0 test.png test.ktx
```

### ImageMagick
https://imagemagick.org/#gsc.tab=0

ImageMagick is famous as open software for image conversion, but it is rarely used for game purposes.<br />
It can convert images to BC1 and BC3 DDS formats.

```cmd
:: dxt1 (BC1), dxt5 (BC3)
magick input.png -flip -define dds:compression=dxt5 output.dds
```

</details>

## Sample Code

### Notes
The supported Texture formats vary depending on the runtime platform you are running on.<br />
You need to check if the TextureAtlas you are trying to create can be created in the current execution environment using `SystemInfo.SupportsTextureFormat`.<br />
* When running in the Editor, there is a Decompressor, so even unsupported Textures can be loaded. However, this may not be possible in a built app.<br />
<br />
[Click here for details](https://docs.unity3d.com/Manual/texture-choose-format-by-platform.html)

### Loading a Single File

#### Synchronous loading ( exclude Web runtimes)
```csharp
public Texture2D LoadRawTexture(){
    string path = System.IO.Path.Combine(Application.streamingAssetsPath, "test.astc");
    // Stores the contents read from the file into NativeArray<byte>
    using( var fileBinary = UnsafeFileReadUtility.LoadFileSync(path, Unity.Collections.Allocator.Temp) ){
        // Determines the appropriate file format object based on the file binary
        var textureFormatFile = TextureFileFormatUtility.GetTextureFileFormatObject(fileBinary);
        // Loads the Texture from the created object
        var texture = textureFormatFile.LoadTexture(fileBinary);
        return texture;
    }
    return null;
}
```

#### All platform (WebRuntime requires Asynchronous loading.)
```csharp
public async Awaitable<Texture2D> LoadRawTexture(){
    string url = System.IO.Path.Combine(Application.streamingAssetsPath, "test.astc");

    using (var fileBinary = await UnsafeFileReadUtility.LoadAsync(url)){
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
private void OnFailedLoadFile(string file, AtlasFailedReason reason, int width, int height){
    Debug.LogError("Failed LoadFile " + file + "::" + reason + "::" + width + "x" + height);
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

### Supporting Custom Formats (e.g., Encrypted Files)

To support custom formats, you need to do the following two things:
 - 1. Implement a struct/class that inherits from `ITextureFileFormat`
 - 2. Add a file format detection process using the `TextureFileFormatUtility.SetAppendFormatDelagete`

```csharp
/// 1. Implement a class that inherits from ITextureFileFormat

/// <summary>
/// Encrypted file texture format (Custom)
/// </summary>
public class EncryptedTextureFileFormat : ITextureFileFormat{
    // First 4 bytes Signature
    private const uint Signature = 0x5446594DU;
    // XOR Encryption Key
    private const uint EncryptKey = 0x20534444U;

    // Texture width
    private uint textureWidth;
    // Texture height
    private uint textureHeight;
    // Texture format
    private TextureFormat format;

    /// <summary>
    /// [interface implementation] Texture width
    /// </summary>
    public int width => (int)textureWidth;

    /// <summary>
    /// [interface implementation] Texture height
    /// </summary>
    public int height => (int)textureHeight;

    /// <summary>
    /// [interface implementation] Texture format
    /// </summary>
    public TextureFormat textureFormat => format;

    /// <summary>
    /// [interface implementation] Is it a valid Texture?
    /// </summary>
    public bool IsValid => (width > 0 && height > 0);

    /// <summary>
    /// [interface implementation] Returns the actual texture without Mipmaps from the file contents
    /// </summary>
    /// <param name="fileBinary">File contents</param>
    /// <returns>Returns the created texture bytes</returns>
    public NativeArray<byte> GeImageDataWithoutMipmap(NativeArray<byte> fileBinary)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(fileBinary.Length - 16, Allocator.Temp);

        unsafe{
            uint* src = (uint*)(fileBinary.GetUnsafePtr()) + 4;
            uint* dst = (uint*)(bytes.GetUnsafePtr());
            int size = (fileBinary.Length - 16) / 4;
            for (int i = 0; i < size; i++){
                *dst = *src ^ EncryptKey;
                ++dst; ++src;
            }
        }
        return bytes;
    }

    /// <summary>
    /// [interface implementation] Load header
    /// </summary>
    /// <param name="fileBinary">File contents</param>
    /// <returns>Returns false if loading fails</returns>
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
    /// [interface implementation] Load Texture
    /// </summary>
    /// <param name="fileBinary">File contents</param>
    /// <param name="isLinearColor">Is it linear color?</param>
    /// <param name="useMipmap">Should Mipmaps be considered?</param>
    /// <returns>Created Texture</returns>
    public Texture2D LoadTexture(NativeArray<byte> fileBinary, bool isLinearColor = false, bool useMipmap = false){
        // Create texture without considering Mipmaps
        return TextureFileFormatUtility.CreateTextureWithoutMipmap(this, fileBinary, isLinearColor);
    }

    /// <summary>
    /// Looks at the first bytes and returns whether it matches this format.
    /// </summary>
    /// <param name="fileBinary">Binary data of the file</param>
    /// <returns>True if the Signature matches</returns>
    public static bool SignatureValid(NativeArray<byte> fileBinary){
        var signature = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 0);
        return (signature == Signature);
    }
}
```

```csharp
/// 2. Configuration for TextureFileFormatUtility.appendFormatDetelctFuncction

/// Process at initialization
[RuntimeInitializeOnLoadMethod]
static void RegisterFileFormats(){
    TextureFileFormatUtility.SetAppendFormatDelagete(GetAppTextureFileFormat);
}

/// Looks at the contents of the file and determines the Texture type
private static ITextureFileFormat GetAppTextureFileFormat(NativeArray<byte> fileBinary){
    if (EncryptedTextureFileFormat.SignatureValid(fileBinary)) {
        return new EncryptedTextureFileFormat();
    }
    // Returning NullTextureFile will trigger the system default detection.
    return new NullTextureFile();
}
```

## Included Package Samples
This package includes multiple runnable samples.<br />
[Click here for the Web runtime demo](https://wotakuro.github.io/UnityRuntimeCompressedTexturePacker/)<br />
[Click here for details](Documentation~/Samples.md)
