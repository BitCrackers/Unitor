using System.Collections.Generic;
using System.Windows;

namespace Unitor
{
    /// <summary>
    /// Interaction logic for StringTable.xaml
    /// </summary>
    public partial class StringTable : Window
    {
        public StringTable(IEnumerable<string> strings)
        {
            InitializeComponent();
            Strings.ItemsSource = strings;
        }
    }
}
