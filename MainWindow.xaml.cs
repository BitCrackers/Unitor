using Beebyte_Deobfuscator.Lookup;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Unitor.Core;

namespace Unitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Game game;
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (files.Length > 1) MessageBox.Show("You can only drag one folder at a time");
            if (!Directory.Exists(files[0])) MessageBox.Show("You must drag and drop the whole game folder");
            try
            {
                SelectGame.Visibility = Visibility.Hidden;
                StatusTextContainer.Visibility = Visibility.Visible;
                StatusText.Text = "Loading game...";
                Thread thread = new Thread(() => CreateGame(files[0]));
                thread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured: {ex.Message}");
            }
        }

        public void SetGame(Game g)
        {
            game = g;
            GameInfo.DataContext = game;
            Namespaces.ItemsSource = game.Module.Namespaces;
            Namespaces.SelectedIndex = 0;
            Types.ItemsSource = game.Module.Types.Where(t => t.Namespace == "");
            Types.SelectedIndex = 0;
            Dimmer.Visibility = Visibility.Hidden;
            StatusTextContainer.Visibility = Visibility.Hidden;
        }

        public void CreateGame(string dir)
        {
            Game game = new Game(dir, StatusUpdate);
            Application.Current.Dispatcher.Invoke(new Action(() => SetGame(game)));
        }


        public void StatusUpdate(object sender, string status)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => StatusText.Text = status + "..."));
        }

        private void Namespaces_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (string.IsNullOrEmpty((sender as ListBox).SelectedItem as string)) return;
            Types.ItemsSource = game.Module.Types.Where(t => t.Namespace == (sender as ListBox).SelectedItem.ToString().Replace("<root>", ""));
            Types.SelectedIndex = 0;
            TypeSearch.Text = string.Empty;
        }

        private void NamespaceSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (game == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(NamespaceSearch.Text))
            {
                Namespaces.ItemsSource = game.Module.Namespaces.Where(n => n.ToLower().Contains(NamespaceSearch.Text.ToLower()));
            }
            else
            {
                Namespaces.ItemsSource = game.Module.Namespaces;
            }
            Namespaces.SelectedIndex = 0;
        }

        private void TypeSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (game == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(TypeSearch.Text))
            {
                Types.ItemsSource = game.Module.Types
                    .Where(t => t.Namespace == Namespaces.SelectedItem.ToString().Replace("<root>", ""))
                    .Where(n => n.Name.ToLower().Contains(TypeSearch.Text.ToLower()));
            }
            else
            {
                Types.ItemsSource = game.Module.Types.Where(t => t.Namespace == Namespaces.SelectedItem.ToString().Replace("<root>", ""));
            }
            Types.SelectedItem = null;
        }

        private void Types_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(Types.SelectedItem is LookupType)) return;
            LookupType type = (LookupType)Types.SelectedItem;
            type.Resolve();

            TypeInfo.DataContext = type;
            Fields.ItemsSource = type.Fields;
            Fields.SelectedIndex = 0;
            Methods.ItemsSource = type.Methods.Where(m => !m.IsPropertymethod);
            Methods.SelectedIndex = 0;
            Properties.ItemsSource = type.Properties;
            Properties.SelectedIndex = 0;
        }

        private void Fields_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(Fields.SelectedItem is LookupField)) return;

            LookupType type = (LookupType)TypeInfo.DataContext;
            type.Resolve();

            LookupField field = (LookupField)Fields.SelectedItem;
            FieldInfo.DataContext = field;
        }

        private void Methods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(Methods.SelectedItem is LookupMethod)) return;

            LookupType type = (LookupType)TypeInfo.DataContext;
            type.Resolve();

            LookupMethod method = (LookupMethod)Methods.SelectedItem;
            MethodInfo.DataContext = method;

            MethodAddress.Content = string.Format("0x{0:X}", method.GetAddress(game.Module.AppModel));
            IsCalled.Content = game.CalledMethods == null ? "Not analysed" : (game.CalledMethods.ContainsKey(method) ? "True" : "False");
        }

        private void Properties_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(Properties.SelectedItem is LookupProperty)) return;

            LookupType type = (LookupType)TypeInfo.DataContext;
            type.Resolve();

            LookupProperty property = (LookupProperty)Properties.SelectedItem;
            PropertyInfo.DataContext = property;
        }
        private void Dissasemble_Click(object sender, RoutedEventArgs e)
        {
            if (!(Methods.SelectedItem is LookupMethod)) return;

            LookupMethod method = (LookupMethod)Methods.SelectedItem;
            new Dissasembly(Dissasembler.DissasembleMethod(method, game.Module)).Show();
        }
        private void DissasembleGet_Click(object sender, RoutedEventArgs e)
        {
            if (!(Properties.SelectedItem is LookupProperty)) return;

            LookupProperty property = (LookupProperty)Properties.SelectedItem;
            new Dissasembly(Dissasembler.DissasembleMethod(property.GetMethod, game.Module)).Show();
        }

        private void DissasembleSet_Click(object sender, RoutedEventArgs e)
        {
            if (!(Properties.SelectedItem is LookupProperty)) return;

            LookupProperty property = (LookupProperty)Properties.SelectedItem;
            new Dissasembly(Dissasembler.DissasembleMethod(property.SetMethod, game.Module)).Show();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Namespaces.ItemsSource = null;
            Types.ItemsSource = null;
            GameInfo.DataContext = null;
            TypeInfo.DataContext = null;
            Fields.ItemsSource = null;
            Methods.ItemsSource = null;
            Properties.ItemsSource = null;
            FieldInfo.DataContext = null;
            MethodInfo.DataContext = null;
            PropertyInfo.DataContext = null;

            game.Dispose();
            game = null;
            Dimmer.Visibility = Visibility.Visible;
            SelectGame.Visibility = Visibility.Visible;
        }

        private void AnalyzeMethods_Click(object sender, RoutedEventArgs e)
        {
            if (game == null)
            {
                return;
            }
            if (game.CalledMethods != null)
            {
                new MethodStatistics(game).Show();
                return;
            }
            AnalyzeMethods.IsEnabled = false;
            Dimmer.Visibility = Visibility.Visible;
            StatusTextContainer.Visibility = Visibility.Visible;
            Thread thread = new Thread(() => game.AnalyseMethodStructure(AnalyseMethodEndCallback, StatusUpdate));
            thread.Start();

        }
        public void AnalyseMethodEndCallback(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                Dimmer.Visibility = Visibility.Hidden;
                StatusTextContainer.Visibility = Visibility.Hidden;
                AnalyzeMethods.Content = "Method stats";
                AnalyzeMethods.IsEnabled = true;

                LookupMethod method = (LookupMethod)Methods.SelectedItem;
                if (method != null)
                {
                    IsCalled.Content = game.CalledMethods == null ? "Not analysed" : (game.CalledMethods.ContainsKey(method) ? "True" : "False");
                }
            }));
        }
    }
}
