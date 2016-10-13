using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// Interaction logic for CustomTabHeader.xaml
    /// </summary>
    public partial class CustomTabHeader : UserControl
    {

        public event EventHandler CloseButtonPressed;
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(CustomTabHeader), new PropertyMetadata(string.Empty));




        public string ModifiedMarker
        {
            get { return (string)GetValue(ModifiedMarkerProperty); }
            set { SetValue(ModifiedMarkerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ModifiedMarker.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModifiedMarkerProperty =
            DependencyProperty.Register("ModifiedMarker", typeof(string), typeof(CustomTabHeader), new PropertyMetadata(string.Empty));



        public Visibility PendingMarker
        {
            get { return (Visibility)GetValue(PendingMarkerProperty); }
            set { SetValue(PendingMarkerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PendingMarker.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PendingMarkerProperty =
            DependencyProperty.Register("PendingMarker", typeof(Visibility), typeof(CustomTabHeader), new PropertyMetadata(Visibility.Collapsed));




        public Visibility CompleteMarker
        {
            get { return (Visibility)GetValue(CompleteMarkerProperty); }
            set { SetValue(CompleteMarkerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CompleteMarker.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CompleteMarkerProperty =
            DependencyProperty.Register("CompleteMarker", typeof(Visibility), typeof(CustomTabHeader), new PropertyMetadata(Visibility.Collapsed));




        public Visibility ErrorsMarker
        {
            get { return (Visibility)GetValue(ErrorsMarkerProperty); }
            set { SetValue(ErrorsMarkerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ErrorsMarker.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ErrorsMarkerProperty =
            DependencyProperty.Register("ErrorsMarker", typeof(Visibility), typeof(CustomTabHeader), new PropertyMetadata(Visibility.Collapsed));



        public CustomTabHeader()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (CloseButtonPressed != null)
            {
                CloseButtonPressed(this, null);
            }
        }

        private void This_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle) CloseButtonPressed(this, null);
        }
    }
}
