using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using NUnit.Framework.Interfaces;
using Unity.Collections;
using UnityEngine;
using UnityEngine.LightTransport;

namespace UTJ.RuntimeCompressedTexturePacker.Format {

    public unsafe struct DdsTextureFormat : ITextureFormatFile
    {
        private enum DXGI_FORMAT : uint
        {
            DXGI_FORMAT_BC1_TYPELESS = 70,
            DXGI_FORMAT_BC1_UNORM = 71,
            DXGI_FORMAT_BC1_UNORM_SRGB = 72,
            DXGI_FORMAT_BC2_TYPELESS = 73,
            DXGI_FORMAT_BC2_UNORM = 74,
            DXGI_FORMAT_BC2_UNORM_SRGB = 75,
            DXGI_FORMAT_BC3_TYPELESS = 76,
            DXGI_FORMAT_BC3_UNORM = 77,
            DXGI_FORMAT_BC3_UNORM_SRGB = 78,
            DXGI_FORMAT_BC4_TYPELESS = 79,
            DXGI_FORMAT_BC4_UNORM = 80,
            DXGI_FORMAT_BC4_SNORM = 81,
            DXGI_FORMAT_BC5_TYPELESS = 82,
            DXGI_FORMAT_BC5_UNORM = 83,
            DXGI_FORMAT_BC5_SNORM = 84,
            DXGI_FORMAT_BC6H_TYPELESS = 94,
            DXGI_FORMAT_BC6H_UF16 = 95,
            DXGI_FORMAT_BC6H_SF16 = 96,
            DXGI_FORMAT_BC7_TYPELESS = 97,
            DXGI_FORMAT_BC7_UNORM = 98,
            DXGI_FORMAT_BC7_UNORM_SRGB = 99,
        }

        private enum DdsFourCC :uint
        {
            DX10 = 0x30315844,
            DXT1 = 0x31545844,
            DXT5 = 0x35545844,
        }

        private uint dwSize;
        private uint dwFlag;
        private uint dwHeight;
        private uint dwWidth;
        private uint dwPitchOrLinearSize;
        private uint dwDepth;
        private uint dwMipMapCount;
        private fixed uint dwReserved1[11];
        // ddspf
        private uint ddspf_dwSize;
        private uint ddspf_dwFlags;
        private uint ddspf_dwFourCC;
        private uint ddspf_dwRGBBitCount;
        private uint ddspf_dwRBitMask;
        private uint ddspf_dwGBitMask;
        private uint ddspf_dwBBitMask;
        private uint ddspf_dwABitMask;
        private uint dwCaps;
        private uint dwCaps2;
        private uint dwCaps3;
        private uint dwCaps4;
        private uint dwReserved2;
        // DXT10拡張
        private uint dxt10_dxgiFormat;
        private uint dxt10_resourceDimension;
        private uint dxt10_miscFlag;
        private uint dxt10_arraySize;
        private uint dxt10_miscFlags2;
        private bool hasDxt10Extention;

        public int width => (int)dwWidth;

        public int height => (int)dwHeight;

        public TextureFormat textureFormat
        {
            get
            {
                if(hasDxt10Extention)
                {
                    switch (dxt10_dxgiFormat)
                    {
                        case (uint)DXGI_FORMAT.DXGI_FORMAT_BC1_TYPELESS:
                        case (uint)DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
                        case (uint)DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB:
                            return TextureFormat.DXT1;


                        case (uint)DXGI_FORMAT.DXGI_FORMAT_BC3_TYPELESS:
                        case (uint)DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
                        case (uint)DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB:
                            return TextureFormat.DXT5;

                        case (uint)DXGI_FORMAT.DXGI_FORMAT_BC7_TYPELESS:
                        case (uint)DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
                        case (uint)DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB:
                            return TextureFormat.BC7;
                    }
                }
                else
                {
                    switch (ddspf_dwFourCC)
                    {
                        case (uint)DdsFourCC.DXT1:
                            return TextureFormat.DXT1;
                        case (uint)DdsFourCC.DXT5:
                            return TextureFormat.DXT5;
                    }
                }
                return TextureFormat.RGBA32;
            }
        }

        public bool IsValid
        {
            get
            {
                if(this.textureFormat == TextureFormat.RGBA32){
                    return false;
                }
                return true;
            }
        }

        public NativeArray<byte> GeImageDataWithoutMipmap(NativeArray<byte> fileBinary)
        {
            int head = 128;
            if (this.hasDxt10Extention)
            {
                head += 20;
            }
            return fileBinary.GetSubArray(head, fileBinary.Length - head);
        }

        public bool LoadHeader(NativeArray<byte> fileBinary)
        {
            this.dwSize = BytesToOtherTypesUtility.ReadUintFast(fileBinary,4);
            this.dwFlag = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 8);
            this.dwHeight = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 12);        
            this.dwWidth = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 16);
            this.dwPitchOrLinearSize = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 20);
            this.dwDepth = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 24);
            this.dwMipMapCount = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 28);
            for(int i = 0; i < 11; ++i)
            {
                this.dwReserved1[i] = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 32 + i * 4 );
            }        
            // ddspf
            this.ddspf_dwSize = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 76); 
            this.ddspf_dwFlags = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 80); 
            this.ddspf_dwFourCC = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 84); 
            this.ddspf_dwRGBBitCount = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 88); 
            this.ddspf_dwRBitMask = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 92); 
            this.ddspf_dwGBitMask = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 96);
            this.ddspf_dwBBitMask = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 100); 
            this.ddspf_dwABitMask = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 104); 
            this.dwCaps = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 108); 
            this.dwCaps2 = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 112); 
            this.dwCaps3 = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 116); 
            this.dwCaps4 = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 120); 
            this.dwReserved2 = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 124);


            // 基本ないはずですが、BigEndianなら反転
            if (!BytesToOtherTypesUtility.IsLittleEndianRuntime())
            {
                this.dwSize =　BytesToOtherTypesUtility.SwapUintEndian(this.dwSize);
                this.dwFlag = BytesToOtherTypesUtility.SwapUintEndian(this.dwFlag);
                this.dwHeight = BytesToOtherTypesUtility.SwapUintEndian(this.dwHeight);
                this.dwWidth = BytesToOtherTypesUtility.SwapUintEndian(this.dwWidth);
                this.dwPitchOrLinearSize = BytesToOtherTypesUtility.SwapUintEndian(this.dwPitchOrLinearSize);
                this.dwDepth = BytesToOtherTypesUtility.SwapUintEndian(this.dwDepth);
                this.dwMipMapCount = BytesToOtherTypesUtility.SwapUintEndian(this.dwMipMapCount);
                for (int i = 0; i < 11; ++i)
                {
                    this.dwReserved1[i] = BytesToOtherTypesUtility.SwapUintEndian(this.dwReserved1[i]);
                }
                // ddspf
                this.ddspf_dwSize = BytesToOtherTypesUtility.SwapUintEndian(this.ddspf_dwSize);
                this.ddspf_dwFlags = BytesToOtherTypesUtility.SwapUintEndian(this.ddspf_dwFlags);
                this.ddspf_dwFourCC = BytesToOtherTypesUtility.SwapUintEndian(this.ddspf_dwFourCC);
                this.ddspf_dwRGBBitCount = BytesToOtherTypesUtility.SwapUintEndian(this.ddspf_dwRGBBitCount);
                this.ddspf_dwRBitMask = BytesToOtherTypesUtility.SwapUintEndian(this.ddspf_dwRBitMask);
                this.ddspf_dwGBitMask = BytesToOtherTypesUtility.SwapUintEndian(this.ddspf_dwGBitMask);
                this.ddspf_dwBBitMask = BytesToOtherTypesUtility.SwapUintEndian(this.ddspf_dwBBitMask);
                this.ddspf_dwABitMask = BytesToOtherTypesUtility.SwapUintEndian(this.ddspf_dwABitMask);
                this.dwCaps = BytesToOtherTypesUtility.SwapUintEndian(this.dwCaps);
                this.dwCaps2 = BytesToOtherTypesUtility.SwapUintEndian(this.dwCaps2);
                this.dwCaps3 = BytesToOtherTypesUtility.SwapUintEndian(this.dwCaps3);
                this.dwCaps4 = BytesToOtherTypesUtility.SwapUintEndian(this.dwCaps4);
                this.dwReserved2 = BytesToOtherTypesUtility.SwapUintEndian(this.dwReserved2);
            }

            if (HasDXT10Extention(this.ddspf_dwFourCC))
            {
                this.dxt10_dxgiFormat = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 128);
                this.dxt10_resourceDimension = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 132);
                this.dxt10_miscFlag = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 136);
                this.dxt10_arraySize = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 140);
                this.dxt10_miscFlags2 = BytesToOtherTypesUtility.ReadUintFast(fileBinary, 144);

                if (!BytesToOtherTypesUtility.IsLittleEndianRuntime())
                {
                    this.dxt10_dxgiFormat = BytesToOtherTypesUtility.SwapUintEndian(this.dxt10_dxgiFormat);
                    this.dxt10_resourceDimension = BytesToOtherTypesUtility.SwapUintEndian(this.dxt10_resourceDimension);
                    this.dxt10_miscFlag = BytesToOtherTypesUtility.SwapUintEndian(this.dxt10_miscFlag);
                    this.dxt10_arraySize = BytesToOtherTypesUtility.SwapUintEndian(this.dxt10_arraySize);
                    this.dxt10_miscFlags2 = BytesToOtherTypesUtility.SwapUintEndian(this.dxt10_miscFlags2);
                }
                hasDxt10Extention = true;
            }
            else
            {
                hasDxt10Extention = false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasDXT10Extention(uint val) {
            return (val == (uint)DdsFourCC.DX10);
        }

        public Texture2D LoadTexture(NativeArray<byte> fileBinary, bool isLinearColor = false, bool useMipmap = false)
        {
            if (!this.LoadHeader(fileBinary))
            {
                return null;
            }
            if (!this.IsValid)
            {
                return null;
            }
            var tex = new Texture2D((int)width, (int)height, this.textureFormat, false, isLinearColor);
            if (tex != null)
            {
                var rawData = this.GeImageDataWithoutMipmap(fileBinary);
                tex.LoadRawTextureData(rawData);
                tex.Apply();
            }
            return tex;
        }

        public static bool SignatureValid(NativeArray<byte> fileBinary)
        {

            if(fileBinary[0] == 0x44 && fileBinary[0] == 0x44 && fileBinary[0] == 0x53 && fileBinary[0] == 0x20)
            {
                return true;
            }
            return false;
        }
    }
}
