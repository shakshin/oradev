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
using System.Data.OracleClient;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Timers;
using System.Windows.Shell;
using System.Collections.Specialized;
using System.Windows.Media.Effects;

namespace oradev
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<CustomTab> tabs = new ObservableCollection<CustomTab>();
        private Timer _timer;
        private int objSearchTimer = -1;

        public List<DBObject> objects = new List<DBObject>();



        public string MemUsage
        {
            get { return (string)GetValue(MemUsageProperty); }
            set { SetValue(MemUsageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MemUsage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MemUsageProperty =
            DependencyProperty.Register("MemUsage", typeof(string), typeof(MainWindow), new PropertyMetadata("0"));



        

               
        public bool CloseTab(CustomTab tab)
        {
            if ((tab.Content as SQLEdit).txtCode.IsModified)
            {
                switch (MessageBox.Show("File " + (tab.Header as CustomTabHeader).Title + " was modified. Would you like to save it?", "Warning", MessageBoxButton.YesNoCancel))
                {
                    case MessageBoxResult.Yes:
                        if (string.IsNullOrEmpty(tab.SaveFile))
                        {
                            SaveFileDialog dlg = new SaveFileDialog();
                            dlg.DefaultExt = ".sql";
                            dlg.Filter = "SQL files|*.sql|Table definitions|*.tab|All files|*.*";
                            dlg.CheckPathExists = true;
                            dlg.OverwritePrompt = true;
                            if (dlg.ShowDialog() == true)
                            {
                                tab.SaveFile = dlg.FileName;
                                tab.Save();
                            }
                            else
                            {
                                return false;
                            }
                        }
                        tabs.Remove(tab);
                        IDESession.Save();
                        return true;
                    case MessageBoxResult.No:
                        tabs.Remove(tab);
                        IDESession.Save();
                        return true;
                    case MessageBoxResult.Cancel:
                        return false;
                }
            }
            else
            {
                tabs.Remove(tab);
                return true;
            }
            
            return true;
        }

        public bool CloseAll()
        {
            for (int i = tabs.Count - 1; i >= 0; i--)
            {
                if (!CloseTab(tabs[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public CustomTab NewTab(Boolean focus, DataBaseConfig db, bool first = false)
        {
            CustomTab newTab = new CustomTab(db);
            
            newTab.CloseButtonPressed += delegate {
                CloseTab(newTab);
            };

            if (first)
            {
                tabs.Insert(0, newTab);
            }
            else
            {
                tabs.Add(newTab);
            }
            if (focus)
            {
                _tabs.SelectedItem = newTab;
                (newTab.Content as SQLEdit).txtCode.Focus();
            }
            
            return newTab;
        }

        public void ObjectsSearch()
        {
            ObservableCollection<DBObject> pk = null;
            ObservableCollection<DBObject> tb = null;
            Action finish = delegate () {
                objtree.Effect = null;
                objtree.IsEnabled = true;
            };

            if ((App.Current as App).Configuration.Databases.Count > 0)
            {
                objtree.Effect = new BlurEffect();
                objtree.IsEnabled = false;
                Oracle.GetPackagesAsync(objsearch.Text.Trim().ToUpper(), delegate (ObservableCollection<DBObject> result) {
                    lstPackages.ItemsSource = result;
                    pk = result;
                    if (tb != null) finish();
                }, dbselect.SelectedItem as DataBaseConfig);

                Oracle.GetTablesAsync(objsearch.Text.Trim().ToUpper(), delegate (ObservableCollection<DBObject> result) {
                    lstTables.ItemsSource = result;
                    tb = result;
                    if (tb != null) finish();
                }, dbselect.SelectedItem as DataBaseConfig);
            }
        }

        public void LoadFile(String fileName)
        {
            foreach (CustomTab tab in _tabs.Items)
            {
                if (tab.SaveFile == fileName)
                {
                    _tabs.SelectedItem = tab;
                    return;
                }
            }
            Console.Log("Loading file: " + fileName);
            this.Cursor = Cursors.Wait;
            try
            {
                CustomTab newTab = NewTab(true, dbselect.SelectedItem as DataBaseConfig);
                newTab.LoadFile(fileName);
                _tabs.SelectedItem = newTab;                
            }
            catch (Exception ex)
            {
                Console.Log("Exception while loading file: " + ex.Message);
            }
            this.Cursor = Cursors.Arrow;
        }

        public MainWindow()
        {
            InitializeComponent();
            Console.Log("C+ Oracle Developer startup");
            Console.Log("Original code by Sergey Shakshin");
            this.WindowState = WindowState.Maximized;

            dbselect.ItemsSource = (App.Current as App).Configuration.Databases;

            if (dbselect.Items.Count > 0)
            {
                dbselect.SelectedItem = dbselect.Items[0];
                ObjectsSearch();
            }

            lstConsole.ItemsSource = Console.Messages;
            _tabs.ItemsSource = tabs;

            tabs.CollectionChanged += delegate(Object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (CustomTab tab in e.OldItems)
                    {
                        (tab.Content as SQLEdit).StopMonitor();
                        (tab.Content as SQLEdit).StopThread();
                        if ((tab.Content as SQLEdit).ctlLockers.timer != null) (tab.Content as SQLEdit).ctlLockers.timer.Stop();
                    }
                }
            };

            Console.Messages.CollectionChanged += Messages_CollectionChanged;
            ObjectsSearch();

            IDESession.Restore();

            if ((App.Current as App).Args.Length > 0)
            {
                foreach (string arg in (App.Current as App).Args)
                {
                    LoadFile(arg);
                }
            }
            else if (tabs.Count == 0)
            {
                CustomTab newTab = NewTab(true, dbselect.SelectedItem as DataBaseConfig);
            }

            App.NewFiles += delegate(Object Sender, EventArgs args) {
                foreach (string file in (args as NewFilesEventArgs).Files) 
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        LoadFile(file);
                        this.Activate();
                    });
                }
            };

            _timer = new Timer(1000);
            _timer.Elapsed += delegate
            {
                try
                {
                    long _mem = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
                    double _mem2 = _mem / 1024 / 1024;
                    Math.Round(_mem2, 2);
                    App.Current.Dispatcher.Invoke((Action)delegate { MemUsage = string.Format("Memory usage: {0} MB", _mem2); });
                    if (this.objSearchTimer > 0)
                    {
                        this.objSearchTimer--;
                    }
                    else if (this.objSearchTimer == 0)
                    {
                        this.objSearchTimer = -1;
                        App.Current.Dispatcher.Invoke((Action)delegate { ObjectsSearch(); });
                    }
                }
                catch (Exception )
                {

                }
            };
            _timer.Start();

            foreach (EncodingInfo enc in Encoding.GetEncodings().OrderBy(e => e.DisplayName))
            {
                MenuItem item = new MenuItem();
                item.Header = enc.DisplayName;
                item.Click += delegate
                {
                    this.Cursor = Cursors.Wait;
                    if (!String.IsNullOrEmpty((_tabs.SelectedItem as CustomTab).SaveFile))
                    {
                        (_tabs.SelectedItem as CustomTab).LoadFile((_tabs.SelectedItem as CustomTab).SaveFile, enc.GetEncoding());
                    }
                    IDESession.Save();
                    this.Cursor = Cursors.Arrow;
                };
                mnuReopenEnc.Items.Add(item);

                MenuItem item2 = new MenuItem();
                item2.Header = enc.DisplayName;
                item2.Click += delegate
                {
                    (_tabs.SelectedItem as CustomTab).FileEncoding = enc.GetEncoding();
                    IDESession.Save();
                };
                mnuSetEnc.Items.Add(item2);
            }
        }

        void Messages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                lstConsole.ScrollIntoView(e.NewItems[0]);
            }
        }

       

        

        private void objsearch_KeyUp(object sender, KeyEventArgs e)
        {
            objSearchTimer = 1;
        }

        /*private void lstObjects_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstObjects.SelectedItem == null)
            {
                return;
            }
            this.Cursor = Cursors.Wait;
            if ((lstObjects.SelectedItem as DBObject).Type == "PACKAGE")
            {
                CustomTab newTab = NewTab(true, dbselect.SelectedItem as DataBaseConfig);
                (newTab.Content as SQLEdit).LoadPackageHead((lstObjects.SelectedItem as DBObject).Name);

                newTab = NewTab(false, dbselect.SelectedItem as DataBaseConfig);
                (newTab.Content as SQLEdit).LoadPackage((lstObjects.SelectedItem as DBObject).Name);
                IDESession.Save();
            }
            else if ((lstObjects.SelectedItem as DBObject).Type == "TABLE")
            {
                CustomTab newTab = NewTab(true, dbselect.SelectedItem as DataBaseConfig);
                (newTab.Content as SQLEdit).LoadTable((lstObjects.SelectedItem as DBObject).Name);
                IDESession.Save();
            }

            this.Cursor = Cursors.Arrow;
        } */

        public void CompileCurrent()
        {
            if (_tabs.SelectedItem == null) return;
            this.Cursor = Cursors.Wait;
            if ((App.Current as App).Configuration.SaveOnCompile)
                if (!String.IsNullOrEmpty((_tabs.SelectedItem as CustomTab).SaveFile)) (_tabs.SelectedItem as CustomTab).Save();

            ((_tabs.SelectedItem as CustomTab).Content as SQLEdit).Compile();
            this.Cursor = Cursors.Arrow;
        }
        
        private void MenuCompile_Click(object sender, RoutedEventArgs e)
        {
            CompileCurrent();
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow wnd = new SettingsWindow();
            wnd.Owner = this;
            wnd.ShowDialog();
        }

        private void tags_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (tags.SelectedItem != null && _tabs.SelectedItem != null)
            {
                ((_tabs.SelectedItem as CustomTab).Content as SQLEdit).GoToLine((tags.SelectedItem as SourceCodeTag).Line);
            }
        }

        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            NewTab(true, dbselect.SelectedItem as DataBaseConfig);
            IDESession.Save();
        }

        

        public void OpenFileDialog()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "SQL files|*.sql|Table definitions|*.tab|All files|*.*";
            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            dlg.Multiselect = false;
            if (dlg.ShowDialog() == true)
            {
                LoadFile(dlg.FileName);
                IDESession.Save();
            }
        }
        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog();
        }

        

        public void SaveCurrent(Boolean newName = false)
        {
            this.Cursor = Cursors.Wait;
            if (newName || String.IsNullOrEmpty((_tabs.SelectedItem as CustomTab).SaveFile))
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.DefaultExt = ".sql";
                dlg.Filter = "SQL files|*.sql|Table definitions|*.tab|All files|*.*";
                dlg.CheckPathExists = true;
                dlg.OverwritePrompt = true;
                if (dlg.ShowDialog() == true)
                {
                    (_tabs.SelectedItem as CustomTab).SaveFile = dlg.FileName;
                }
                else
                {
                    this.Cursor = Cursors.Arrow;
                    return;
                }
            }
            (_tabs.SelectedItem as CustomTab).Save();
            IDESession.Save();
            this.Cursor = Cursors.Arrow;
        }


        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrent();
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrent(true);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.N && (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))) 
            {
                NewTab(true, dbselect.SelectedItem as DataBaseConfig);
            }
            else if (e.Key == Key.W && (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
            {
                if (_tabs.SelectedItem != null) (_tabs.SelectedItem as CustomTab).CloseRequest();
            }
            else if (e.Key == Key.S && (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
            {
                SaveCurrent();
            }
            else if (e.Key == Key.O && (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
            {
                OpenFileDialog();
            }
            else if (e.Key == Key.G && (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
            {
                GoToWindow w = new GoToWindow();
                w.Owner = this;
                w.ShowDialog();
            }
            else if (e.Key == Key.F6)
            {
                CompileCurrent();
            }
            else if (e.Key == Key.F7)
            {
                if (_tabs.SelectedItem != null)
                {
                    this.Cursor = Cursors.Wait;
                    ((_tabs.SelectedItem as CustomTab).Content as SQLEdit).ExplainPlan();
                    this.Cursor = Cursors.Arrow;
                }
            }
            else if (e.Key ==  Key.F5)
            {
                if (_tabs.SelectedItem != null)
                {
                    this.Cursor = Cursors.Wait;
                    ((_tabs.SelectedItem as CustomTab).Content as SQLEdit).ExecuteSelected();
                    this.Cursor = Cursors.Arrow;
                }
            }
            
        }

        private void MenuGoTo_Click(object sender, RoutedEventArgs e)
        {
            GoToWindow w = new GoToWindow();
            w.Owner = this;
            w.ShowDialog();
        }

        private void MenuCut_Click(object sender, RoutedEventArgs e)
        {
            ((_tabs.SelectedItem as CustomTab).Content as SQLEdit).txtCode.Cut();
        }

        private void MenuCopy_Click(object sender, RoutedEventArgs e)
        {
            ((_tabs.SelectedItem as CustomTab).Content as SQLEdit).txtCode.Copy();
        }

        private void MenuPaste_Click(object sender, RoutedEventArgs e)
        {
            ((_tabs.SelectedItem as CustomTab).Content as SQLEdit).txtCode.Paste();
        }

        private void MenuUndo_Click(object sender, RoutedEventArgs e)
        {
            ((_tabs.SelectedItem as CustomTab).Content as SQLEdit).txtCode.Undo();
        }

        private void MenuRedo_Click(object sender, RoutedEventArgs e)
        {
            ((_tabs.SelectedItem as CustomTab).Content as SQLEdit).txtCode.Redo();
        }

        private void MenuCloseAll_Click(object sender, RoutedEventArgs e)
        {
            CloseAll();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CloseAll())
            {
                e.Cancel = true;
                return;
            }
            (App.Current as App).Cache.StopWorker();
            foreach (CustomTab tab in tabs)
            {
                (tab.Content as SQLEdit).StopThread();
            }
        }

        private void MenuExecSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_tabs.SelectedItem != null)
            {
                this.Cursor = Cursors.Wait;
                ((_tabs.SelectedItem as CustomTab).Content as SQLEdit).ExecuteSelected();
                this.Cursor = Cursors.Arrow;
            }
        }

        private void _tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_tabs.SelectedItem != null)
                {
                    ((_tabs.SelectedItem as CustomTab).Content as SQLEdit).txtCode.Focus();
                    tags.ItemsSource = (_tabs.SelectedItem as CustomTab).Tags;
                    (_tabs.SelectedItem as CustomTab).UnMark();
                }
            }));            
        }

        private void MenuFind_Click(object sender, RoutedEventArgs e)
        {
            if (this._tabs.SelectedItem != null) ((this._tabs.SelectedItem as CustomTab).Content as SQLEdit)._query.Focus();
        }

        private void MenuPlan_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            if (this._tabs.SelectedItem != null) ((this._tabs.SelectedItem as CustomTab).Content as SQLEdit).ExplainPlan();
            this.Cursor = Cursors.Arrow;
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow() { Owner = this }.ShowDialog();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            { 
                foreach (string file in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    LoadFile(file);
                }
                IDESession.Save();
            }
        }

        private void dbselect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ObjectsSearch();
        }

        private void ConsoleClear_Click(object sender, RoutedEventArgs e)
        {
            Console.Messages.Clear();
        }

        private void MacroManager_Click(object sender, RoutedEventArgs e)
        {
            MacroManager wnd = new MacroManager();
            wnd.Owner = this;
            wnd.ShowDialog();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            AllSessions wnd = new AllSessions();
            wnd.Owner = this;
            wnd.ShowDialog();
        }
        
        private void MenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            mnuCopy.IsEnabled = false;
            mnuCut.IsEnabled = false;
            mnuPaste.IsEnabled = false;
            mnuUndo.IsEnabled = false;
            mnuRedo.IsEnabled = false;
            mnuFind.IsEnabled = false;
            mnuGoTo.IsEnabled = false;
            if (this._tabs.SelectedItem != null)
            {
                SQLEdit editor = (this._tabs.SelectedItem as CustomTab).Content as SQLEdit;
                mnuCopy.IsEnabled = editor.txtCode.SelectionLength > 0;
                mnuCut.IsEnabled = editor.txtCode.SelectionLength > 0;
                mnuPaste.IsEnabled = true;
                mnuUndo.IsEnabled = editor.txtCode.CanUndo;
                mnuRedo.IsEnabled = editor.txtCode.CanRedo;
                mnuFind.IsEnabled = true;
                mnuGoTo.IsEnabled = true;

            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NewTab(true, dbselect.SelectedItem as DataBaseConfig, true);
            IDESession.Save();
        }

        private void objtree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (objtree.SelectedItem == null) return;
            if (!(objtree.SelectedItem is DBObject)) return;
            DBObject obj = objtree.SelectedItem as DBObject;
            
            this.Cursor = Cursors.Wait;
            if (obj.Type == "PACKAGE")
            {
                CustomTab newTab = NewTab(true, dbselect.SelectedItem as DataBaseConfig);
                (newTab.Content as SQLEdit).LoadPackageHead(obj.Name);

                newTab = NewTab(false, dbselect.SelectedItem as DataBaseConfig);
                (newTab.Content as SQLEdit).LoadPackage(obj.Name);
                IDESession.Save();
            }
            else if (obj.Type == "TABLE")
            {
                CustomTab newTab = NewTab(true, dbselect.SelectedItem as DataBaseConfig);
                (newTab.Content as SQLEdit).LoadTable(obj.Name);
                IDESession.Save();
            }

            this.Cursor = Cursors.Arrow;
        }
    }
}
