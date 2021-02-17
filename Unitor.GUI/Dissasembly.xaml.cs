using System.Windows;

namespace Unitor
{
    /// <summary>
    /// Interaction logic for Dissasembly.xaml
    /// </summary>
    public partial class Dissasembly : Window
    {
        public Dissasembly(string text)
        {
            InitializeComponent();
            CodeViewer.Text = text;
        }
    }
}
