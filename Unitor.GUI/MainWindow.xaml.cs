﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Unitor.Core;
using Unitor.Core.Deobfuscation;
using Unitor.Core.Reflection;

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
            if (files.Length > 1)
            {
                MessageBox.Show("You can only drag one folder at a time");
            }

            if (!Directory.Exists(files[0]))
            {
                MessageBox.Show("You must drag and drop the whole game folder");
            }
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
                StatusTextContainer.Visibility = Visibility.Hidden;
                SelectGame.Visibility = Visibility.Visible;
            }
        }

        public void SetGame(Game g)
        {
            game = g;
            GameInfo.DataContext = game;
            Namespaces.ItemsSource = game.Model.Namespaces;
            Namespaces.SelectedIndex = 0;
            Types.ItemsSource = game.Model.Types.Where(t => t.Namespace == "");
            Types.SelectedIndex = 0;
            Dimmer.Visibility = Visibility.Hidden;
            StatusTextContainer.Visibility = Visibility.Hidden;
        }

        public void CreateGame(string dir)
        {
            try
            {
                Game g = new Game(dir, StatusUpdate);
                Application.Current.Dispatcher.Invoke(new Action(() => SetGame(g)));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured: {ex.Message}");
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    StatusTextContainer.Visibility = Visibility.Hidden;
                    SelectGame.Visibility = Visibility.Visible;
                }));
            }
        }


        public void StatusUpdate(object sender, string status)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => StatusText.Text = status + "..."));
        }

        private void Namespaces_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (string.IsNullOrEmpty((sender as ListBox).SelectedItem as string))
            {
                return;
            }
            Types.ItemsSource = game.Model.Types.Where(t => t.Namespace == (sender as ListBox).SelectedItem.ToString().Replace("<root>", ""));
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
                Namespaces.ItemsSource = game.Model.Namespaces.Where(n => n.ToLower().Contains(NamespaceSearch.Text.ToLower()));
            }
            else
            {
                Namespaces.ItemsSource = game.Model.Namespaces;
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
                Types.ItemsSource = game.Model.Types
                    .Where(t => t.Namespace == Namespaces.SelectedItem.ToString().Replace("<root>", ""))
                    .Where(n => n.Name.ToLower().Contains(TypeSearch.Text.ToLower()));
            }
            else
            {
                Types.ItemsSource = game.Model.Types.Where(t => t.Namespace == Namespaces.SelectedItem.ToString().Replace("<root>", ""));
            }
            Types.SelectedItem = null;
        }

        private void Types_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Types.SelectedItem is UnitorType type)
            {
                type.Resolve();

                TypeInfo.DataContext = type;
                TypeAddress.Content = string.Format("0x{0:X}", type.TypeClassAddress);

                Fields.ItemsSource = type.Fields;
                Fields.SelectedIndex = 0;
                Methods.ItemsSource = type.Methods.Where(m => !m.IsPropertymethod && (!(IsCalledOnly.IsChecked ?? false) || game.Model.CalledMethods.ContainsKey(m)));
                Methods.SelectedIndex = 0;
                Properties.ItemsSource = type.Properties;
                Properties.SelectedIndex = 0;
            }
        }

        private void Fields_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Fields.SelectedItem is UnitorField field)
            {
                FieldInfo.DataContext = field;
                FieldOffset.Content = string.Format("0x{0:X}", field.Offset);
            }
        }

        public void SetSelectedMethod(object sender, UnitorMethod method)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                TypeSearch.Text = "";
                NamespaceSearch.Text = "";
                Namespaces.SelectedItem = Namespaces.ItemsSource.Cast<string>().ToList().IndexOf(method.DeclaringType.Namespace);
                Types.SelectedIndex = Types.ItemsSource.Cast<UnitorType>().ToList().IndexOf(method.DeclaringType);
                if (!game.Model.CalledMethods.ContainsKey(method) && (IsCalledOnly.IsChecked ?? false))
                {
                    IsCalledOnly.IsChecked = false;
                }
                Methods.SelectedItem = Methods.ItemsSource.Cast<UnitorMethod>().ToList().IndexOf(method);
            }));
        }

        private void Methods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Methods.SelectedItem is UnitorMethod method)
            {
                MethodInfo.DataContext = method;

                MethodAddress.Content = string.Format("0x{0:X}", method.Address);
                IsCalled.Content = game.Model.CalledMethods.ContainsKey(method) || Helpers.IsUnityMonobehaviourMessage(method) ? "True" : "False";
            }
        }

        private void Properties_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Properties.SelectedItem is UnitorProperty property)
            {
                PropertyInfo.DataContext = property;
            }
        }
        private void Dissasemble_Click(object sender, RoutedEventArgs e)
        {
            if (Methods.SelectedItem is UnitorMethod method)
            {
                new Dissasembly(Dissasembler.DissasembleMethod(method, game.Model)).Show();
            }
        }

        private void DissasembleGet_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.SelectedItem is UnitorProperty property)
            {
                new Dissasembly(Dissasembler.DissasembleMethod(property.GetMethod, game.Model)).Show();
            }
        }

        private void DissasembleSet_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.SelectedItem is UnitorProperty property)
            {
                new Dissasembly(Dissasembler.DissasembleMethod(property.SetMethod, game.Model)).Show();
            }
        }

        public void EndDeobfuscationCallback(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                Dimmer.Visibility = Visibility.Hidden;
                StatusTextContainer.Visibility = Visibility.Hidden;
                NamespaceSearch.Text = "";
                TypeSearch.Text = "";
                Namespaces.ItemsSource = game.Model.Namespaces;
                Namespaces.SelectedIndex = 0;
                Types.ItemsSource = game.Model.Types.Where(t => t.Namespace == "");
                Types.SelectedIndex = 0;
            }));
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
            new MethodStatistics(game).Show();
        }

        private void ViewStrings_Click(object sender, RoutedEventArgs e)
        {
            new StringTable(game.Model, SetSelectedMethod).Show();
        }

        private void References_Click(object sender, RoutedEventArgs e)
        {
            if (Methods.SelectedItem is UnitorMethod method)
            {
                new ReferenceView(method, SetSelectedMethod).Show();
            }
        }

        private void IsCalledOnly_Changed(object sender, RoutedEventArgs e)
        {
            if (Types.SelectedItem is UnitorType type)
            {
                Methods.ItemsSource = type.Methods.Where(m => (!m.IsPropertymethod && (!(IsCalledOnly.IsChecked ?? false) || game.Model.CalledMethods.ContainsKey(m))));
                Methods.SelectedIndex = 0;
            }
        }

        private void HexView_Click(object sender, RoutedEventArgs e)
        {
            if (Methods.SelectedItem is UnitorMethod method)
            {
                new HexViewWindow(method.Il2CppMethod.GetMethodBody(), method.Address).Show();
            }
        }

        private void ViewAssets_Click(object sender, RoutedEventArgs e)
        {
            new AssetViewer(game.AssetModel).Show();
        }

        private void Deobfuscate_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            //fileDialog.InitialDirectory = game.GameDir;
            fileDialog.Title = "Open Json Deobfuscation file";
            //fileDialog.Filter = "json files(*.json)| *.json | All files(*.*) | *.* ";
            //fileDialog.CheckFileExists = true;
            //fileDialog.CheckPathExists = true;
            fileDialog.ShowDialog();
            if(string.IsNullOrEmpty(fileDialog.FileName))
            {
                return;
            }
            Dimmer.Visibility = Visibility.Visible;
            StatusTextContainer.Visibility = Visibility.Visible;
            StatusUpdate(this, "Deobfuscating game");
            GameDeobfuscator.Deobfuscate(game, fileDialog.FileName, EndDeobfuscationCallback);
        }
    }
}
