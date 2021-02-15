using AssetsTools.NET.Extra;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unitor.Core.Assets.Types;

namespace Unitor.Core.Assets
{
    public class AssetModel
    {
        public List<Level> Levels;
        public List<SharedAsset> SharedAssets;

        public GGM GlobalGameManager;
        public Resources Resources;
        public AssetsManager Manager;

        public AssetModel(string gameDataPath)
        {
            Manager = new AssetsManager();

            List<string> files = Directory.GetFiles(gameDataPath).ToList();
            Levels = files.Where(f => f.StartsWith("level")).Select(f => new Level(f, this)).ToList();
            Levels.Sort((l1, l2) => l1.LevelNumber.CompareTo(l2.LevelNumber));

            string ggmPath = files.FirstOrDefault(f => Path.GetFileName(f) == "globalgamemanagers.assets");
            if (string.IsNullOrEmpty(ggmPath))
            {
                throw new FileNotFoundException(gameDataPath + Path.DirectorySeparatorChar + "globalgamemanagers.assets");
            }
            GlobalGameManager = new GGM(ggmPath, this);

            string resourcesPath = files.FirstOrDefault(f => Path.GetFileName(f) == "resources.assets");
            if (string.IsNullOrEmpty(resourcesPath))
            {
                throw new FileNotFoundException(gameDataPath + Path.DirectorySeparatorChar + "resources.assets");
            }
            Resources = new Resources(resourcesPath, this);

            SharedAssets = files.Where(f => Regex.IsMatch(f, @"^sharedassets\d+\.assets$")).Select(f => new SharedAsset(f, this)).ToList();
            SharedAssets.Sort((s1, s2) => s1.AssetNumber.CompareTo(s2.AssetNumber));
        }
    }
}
