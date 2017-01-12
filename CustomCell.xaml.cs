using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace oradev
{
    /// <summary>
    /// Логика взаимодействия для CustomCell.xaml
    /// </summary>
    public partial class CustomCell : UserControl
    {

        public String Text
        {
            get { return (String)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(String), typeof(CustomCell), new PropertyMetadata(String.Empty, delegate (DependencyObject d, DependencyPropertyChangedEventArgs e) {
                (d as CustomCell).Foreground = Brushes.Black;
                if (e.NewValue as string == "{{{null}}}")
                {
                    (d as CustomCell).txtContent.Text = "";
                    (d as CustomCell).btnDetails.Visibility = Visibility.Collapsed;
                    (d as CustomCell).Background = Brushes.Wheat;
                    (d as CustomCell).ToolTip = "Null value";
                }
                else
                {
                    (d as CustomCell).txtContent.Text = Regex.Replace(Regex.Replace(e.NewValue as string, @"[^\u0000-\u007F]+", ""), @"[\r\n]", "");

                    if ((d as CustomCell).txtContent.Text != e.NewValue as string)
                    {
                        (d as CustomCell).txtContent.Text = "{complex value}";
                    }
                    (d as CustomCell).ValidateButton(e.NewValue as string);
                    (d as CustomCell).Background = Brushes.White;
                }
            }));



        public CustomCell()
        {
            InitializeComponent();
        }

        public void ValidateButton(string newVal)
        {
            if (newVal.Length < 50 && Text == txtContent.Text)
            {
                btnDetails.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnDetails.Visibility = Visibility.Visible;
            }
        }

        

        private void btnDetails_Click(object sender, RoutedEventArgs e)
        {
            ValueView w = new ValueView(Text);
            w.Owner = (App.Current as App).MainWindow;
            w.Show();
        }

        private void btnDetails_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ValueView w = new ValueView(Text);
            w.Owner = (App.Current as App).MainWindow;
            w.Show();
        }
    }
}
