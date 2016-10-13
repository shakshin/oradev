using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace oradev
{
    /// <summary>
    /// Interaction logic for MacroManager.xaml
    /// </summary>
    public partial class MacroManager : Window
    {
        public MacroManager()
        {
            InitializeComponent();
            grid.ItemsSource = (App.Current as App).Configuration.Macros;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            MacroVew wnd = new MacroVew(null);
            wnd.Owner = this;
            wnd.ShowDialog();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (grid.SelectedItem == null) return;
            if (MessageBox.Show("Would you like to delete this macro?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                (App.Current as App).Configuration.Macros.Remove(grid.SelectedItem as TextMacro);
                (App.Current as App).Configuration.SaveToFile();
            }
        }

        private void grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (grid.SelectedItem == null) return;
            MacroVew wnd = new MacroVew(grid.SelectedItem as TextMacro);
            wnd.Owner = this;
            wnd.ShowDialog();
            
            grid.ItemsSource = null;
            grid.ItemsSource = (App.Current as App).Configuration.Macros;
        }

        
    }
}
