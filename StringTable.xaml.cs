using System;
using System.Collections.Generic;
using System.Windows;
using Unitor.Core.Reflection;

namespace Unitor
{
    /// <summary>
    /// Interaction logic for StringTable.xaml
    /// </summary>
    public partial class StringTable : Window
    {
        private readonly UnitorModel Model;
        readonly EventHandler<UnitorMethod> Callback;
        public StringTable(UnitorModel model, EventHandler<UnitorMethod> referenceCallback )
        {
            InitializeComponent();
            Strings.ItemsSource = model.StringTable;
            Model = model;
            Callback = referenceCallback;
        }

        private void Strings_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Strings.SelectedItem is KeyValuePair<ulong, string> s)
            {
                new ReferenceView(s, Model, Callback).Show();
            }
        }
    }
}
