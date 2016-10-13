using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Timers;
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
    /// Interaction logic for Lockers.xaml
    /// </summary>
    public partial class Lockers : UserControl
    {
        public delegate void Callback();

        private Callback OnFinal;
        private DataBaseConfig _config;
        public Timer timer;

        public Lockers()
        {
            InitializeComponent();
        }

        public void Show(Callback SuccessHandler, Callback FinalHandler, DataBaseConfig config, String package, ObservableCollection<DBSession> lockers)
        {
            OnFinal = FinalHandler;
            _config = config;
            Visibility = Visibility.Visible;

            lstSessions.ItemsSource = lockers;

            
            
            timer = new Timer(2000);
            timer.Elapsed += delegate
            {
                Oracle.QueryAsync(string.Format(
                    @"select distinct l.session_id, s.username, s.serial#
                        from dba_ddl_locks l, v$session s 
                        where 
                            s.sid = l.session_id 
                            and l.name = '{0}'",
                    package.ToUpper()
                ), delegate(DataTable result, long elapsed)
                {
                    #region delegate body
                    DBSession _selected = lstSessions.SelectedItem as DBSession;
                    DBSession _newselected = null;
                    ObservableCollection<DBSession> _items = new ObservableCollection<DBSession>();
                    if (result.Rows.Count == 0)
                    {
                        
                        
                        App.Current.Dispatcher.Invoke((Action)delegate
                        {
                            timer.Stop();
                            this.Visibility = Visibility.Collapsed;
                            SuccessHandler();
                            OnFinal();
                        });
                        return;
                    }
                    foreach (DataRow row in result.Rows)
                    {
                        DBSession newitem = new DBSession() { Id = row[0].ToString(), User = row[1].ToString(), Serial = row[2].ToString() };
                        _items.Add(newitem);
                        if (_selected != null && newitem.Id == _selected.Id && newitem.Serial == _selected.Serial && newitem.User == _selected.User) _newselected = newitem;
                    }
                    lstSessions.ItemsSource = _items;
                    if (_newselected != null) lstSessions.SelectedItem = _newselected;
                    #endregion
                }, config);
            };


            timer.Start();

            
        }

        private void ButtonKill_Click(object sender, RoutedEventArgs e)
        {
            if (lstSessions.SelectedItem != null) Oracle.KillSession((lstSessions.SelectedItem as DBSession).Id, (lstSessions.SelectedItem as DBSession).Serial, _config);
        }

        private void ButtonKillAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (DBSession sess in lstSessions.Items)
            {
                Oracle.KillSession(sess.Id, sess.Serial, _config);
            }
        }
        public void Cancel() 
        {
            Visibility = Visibility.Collapsed;
            Console.Log("Compilation aborted");
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                timer.Stop();
                OnFinal();
            });
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Cancel();
        }

    }
}
