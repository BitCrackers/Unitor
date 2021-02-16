using System;

namespace Unitor.Core.Assets
{
    public class AssetLoadException : Exception
    {
        public AssetLoadException(string file) : base($"Unable to load file {file}")
        {

        }
    }

    public class AssetTypeDetectionException : Exception
    {
        public AssetTypeDetectionException(string file) : base($"Unable to detect asset type for {file}")
        {

        }
    }

    public class AssetIncorrectTypeLoaderException : Exception
    {
        public AssetIncorrectTypeLoaderException(AssetType type, string file) : base($"Incorrect type {type} for asset {file}")
        {

        }
    }
}
