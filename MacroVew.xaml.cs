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
    /// Interaction logic for MacroVew.xaml
    /// </summary>
    public partial class MacroVew : Window
    {
        private TextMacro Macro = null;
        public MacroVew(TextMacro macro)
        {
            InitializeComponent();
            if (macro != null)
            {
                Macro = macro;
                txtText.Text = Macro.Text;
                cbControl.IsChecked = Macro.Control;
                cbShift.IsChecked = Macro.Shift;
                txtKey.Text = Macro.Key;

                cbEOL.IsChecked = Macro.AtEOL;
            }
        }

        private void Key_KeyUp(object sender, KeyEventArgs e)
        {
            txtKey.Text = e.Key.ToString();
            
            e.Handled = true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (Macro == null)
            {
                Macro = new TextMacro();
                Macro.Text = txtText.Text;
                Macro.Key = txtKey.Text;
                Macro.Control = cbControl.IsChecked.HasValue && cbControl.IsChecked.Value;
                Macro.Shift = cbShift.IsChecked.HasValue && cbShift.IsChecked.Value;
                Macro.AtEOL = cbEOL.IsChecked.HasValue && cbEOL.IsChecked.Value;
                (App.Current as App).Configuration.Macros.Add(Macro);
                (App.Current as App).Configuration.SaveToFile();
            }
            else
            {
                Macro.Text = txtText.Text;
                Macro.Key = txtKey.Text;
                Macro.Control = cbControl.IsChecked.HasValue && cbControl.IsChecked.Value;
                Macro.Shift = cbShift.IsChecked.HasValue && cbShift.IsChecked.Value;
                Macro.AtEOL = cbEOL.IsChecked.HasValue && cbEOL.IsChecked.Value;
                (App.Current as App).Configuration.SaveToFile();
            }
            DialogResult = true;
        }
    }
}
