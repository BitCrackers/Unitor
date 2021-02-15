namespace Unitor.Core.Assets.Types
{
    public enum ExportType
    {
        MonoBehaviour,
        Image,
        None
    }
    public interface IAsset
    {
        abstract public AssetType GetAssetType();
    }
}
