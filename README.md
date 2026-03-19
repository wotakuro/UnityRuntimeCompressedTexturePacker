# UnityRuntimeCompressedTexturePacker
A Unity runtime utility that dynamically packs compressed textures  to atlases to optimize VRAM and draw calls.(currently .astc only support)


## Support Files
.astc

feature plan
- dds ( BC1-BC7)
- ktx(ETC)

## Convert command
### ASTC Texture file
https://github.com/ARM-software/astc-encoder

```
:: 4x4,5x5,6x6,8x8,10x10,12x12
astcenc -cs input.png output.astc 4x4 -exhaustive -yflip
```
### KTX version1 Texture file( ETC2 - ASTC )
https://developer.imaginationtech.com/solutions/pvrtextool/

```
:: ETC2_RGB_A1,ETC2_RGB,ETC2_RGBA
PVRTexToolCLI -i test.png -o test.ktx -flip y -f ETC2_RGB_A1,UBN,sRGB -ics sRGB -q etcslow

:: ASTC_4x4,ASTC_5x5,ASTC_6x6,ASTC_8x8,ASTC_10x10,ASTC_12x12
PVRTexToolCLI -i test.png -o test.ktx -flip y -f ASTC_4x4,UBN,sRGB -ics sRGB -q astcexhaustive
```

### DDS file (BC1,BC3,BC7)
https://github.com/microsoft/DirectXTex/releases

```
:: BC7_UNORM , BC3_UNORM , BC1_UNORM
Texcov -f BC7_UNORM test.png -nogpu -y -srgi -srgbo -vflip
```
## How to use

### Single astc texture load
```
public Texture2D LoadAstcTexture()
{
    string path = System.IO.Path.Combine(Application.streamingAssetsPath, "test.astc");

    var astcTexture = new AstcTextureFormat();
    using( var fileBinary = UnsafeFileReadUtility.LoadFileSync(path, Unity.Collections.Allocator.Temp) ){
        if (fileBinary.IsCreated)
        {
            var texture = astcTexture.LoadTexture(fileBinary);
            return texture;
        }
    }
    return null;
}
```
### astc texture load and packing 
```
public void AsyncLoadStart()
{
    this.autoAtlasBuilder = new AutoAtlasBuilder(1024, 1024, TextureFormat.ASTC_4x4);
    string[] loadFiles = {
        System.IO.Path.Combine(Application.streamingAssetsPath, "test1.astc"),
        System.IO.Path.Combine(Application.streamingAssetsPath, "test2.astc"),
    };
    this.StartCoroutine(autoAtlasBuilder.LoadAndPackAsyncCoroutine(loadFiles, this.OnCompleteLoadAndPack, OnFailedLoadFile));
}

// callback when the loading and packing process is completed
private void OnCompleteLoadAndPack(IEnumerable<Sprite> sprites)
{
    var texture = autoAtlasBuilder.texture;
    foreach (var sprite in sprites)
    {
       // something to do for generated sprite
    }
    // if you don't need to append textures. reelase buffers.
    this.autoAtlasBuilder.ReleaseBuffers();
}

// callback when the load failed or packing failed
// if loading file is failed the width and height will be negative value.
private void OnFailedLoadFile(string file, int width, int height)
{
    Debug.LogError("Failed LoadFile " + file + "::" + width + "x" + height);
}
```

