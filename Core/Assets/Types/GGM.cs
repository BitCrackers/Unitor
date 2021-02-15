using AssetsTools.NET.Extra;
using System.IO;

namespace Unitor.Core.Assets.Types
{
    public class GGM : IAsset
    {
        public AssetsFileInstance Instance;

        public GGM(string path, AssetModel model)
        {
            string levelName = Path.GetFileName(path);
            if (levelName != "globalgamemanagers.assets")
            {
                throw new AssetIncorrectTypeLoaderException(AssetType.GlobalGameManager, path);
            }
            Instance = model.Manager.LoadAssetsFile(path, true);
        }

        public AssetType GetAssetType()
        {
            return AssetType.GlobalGameManager;
        }
    }
}
