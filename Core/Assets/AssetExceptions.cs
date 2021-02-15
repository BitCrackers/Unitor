using System;

namespace Unitor.Core.Assets
{
    class AssetLoadException : Exception
    {
        public AssetLoadException(string file) : base($"Unable to load file {file}")
        {

        }
    }

    class AssetTypeDetectionException : Exception
    {
        public AssetTypeDetectionException(string file) : base($"Unable to detect asset type for {file}")
        {

        }
    }
    class AssetIncorrectTypeLoaderException : Exception
    {
        public AssetIncorrectTypeLoaderException(AssetType type, string file) : base($"Incorrect type {type} for asset {file}")
        {

        }
    }
}
