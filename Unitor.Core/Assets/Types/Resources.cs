using AssetsTools.NET.Extra;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Unitor.Core.Assets.Types
{
    public class Resources : IAsset
    {
        private readonly AssetsFileInstance instance;
        public AssetsFileInstance Instance { get => instance; }
        private readonly List<string> internalAssets;
        public List<string> InternalAssets { get => internalAssets; }

        public Resources(string path, AssetModel model)
        {
            string levelName = Path.GetFileName(path);
            if (levelName != "resources.assets")
            {
                throw new AssetIncorrectTypeLoaderException(AssetType.Resources, path);
            }
            instance = model.Manager.LoadAssetsFile(path, true);
            internalAssets = Instance.file.typeTree.unity5Types.Select(t => t.stringTable).ToList();
        }

        public AssetType GetAssetType()
        {
            return AssetType.GlobalGameManager;
        }
    }
}
