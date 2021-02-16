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
        private readonly List<Level> levels;
        public List<Level> Levels { get => levels; }
        private readonly List<SharedAsset> sharedAssets;
        public List<SharedAsset> SharedAssets { get => sharedAssets; }
        private readonly GGM globalGameManager;
        public GGM GlobalGameManager { get => globalGameManager; }
        private readonly Resources resources;
        public Resources Resources { get => resources; }
        private readonly AssetsManager manager;
        public AssetsManager Manager { get => manager; }

        public AssetModel(string gameDataPath)
        {
            manager = new AssetsManager();

            List<string> files = Directory.GetFiles(gameDataPath).ToList();
            levels = files.Where(f => f.StartsWith("level")).Select(f => new Level(f, this)).ToList();
            Levels.Sort((l1, l2) => l1.LevelNumber.CompareTo(l2.LevelNumber));

            string ggmPath = files.FirstOrDefault(f => Path.GetFileName(f) == "globalgamemanagers.assets");
            if (string.IsNullOrEmpty(ggmPath))
            {
                throw new FileNotFoundException(gameDataPath + Path.DirectorySeparatorChar + "globalgamemanagers.assets");
            }
            globalGameManager = new GGM(ggmPath, this);

            string resourcesPath = files.FirstOrDefault(f => Path.GetFileName(f) == "resources.assets");
            if (string.IsNullOrEmpty(resourcesPath))
            {
                throw new FileNotFoundException(gameDataPath + Path.DirectorySeparatorChar + "resources.assets");
            }
            resources = new Resources(resourcesPath, this);

            sharedAssets = files.Where(f => Regex.IsMatch(f, @"^sharedassets\d+\.assets$")).Select(f => new SharedAsset(f, this)).ToList();
            sharedAssets.Sort((s1, s2) => s1.AssetNumber.CompareTo(s2.AssetNumber));
        }
    }
}
