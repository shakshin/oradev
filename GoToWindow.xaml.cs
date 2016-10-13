using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    /// Interaction logic for GoToWindow.xaml
    /// </summary>
    public partial class GoToWindow : Window
    {
        public GoToWindow()
        {
            InitializeComponent();
            line.Focus();
        }

        private void ButtonGo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int _line = int.Parse(line.Text);
                (((this.Owner as MainWindow)._tabs.SelectedItem as CustomTab).Content as SQLEdit).GoToLine(_line);
                this.Close();
            }
            catch (Exception )
            { }
        }

        private void line_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex r = new Regex("[^0-9]");
            e.Handled = r.IsMatch(e.Text);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) 
            {
                this.Close();
            }
        }
    }
}
