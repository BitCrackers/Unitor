using System.Windows;
using Unitor.Core.Assets;

namespace Unitor
{
    /// <summary>
    /// Interaction logic for AssetViewer.xaml
    /// </summary>
    public partial class AssetViewer : Window
    {
        public AssetViewer(AssetModel model)
        {
            InitializeComponent();
            Resources.ItemsSource = model.Resources.InternalAssets;
        }
    }
}
