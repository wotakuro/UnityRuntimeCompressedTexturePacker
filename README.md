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
astcenc -cs input.png output.astc 4x4 -exhaustive -yflip
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
    string[] loadFilesInStreamingAssets = {
        System.IO.Path.Combine(Application.streamingAssetsPath, "test1.astc"),
        System.IO.Path.Combine(Application.streamingAssetsPath, "test2.astc"),
    };
    this.StartCoroutine(autoAtlasBuilder.LoadAndPackAsyncCoroutine(loadFiles, this.OnCompleteLoadAndPack, OnFailedLoadFile));
}

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

private void OnFailedLoadFile(string file, int width, int height)
{
    Debug.LogError("Failed LoadFile " + file + "::" + width + "x" + height);
}
```

