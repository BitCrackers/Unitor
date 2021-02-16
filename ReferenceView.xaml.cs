using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Unitor.Core.Reflection;

namespace Unitor
{
    /// <summary>
    /// Interaction logic for ReferenceView.xaml
    /// </summary>
    public partial class ReferenceView : Window
    {
        readonly EventHandler<UnitorMethod> Callback;
        public ReferenceView(UnitorMethod method, EventHandler<UnitorMethod> referenceCallback)
        {
            InitializeComponent();
            References.ItemsSource = method.References;
            Callback = referenceCallback;
        }
        public ReferenceView(KeyValuePair<ulong, string> s, UnitorModel model, EventHandler<UnitorMethod> referenceCallback)
        {
            InitializeComponent();
            References.ItemsSource = model.Types.SelectMany(t => t.Methods).Where(m => m.Strings.Contains(s));
            Callback = referenceCallback;
        }

        private void References_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (References.SelectedItem is UnitorMethod method)
            {
                Callback.Invoke(this, method);
            }
        }
    }
}
