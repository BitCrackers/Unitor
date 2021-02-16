using AssetsTools.NET.Extra;
using System.IO;

namespace Unitor.Core.Assets.Types
{
    public class GGM : IAsset
    {
        private AssetsFileInstance instance;
        public AssetsFileInstance Instance { get => instance; }

        public GGM(string path, AssetModel model)
        {
            string levelName = Path.GetFileName(path);
            if (levelName != "globalgamemanagers.assets")
            {
                throw new AssetIncorrectTypeLoaderException(AssetType.GlobalGameManager, path);
            }
            instance = model.Manager.LoadAssetsFile(path, true);
        }

        public AssetType GetAssetType()
        {
            return AssetType.GlobalGameManager;
        }
    }
}
