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
    /// Логика взаимодействия для ValueView.xaml
    /// </summary>
    public partial class ValueView : Window
    {
        public ValueView(string value)
        {
            InitializeComponent();
            txtView.Text = value;

        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            try
            {
                this.Close();
                this.Owner.Activate();
            }
            catch (Exception) { }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
                this.Owner.Activate();
            }
        }
    }
}
