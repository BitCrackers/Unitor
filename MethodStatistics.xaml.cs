using Beebyte_Deobfuscator.Lookup;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Unitor.Core;

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
            List<KeyValuePair<LookupMethod, int>> ranked = game.CalledMethods.ToList();

            ranked.Sort(delegate (KeyValuePair<LookupMethod, int> v1,
                KeyValuePair<LookupMethod, int> v2)
            {
                return v1.Value.CompareTo(v2.Value);
            }
            );
            ranked.Reverse();

            MethodCallRanking.ItemsSource = ranked;
            List<LookupMethod> methods = game.Module.Types.AsParallel().SelectMany(t => t.Methods).ToList();
            TotalMethods.Content = methods.Count;
            UncalledMethods.Content = methods.Count - ranked.Count;
            TotalCalls.Content = ranked.Sum(e => e.Value);
        }

        private void MethodCallRanking_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
