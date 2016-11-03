using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace oradev
{
    /// <summary>
    /// Логика взаимодействия для AllSessions.xaml
    /// </summary>
    public partial class AllSessions : Window
    {
        


        public AllSessions()
        {
            InitializeComponent();
            dbs.ItemsSource = (App.Current as App).Configuration.Databases;
            if (dbs.Items.Count > 0) dbs.SelectedIndex = 0;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (dbs.Items.Count == 0) return;
            srch.Visibility = Visibility.Hidden;
            tree.Items.Clear();
            this.Cursor = Cursors.Wait;
            Oracle.QueryAsync(
                @"select 
                    l.name, s.username, s.sid, s.serial#, l.type from dba_ddl_locks l, v$session s 
                  where 
                    s.sid = l.session_id
                  order by l.name, s.sid, s.serial#
                 ", delegate (DataTable result, long elapsed) {
                    tree.Items.Clear();
                    String lastResource = String.Empty;
                    TreeViewItem lastRoot = null;
                    foreach (DataRow row in result.Rows) {
                        if (lastResource != row[0].ToString())
                        {
                            TreeViewItem item = new TreeViewItem();
                            item.Header = row[0].ToString();
                            tree.Items.Add(item);
                            lastRoot = item;
                            lastResource = row[0].ToString();
                        }
                        TreeViewItem child = new TreeViewItem();
                        child.Header = String.Format("{0} ({1})", row[1].ToString(), row[4].ToString());
                        lastRoot.Items.Add(child);
                    }
                    if (tree.Items.Count > 0)
                    {
                        srch.Visibility = Visibility.Visible;
                        ApplySearch();
                    }
                    this.Cursor = Cursors.Arrow;

                }, (DataBaseConfig)dbs.SelectedItem);
        }

        private void ApplySearch()
        {
            String qry = srch.Text.Trim();
            Regex r = new Regex(@"^" + qry, RegexOptions.IgnoreCase);
            foreach (TreeViewItem item in tree.Items)
            {
                if (qry == String.Empty)
                {
                    item.Visibility = Visibility.Visible;
                }
                else
                {
                    String txt = item.Header.ToString();
                    if (r.IsMatch(txt))
                    {
                        item.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        item.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void srch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearch();
        }
    }
}
