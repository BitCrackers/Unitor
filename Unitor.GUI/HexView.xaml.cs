using System.IO;
using System.Windows;

namespace Unitor
{
    /// <summary>
    /// Interaction logic for HexView.xaml
    /// </summary>
    public partial class HexViewWindow : Window
    {
        public HexViewWindow(byte[] content, ulong VAS)
        {
            InitializeComponent();
            MemoryStream memStream = new MemoryStream();
            memStream.Write(content);
            BinaryReader reader = new BinaryReader(memStream);
            hexViewer.DataSource = reader;
            hexViewer.Address = VAS;
        }
    }
}
