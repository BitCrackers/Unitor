using AssetsTools.NET.Extra;
using System.IO;
using System.Text.RegularExpressions;

namespace Unitor.Core.Assets.Types
{
    public class Level : IAsset
    {
        public int LevelNumber;
        public AssetsFileInstance Instance;

        public Level(string path, AssetModel model)
        {
            string levelName = Path.GetFileNameWithoutExtension(path);
            if (!Regex.IsMatch(levelName, @"^level\d+$"))
            {
                throw new AssetIncorrectTypeLoaderException(AssetType.Level, path);
            }
            if (Regex.Match(levelName, @"^level(?<levelnr>\d+)$").Groups.TryGetValue("levelnr", out Group levelNr))
            {
                LevelNumber = int.Parse(levelNr.Value);
            }
            Instance = model.Manager.LoadAssetsFile(path, true);
        }
        public AssetType GetAssetType()
        {
            return AssetType.Level;
        }
    }
}
