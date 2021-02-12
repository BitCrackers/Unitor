using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Unitor.Core;
using Unitor.Core.Reflection;

namespace Unitor
{
    /// <summary>
    /// Interaction logic for MethodStatistics.xaml
    /// </summary>
    public partial class MethodStatistics : Window
    {
        public MethodStatistics(Game game)
        {
            InitializeComponent();
            List<KeyValuePair<UnitorMethod, int>> ranked = game.CalledMethods.ToList();

            ranked.Sort(delegate (KeyValuePair<UnitorMethod, int> v1,
                KeyValuePair<UnitorMethod, int> v2)
            {
                return v1.Value.CompareTo(v2.Value);
            }
            );
            ranked.Reverse();

            MethodCallRanking.ItemsSource = ranked;
            List<UnitorMethod> methods = game.Model.Types.AsParallel().SelectMany(t => t.Methods).ToList();
            TotalMethods.Content = methods.Count;
            UncalledMethods.Content = methods.Count - ranked.Count;
            TotalCalls.Content = ranked.Sum(e => e.Value);
        }

        private void MethodCallRanking_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
