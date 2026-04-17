using System;

namespace Orora.ImageObjectForge
{
    [Serializable]
    internal class ForgeCropMeta
    {
        public int version = 2;
        public string sourceAssetPath;
        public string sourceGUID;
        public SourceSize sourceSize;
        public int sourceBitDepth = 0;
        public int sourceColorType = -1;
        public int sourceDpiX = 0;
        public int sourceDpiY = 0;
        public CropBounds cropBounds;
        public string yConvention = "up";
        public string createdAt;
        public string toolVersion = "ImageObjectForge 0.2";

        [Serializable]
        public struct SourceSize
        {
            public int width;
            public int height;
        }

        [Serializable]
        public struct CropBounds
        {
            public int x;
            public int y;
            public int width;
            public int height;
        }
    }
}
