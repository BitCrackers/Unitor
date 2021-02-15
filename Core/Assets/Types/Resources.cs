using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Unitor.Core.Assets.Types
{
    public class Resources : IAsset
    {
        public AssetsFileInstance Instance;
        public List<string> InternalAssets;
        public Resources(string path, AssetModel model)
        {
            string levelName = Path.GetFileName(path);
            if (levelName != "resources.assets")
            {
                throw new AssetIncorrectTypeLoaderException(AssetType.Resources, path);
            }
            Instance = model.Manager.LoadAssetsFile(path, true);
            InternalAssets = Instance.table.assetFileInfo.Select((a) =>
            {
                return model.Manager.GetTypeInstance(Instance.file, a).GetBaseField().GetName();
            }
            ).ToList();
        }

        public AssetType GetAssetType()
        {
            return AssetType.GlobalGameManager;
        }
    }
}
