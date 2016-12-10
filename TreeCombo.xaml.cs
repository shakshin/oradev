using oradev.Parser;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace oradev
{
    /// <summary>
    /// Логика взаимодействия для TreeCombo.xaml
    /// </summary>
    public partial class TreeCombo : UserControl
    {
        public event EventHandler SelectedNode;
        private bool active = false;

        public TreeCombo()
        {
            InitializeComponent();
        }

        private void btnMain_Click(object sender, RoutedEventArgs e)
        {
            selector.IsOpen = !selector.IsOpen;
            active = selector.IsOpen;
        }

        private void UserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            selector.IsOpen = false;
            //active = false;
        }

        public void Refresh(StructureElement s)
        {
            tree.Items.Clear();
            AddElement(s, null);
        }

        private void AddElement(StructureElement e, TreeViewItem parent)
        {
            TreeViewItem i = new TreeViewItem();
            i.Header = e.Display;
            i.Tag = e;
            i.IsExpanded = true;
            if (parent == null)
            {
                tree.Items.Add(i);
            }
            else
            {
                parent.Items.Add(i);
            }
            foreach (StructureElement el in e.Children)
                AddElement(el, i);
        }

        public void SetCurrent(StructureElement e)
        {
            btnMain.Content = e.Identifier;
        }

        public StructureElement GetSelected()
        {
            return ((tree.SelectedItem as TreeViewItem).Tag as StructureElement);
        }

        private void tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (active)
            {
                SelectedNode(this, new EventArgs());
                active = false;
            }
        }
    }
}
