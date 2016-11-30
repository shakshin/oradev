using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace oradev
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {


        public Config cfg
        {
            get { return (Config)GetValue(cfgProperty); }
            set { SetValue(cfgProperty, value); }
        }

        // Using a DependencyProperty as the backing store for cfg.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty cfgProperty =
            DependencyProperty.Register("cfg", typeof(Config), typeof(SettingsWindow), new PropertyMetadata(new Config()));

        
        public SettingsWindow()
        {
            InitializeComponent();
            cfg = (App.Current as App).Configuration;
            dblist.ItemsSource = cfg.Databases;
            if (dblist.Items.Count > 0)
            {
                dblist.SelectedItem = dblist.Items[0];
            }
            ObservableCollection<EncodingInfo> encs = new ObservableCollection<EncodingInfo>();
            EncodingInfo currenc = null;
            foreach (EncodingInfo enc in Encoding.GetEncodings().OrderBy(e => e.DisplayName)) {
                if (cfg.FileEncoding == enc.Name) currenc = enc;
                encs.Add(enc);
                
            }
            cmbEnc.ItemsSource = encs;
            if (currenc != null) cmbEnc.SelectedItem = currenc;
            cmbEnc.SelectionChanged += delegate
            {
                if (cmbEnc.SelectedItem != null)
                {
                    cfg.FileEncoding = (cmbEnc.SelectedItem as EncodingInfo).Name;
                }
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            cfg.SaveToFile();
            this.Close();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            DataBaseConfig item = new DataBaseConfig();
            item.Guid = Guid.NewGuid().ToString();
            cfg.Databases.Add(item);
            dblist.SelectedItem = item;
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (dblist.SelectedItem != null)
            {
                cfg.Databases.Remove(dblist.SelectedItem as DataBaseConfig);
            }
        }
    }
}
