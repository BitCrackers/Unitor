using AssetsTools.NET.Extra;
using System.IO;
using System.Text.RegularExpressions;

namespace Unitor.Core.Assets.Types
{
    public class Level : IAsset
    {
        private readonly int levelNumber;
        public int LevelNumber { get => levelNumber; }

        private readonly AssetsFileInstance instance;
        public AssetsFileInstance Instance { get => instance; }

        public Level(string path, AssetModel model)
        {
            string levelName = Path.GetFileNameWithoutExtension(path);
            if (!Regex.IsMatch(levelName, @"^level\d+$"))
            {
                throw new AssetIncorrectTypeLoaderException(AssetType.Level, path);
            }
            if (Regex.Match(levelName, @"^level(?<levelnr>\d+)$").Groups.TryGetValue("levelnr", out Group levelNr))
            {
                levelNumber = int.Parse(levelNr.Value);
            }
            instance = model.Manager.LoadAssetsFile(path, true);
        }
        public AssetType GetAssetType()
        {
            return AssetType.Level;
        }
    }
}
