#if (!UNITY_EDITOR &&  UNITY_WEBGL )
#define WEB_RUNTIME_BUILD 
#endif

using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UTJ.RuntimeCompressedTexturePacker.Format;
using System.Linq;
using System;
using UnityEngine.Networking;



namespace UTJ.RuntimeCompressedTexturePacker
{
    public enum AtlasFailedReason
    {
        WebRequestError,
        FileLoadError,
        DataFormatError,
        TextureFormatDifferent,
        NoSpaceInAtlas,
    }
}