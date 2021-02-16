using AssetsTools.NET.Extra;
using System.IO;
using System.Text.RegularExpressions;

namespace Unitor.Core.Assets.Types
{
    public class SharedAsset
    {
        private int assetNumber;
        public int AssetNumber { get => assetNumber; }

        private AssetsFileInstance instance;
        public AssetsFileInstance Instance { get => instance; }

        public SharedAsset(string path, AssetModel model)
        {
            string levelName = Path.GetFileNameWithoutExtension(path);
            if (!Regex.IsMatch(levelName, @"^sharedassets\d+\.assets$"))
            {
                throw new AssetIncorrectTypeLoaderException(AssetType.Level, path);
            }
            if (Regex.Match(levelName, @"^sharedassets(?<AssetNr>\d+)\.assets$").Groups.TryGetValue("AssetNr", out Group assetNr))
            {
                assetNumber = int.Parse(assetNr.Value);
            }
            instance = model.Manager.LoadAssetsFile(path, true);
        }
        public AssetType GetAssetType()
        {
            return AssetType.SharedAsset;
        }
    }
}
