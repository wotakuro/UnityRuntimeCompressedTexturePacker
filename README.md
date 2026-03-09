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
### astc texture load and packing async


### astc texture load and packing sync
