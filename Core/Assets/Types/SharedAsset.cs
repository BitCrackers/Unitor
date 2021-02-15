using AssetsTools.NET.Extra;
using System.IO;
using System.Text.RegularExpressions;

namespace Unitor.Core.Assets.Types
{
    public class SharedAsset
    {
        public int AssetNumber;
        public AssetsFileInstance Instance;

        public SharedAsset(string path, AssetModel model)
        {
            string levelName = Path.GetFileNameWithoutExtension(path);
            if (!Regex.IsMatch(levelName, @"^sharedassets\d+\.assets$"))
            {
                throw new AssetIncorrectTypeLoaderException(AssetType.Level, path);
            }
            if (Regex.Match(levelName, @"^sharedassets(?<AssetNr>\d+)\.assets$").Groups.TryGetValue("AssetNr", out Group assetNr))
            {
                AssetNumber = int.Parse(assetNr.Value);
            }
            Instance = model.Manager.LoadAssetsFile(path, true);
        }
        public AssetType GetAssetType()
        {
            return AssetType.SharedAsset;
        }
    }
}
