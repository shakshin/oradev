using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.AddIn;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.SharpDevelop.Editor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Data;
using System.Data.OracleClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace oradev
{
    /// <summary>
    /// Interaction logic for SQLEdit.xaml
    /// </summary>
    public partial class SQLEdit : UserControl
    {
        public event EventHandler HeaderChanged;
        public event EventHandler TagsRescan;
        public event EventHandler ModifiedMarkerChanged;
        public event EventHandler PendingMarkerChanged;

        private CompletionWindow cwnd ;

        private ITextMarkerService textMarkerService;

        private ITextMarker link = null;
        private List<ITextMarker> errs = new List<ITextMarker>();

        private int activityTimer = -1;
        private System.Timers.Timer _timer;
        private bool _modified = false;
        private Encoding _encoding;

        private string _fileName = "";
        private FileSystemWatcher watcher;

        private bool _pending;

        private Point MousePosition;

        private List<string> keywords = new List<string>();

        public bool Pending
        {
            get { return _pending; }
            set { 
                _pending = value;
                if (PendingMarkerChanged != null)
                {
                    PendingMarkerChanged(this, null);
                }
            }
        }

        private CustomTab _parent;

        private DBThread thread = new DBThread();

        public void StartMonitor(string fileName)
        {
            if (fileName != _fileName)
            {
                _fileName = fileName;
                if (watcher != null && watcher.EnableRaisingEvents) watcher.EnableRaisingEvents = false;
                watcher = new FileSystemWatcher();
                watcher.Path = System.IO.Path.GetDirectoryName(fileName);
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Filter = System.IO.Path.GetFileName(fileName);

                watcher.Changed += (object source, FileSystemEventArgs e) => {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        StopMonitor();
                        if (MessageBox.Show(App.Current.MainWindow, string.Format("File {0} was modified from another application. Would you like to reload it? All local changes will be lost!", fileName), "Warning", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            LoadFile(fileName, _encoding);
                        }
                    });
                };

                watcher.Deleted += (object source, FileSystemEventArgs e) => {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (MessageBox.Show(App.Current.MainWindow, string.Format("File {0} was deleted. Would you like to close tab? All local changes will be lost!", fileName), "Warning", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            _parent.CloseRequest();
                        }
                        else
                        {
                            StopMonitor();
                        }
                    });
                };

                watcher.Renamed += (object source, RenamedEventArgs e) =>
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (MessageBox.Show(App.Current.MainWindow, string.Format("File {0} was moved. Would you like to follow it?", fileName), "Warning", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            _parent.SaveFile = e.FullPath;
                            StartMonitor(e.FullPath);
                            Header = System.IO.Path.GetFileName(e.FullPath);
                        }
                        else
                        {
                            StopMonitor();
                        }
                    });
                };

                watcher.EnableRaisingEvents = true;
            }
        }

        public void StopThread()
        {
            thread.Stop();
        }

        public void StopMonitor()
        {
            _fileName = "";
            if (watcher != null && watcher.EnableRaisingEvents) watcher.EnableRaisingEvents = false;
        }

        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { 
                SetValue(HeaderProperty, value); 
                if (HeaderChanged != null)
                {
                    HeaderChanged(this, null);
                }
            }
        }
        
        // Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(SQLEdit), new PropertyMetadata(string.Empty));




        public string Type
        {
            get { return (string)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Type.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register("Type", typeof(string), typeof(SQLEdit), new PropertyMetadata(string.Empty));




        public string ObjectName
        {
            get { return (string)GetValue(ObjectNameProperty); }
            set { SetValue(ObjectNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ObjectName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ObjectNameProperty =
            DependencyProperty.Register("ObjectName", typeof(string), typeof(SQLEdit), new PropertyMetadata(string.Empty));
        
        public SQLEdit(CustomTab parentElement, DataBaseConfig db = null)
        {
            InitializeComponent();

            txtCode.TextArea.TextView.SnapsToDevicePixels = true;

            dbconfig.SelectionChanged += delegate
            {
                if (dbconfig.SelectedItem != null)
                {
                    thread.Init(dbconfig.SelectedItem as DataBaseConfig);
                    thread.Start();
                }
            };


            dbconfig.ItemsSource = (App.Current as App).Configuration.Databases;
            if (db != null)
            {
                dbconfig.SelectedItem = db;
            }
            else if (dbconfig.Items.Count > 0)
            {
                dbconfig.SelectedItem = dbconfig.Items[0];
            }
            

            _parent = parentElement;

            XmlReader reader;
            reader = XmlReader.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("oradev.highlight.xml"));

            IHighlightingDefinition def = HighlightingLoader.Load(reader, HighlightingManager.Instance);

            txtCode.SyntaxHighlighting = def;
            //txtCode.SyntaxHighlighting.GetNamedColor("Keyword").Foreground = new SimpleHighlightingBrush(Colors.Green);


            StreamReader kwreader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("oradev.keywords.txt"));
            while (!kwreader.EndOfStream)
            {
                string word = kwreader.ReadLine().Trim();
                keywords.Add(word);
                
            }

            
            

            txtCode.Options.ConvertTabsToSpaces = true;
            txtCode.Options.IndentationSize = 2;
            txtCode.Options.HighlightCurrentLine = true;
            txtCode.Options.ShowSpaces = true;

            Header = "Untitled.sql";

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += delegate {
                
                if (this.activityTimer > 0)
                {
                    this.activityTimer--;
                }
                else if (this.activityTimer == 0)
                {
                    this.activityTimer = -1;
                    InitTagsRescan();
                    IDESession.Save();
                }
            };
            _timer.Start();

            txtCode.TextArea.MouseMove += delegate (object sender, MouseEventArgs e)
            {
                MousePosition = e.GetPosition(txtCode.TextArea);
                DBObject obj = GetObjectUnderCursor();    
                if (obj != null)
                {
                    /* if (ObjectTooltip == null)
                    {
                        ObjectTooltip = new ToolTip();
                        ObjectTooltip.PlacementTarget = txtCode.TextArea;
                        ObjectTooltip.Content = GetObjDescriptionContent(obj);
                        ObjectTooltip.IsOpen = true;
                    }
                    else
                    {
                        ObjectTooltip.Content = GetObjDescriptionContent(obj);
                    } */
                    if (link != null && objOffset != link.StartOffset)
                    {
                        textMarkerService.Remove(link);
                        link = null;
                        txtCode.TextArea.TextView.Cursor = Cursors.IBeam;
                    }
                    if (link == null)
                    {
                        ITextMarker marker = textMarkerService.Create(objOffset, obj.Name.Length);
                        marker.MarkerTypes = TextMarkerTypes.NormalUnderline;
                        marker.MarkerColor = Colors.Blue;
                        marker.FontWeight = FontWeights.Normal;
                        marker.ForegroundColor = Colors.Blue;
                        link = marker;
                        txtCode.TextArea.TextView.Cursor = Cursors.Hand;
                    }
                }
                else
                {
                    /* if (ObjectTooltip != null)
                    {
                        ObjectTooltip.IsOpen = false;
                        ObjectTooltip = null;
                    } */
                    if (link != null)
                    {
                        textMarkerService.Remove(link);
                        link = null;
                        txtCode.TextArea.TextView.Cursor = Cursors.IBeam;
                    }
                }
            };

            txtCode.TextArea.MouseLeave += delegate (object sender, MouseEventArgs e) {
                /* if (ObjectTooltip != null)
                {
                    ObjectTooltip.IsOpen = false;
                    ObjectTooltip = null;
                } */
                if (link != null)
                {
                    textMarkerService.Remove(link);
                    link = null;
                    txtCode.TextArea.TextView.Cursor = Cursors.IBeam;
                }
            };

            txtCode.TextArea.TextView.MouseDown += delegate (object sender, MouseButtonEventArgs e) {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    DBObject obj = GetObjectUnderCursor();
                    if (obj == null) return;
                    if (obj.Type == "TABLE")
                    {
                        MainWindow wnd = (MainWindow.GetWindow(this) as MainWindow);
                        CustomTab tab = wnd.NewTab(true, dbconfig.SelectedItem as DataBaseConfig);
                        (tab.Content as SQLEdit).LoadTable(obj.Name);
                        IDESession.Save();
                    }
                    else if (obj.Type == "PACKAGE")
                    {
                        MainWindow wnd = (MainWindow.GetWindow(this) as MainWindow);
                        CustomTab tab = wnd.NewTab(true, dbconfig.SelectedItem as DataBaseConfig);
                        (tab.Content as SQLEdit).LoadPackageHead(obj.Name);
                        IDESession.Save();
                    }
                }
            };

            


            textMarkerService = new TextMarkerService(txtCode.Document);
            txtCode.TextArea.TextView.BackgroundRenderers.Add((IBackgroundRenderer)textMarkerService);
            txtCode.TextArea.TextView.LineTransformers.Add((IVisualLineTransformer)textMarkerService);
            IServiceContainer services = (IServiceContainer)txtCode.Document.ServiceProvider.GetService(typeof(IServiceContainer));
            if (services != null)
                services.AddService(typeof(ITextMarkerService), textMarkerService);


            
        }

        private void TextArea_MouseUp(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }

        private object GetObjDescriptionContent(DBObject obj)
        {
            StackPanel desc = new StackPanel();
            StackPanel basic = new StackPanel();
            desc.Orientation = Orientation.Vertical;
            basic.Orientation = Orientation.Horizontal;

            TextBlock tp = new TextBlock();
            Style stType = new Style();
            stType.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            stType.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Blue));
            tp.Style = stType;
            tp.Text = obj.Type.ToLower();

            TextBlock nm = new TextBlock();
            Style stName = new Style();
            stName.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            nm.Style = stName;
            nm.Padding = new Thickness(10, 0, 0, 0);
            nm.Text = obj.Name;

            basic.Children.Add(tp);
            basic.Children.Add(nm);

            desc.Children.Add(basic);

            return desc;
        }

        

        public String GetCodeText()
        {
            return txtCode.Text;
        }

        public void InitTagsRescan()
        {
            if (TagsRescan != null)
            {
                TagsRescan(this, null);
            }
        }

        public void LoadFile(String fileName, Encoding encoding)
        {
            Type = "FILE";
            _encoding = encoding;
            StreamReader reader = new StreamReader(fileName, encoding);
            txtCode.Text = reader.ReadToEnd();
            reader.Close();
            Header = System.IO.Path.GetFileName(fileName);
            InitTagsRescan();
            txtCode.IsModified = false;
            if (_modified != txtCode.IsModified)
            {
                if (ModifiedMarkerChanged != null)
                {
                    ModifiedMarkerChanged(this, null);
                }
                _modified = txtCode.IsModified;
            }
            StartMonitor(fileName);

            Regex r = new Regex(@"^create or replace package body .+ wrapped\s*\n");
            if (r.IsMatch(txtCode.Text))
            {
                Unwrap();
            }
        }

        public void Unwrap()
        {
            Unwrap unwrapper = new Unwrap();
            String unwrapped = unwrapper.Do(txtCode.Text, _encoding);
            txtCode.Text = unwrapped == String.Empty ? txtCode.Text : unwrapped;
        }

        public void SaveFile(String fileName, Encoding encoding)
        {
            Type = "FILE";
            
            _encoding = encoding;
            StopMonitor();
            StreamWriter writer = new StreamWriter(fileName, false, encoding);
            writer.Write(txtCode.Text);
            writer.Close();
            Header = System.IO.Path.GetFileName(fileName);
            txtCode.IsModified = false;
            if (_modified != txtCode.IsModified)
            {
                if (ModifiedMarkerChanged != null)
                {
                    ModifiedMarkerChanged(this, null);
                }
                _modified = txtCode.IsModified;
            }
            StartMonitor(fileName);
        }

        public void LoadTable(String tableName)
        {
            Type = "TABLE";
            ObjectName = tableName;
            txtCode.Text = Oracle.GetTable(tableName, dbconfig.SelectedItem as DataBaseConfig);
            
            this.Header = tableName;
            InitTagsRescan();
            txtCode.IsModified = false;
            if (_modified != txtCode.IsModified)
            {
                if (ModifiedMarkerChanged != null)
                {
                    ModifiedMarkerChanged(this, null);
                }
                _modified = txtCode.IsModified;
            }
            
        }

        public void LoadPackage(String packageName)
        {
            Type = "PACKAGE BODY";
            ObjectName = packageName;
            txtCode.Text = Oracle.GetPackageBody(packageName, dbconfig.SelectedItem as DataBaseConfig);
            this.Header = packageName;
            InitTagsRescan();
            txtCode.IsModified = false;
            if (_modified != txtCode.IsModified)
            {
                if (ModifiedMarkerChanged != null)
                {
                    ModifiedMarkerChanged(this, null);
                }
                _modified = txtCode.IsModified;
            }
        }
        public void LoadPackageHead(String packageName)
        {
            Type = "PACKAGE";
            ObjectName = packageName;
            txtCode.Text = Oracle.GetPackageHead(packageName, dbconfig.SelectedItem as DataBaseConfig);
            this.Header = packageName + "_";
            InitTagsRescan();
            txtCode.IsModified = false;
            if (_modified != txtCode.IsModified)
            {
                if (ModifiedMarkerChanged != null)
                {
                    ModifiedMarkerChanged(this, null);
                }
                _modified = txtCode.IsModified;
            }
        }

        public void Compile()
        {
            if (Pending) return;

            foreach (ITextMarker marker in errs) 
                textMarkerService.Remove(marker);

            Match match = Regex.Match(txtCode.Text, @"create\s+or\s+replace\s+(package)(\s+body)?\s+(\S+)\s+(as|is)", RegexOptions.IgnoreCase);

            String _type = (match.Groups[1].Value + " " + match.Groups[2].Value.Trim()).ToUpper().Trim();
            String _name = match.Groups[3].Value.ToUpper();

            if ((_type == "PACKAGE BODY" || _type == "PACKAGE") && !String.IsNullOrEmpty(_name))
            {
                ObservableCollection<DBSession> lockers = Oracle.GetLockers(_name, dbconfig.SelectedItem as DataBaseConfig);
                if (lockers.Count > 0) 
                {
                    txtCode.IsEnabled = false;
                    _query.IsEnabled = false;
                    _replace.IsEnabled = false;
                    btnReplace.IsEnabled = false;
                    btnReplaceAll.IsEnabled = false;

                    txtCode.Effect = new BlurEffect();

                    ctlLockers.Show(
                            delegate { Compile(); },
                            delegate { 
                                txtCode.IsEnabled = true;
                                _query.IsEnabled = true;
                                _replace.IsEnabled = true;
                                btnReplace.IsEnabled = true;
                                btnReplaceAll.IsEnabled = true;
                                txtCode.Effect = null;
                            },
                            dbconfig.SelectedItem as DataBaseConfig, _name, lockers);
                    return;
                }

                Console.Log(string.Format("Compiling {0} {1}...", _type.ToLower(), _name));
                String code = Regex.Replace(txtCode.Text.Trim(), @"end;\s*/\s+.*$", "end;", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                Pending = true;
                thread.Execute(code, /*);
                Oracle.ExecuteAsync(code,*/ delegate(DataTable result, long elapsed)
                {
                    Pending = false;
                    Console.Log(string.Format("Execution time: {0} ms", elapsed));
                    ObservableCollection<SourceError> errors = Oracle.CheckErrors(_type, _name, dbconfig.SelectedItem as DataBaseConfig);
                    lstErrors.ItemsSource = errors;
                    foreach (SourceError err in errors)
                    {
                        int start = GetLineBeginOffset(err.LineNumber);
                        int end = GetLineEndOffset(err.LineNumber);
                        if (start < 0 || end < 0) continue;
                        ITextMarker marker = textMarkerService.Create(start, end-start );
                        marker.MarkerTypes = TextMarkerTypes.SquigglyUnderline;
                        marker.MarkerColor = Colors.Red;
                        errs.Add(marker);
                    }
                    if (errors.Count > 0)
                    {
                        Console.Log("!!! There are some errors in yor code. Refer to errors list.");
                        tabOutput.SelectedIndex = 0;
                        txtCode.ScrollTo((lstErrors.Items[0] as SourceError).LineNumber, 1);
                        txtCode.TextArea.Caret.Line = (lstErrors.Items[0] as SourceError).LineNumber;
                        txtCode.Focus();
                        _parent.MarkErrors();
                    }
                    else
                    {
                        _parent.MarkComplete();
                        Console.Log("No errors.");
                    }
                } /*, dbconfig.SelectedItem as DataBaseConfig */);
            }
            else
            {
                Pending = true;
                thread.Execute(/*
                Oracle.ExecuteAsync(*/txtCode.Text, delegate(DataTable result, long elapsed)
                {
                    Pending = false;
                    _parent.MarkComplete();
                    Console.Log(string.Format("Execution time: {0} ms", elapsed));
                }/*, dbconfig.SelectedItem as DataBaseConfig*/);
            }
        }

        public int GetLineBeginOffset(int line)
        {
            if (line == 1) return 0;
            if (line > txtCode.LineCount || line < 1) return -1;
            MatchCollection matches = Regex.Matches(txtCode.Text, "\n");
            return matches[line - 2].Index + 1;
        }

        public int GetLineEndOffset(int line)
        {
            if (line == txtCode.LineCount - 1) return txtCode.Text.Length;
            if (line > txtCode.LineCount || line < 1) return -1;
            MatchCollection matches = Regex.Matches(txtCode.Text, "\n");
            return matches[line - 1].Index ;
        }

        public void ExecuteSelected()
        {
            if (Pending) return;

            Match match = Regex.Match(txtCode.Text, @"create\s+or\s+replace\s+(package)(\s+body)?\s+(\S+)\s+as", RegexOptions.IgnoreCase);

            if (txtCode.SelectedText.Trim().Length == 0)
            {
                String _type = (match.Groups[1].Value + " " + match.Groups[2].Value.Trim()).ToUpper().Trim();
                String _name = match.Groups[3].Value.ToUpper();

                if ((_type == "PACKAGE BODY" || _type == "PACKAGE") && !String.IsNullOrEmpty(_name))
                {
                    Compile();
                    return;
                }
            }

            String code = txtCode.SelectedText.Trim().Length > 0 ? txtCode.SelectedText.Trim() : txtCode.Text.Trim();

            if (Regex.IsMatch(code, @"^\s*select\s+", RegexOptions.IgnoreCase))
            {
                code = Regex.Replace(code, ";$", "");
                Pending = true;
                thread.Query(/*
                Oracle.QueryAsync(*/code, delegate(DataTable result, long elapsed) {
                    Pending = false;
                    Console.Log(string.Format("Execution time: {0} ms", elapsed));

                    lstOutput.Columns.Clear();

                    if (result.Columns.Count > 0 && result.Rows.Count > 0)
                    {
                        lstOutput.DataContext = result;

                        Console.Log(result.Rows.Count + " rows selected");

                        tabOutput.SelectedIndex = 1;
                    }
                    else
                    {
                        Console.Log("No rows selected");
                    }
                    _parent.MarkComplete();
                }/*, dbconfig.SelectedItem as DataBaseConfig*/);
            }
            else
            {
                Pending = true;
                thread.Query(/*
                Oracle.ExecuteAsync(*/code, delegate(DataTable result, long elapsed)
                {
                    Pending = false;
                    Console.Log(string.Format("Execution time: {0} ms", elapsed));
                }/*, dbconfig.SelectedItem as DataBaseConfig*/);
            }
        }

        public void GoToLine(int line) 
        {
            txtCode.ScrollTo(line, 1);
            txtCode.TextArea.Caret.Line = line;
            txtCode.Focus();
        }

        private void lstErrors_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstErrors.SelectedItem != null)
            {
                GoToLine((lstErrors.SelectedItem as SourceError).LineNumber);
            }
        }

        private void ShowBasicSuggestion(int offset)
        {
            String preEntered = null;
            if (offset != txtCode.CaretOffset)
            {
                preEntered = txtCode.Text.Substring(offset, txtCode.CaretOffset - offset);
            }
            cwnd = new CompletionWindow(txtCode.TextArea);
            cwnd.Width = 300;
            IList<ICompletionData> data = cwnd.CompletionList.CompletionData;


            cwnd.Closed += delegate
            {
                cwnd = null;
            };

            foreach (string word in keywords)
            {
                if (preEntered != null && !Regex.IsMatch(word, @"^" + preEntered, RegexOptions.IgnoreCase)) continue;
                data.Add(new CompletionData(word, word));
            }

            foreach (SourceCodeTag tag in _parent.Tags)
            {
                if (preEntered != null && !Regex.IsMatch(tag.Name, @"^" + preEntered, RegexOptions.IgnoreCase)) continue;
                data.Add(new CompletionData(tag.Name, tag.Type + " " + tag.Name));
            }

            if (dbconfig.Items.Count > 0)
            foreach (DBObject obj in (dbconfig.SelectedItem as DataBaseConfig).objs)
            {
                if (preEntered != null && !Regex.IsMatch(obj.Name, @"^" + preEntered, RegexOptions.IgnoreCase)) continue;
                data.Add(new CompletionData(obj.Name, GetObjDescriptionContent(obj)));
            }

            cwnd.StartOffset = offset;

            cwnd.Show();
        }

        private void ShowMemberSuggestion(String package, int startOffset, String preEntered)
        {
            (App.Current as App).Cache.GetPackageCache((dbconfig.SelectedItem as DataBaseConfig).Guid, package.ToUpper(), delegate (ObjCache.CachePackage _cp) {
                if (_cp != null && _cp.Members.Count > 0)
                {
                    IEnumerable<ObjCache.CacheMember> mtds = _cp.Members.OrderBy(m => m.Name);
                    cwnd = new CompletionWindow(txtCode.TextArea);
                    cwnd.Width = 300;
                    IList<ICompletionData> data = cwnd.CompletionList.CompletionData;
                    cwnd.Closed += delegate
                    {
                        cwnd = null;
                    };
                    cwnd.StartOffset = startOffset;
                    
                    foreach (ObjCache.CacheMember m in mtds)
                    {
                        if (preEntered != null && !Regex.IsMatch(m.Name, @"^" + preEntered, RegexOptions.IgnoreCase)) continue;
                        StackPanel desc = new StackPanel();
                        StackPanel basic = new StackPanel();
                        desc.Orientation = Orientation.Vertical;
                        basic.Orientation = Orientation.Horizontal;

                        TextBlock tp = new TextBlock();
                        Style stType = new Style();
                        stType.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
                        stType.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Blue));
                        tp.Style = stType;
                        tp.Text = m.Type;

                        TextBlock nm = new TextBlock();
                        Style stName = new Style();
                        stName.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
                        nm.Style = stName;
                        nm.Padding = new Thickness(10, 0, 0, 0);
                        nm.Text = m.Name;

                        basic.Children.Add(tp);
                        basic.Children.Add(nm);

                        desc.Children.Add(basic);

                        if (m.Prototype.Trim() != String.Empty)
                        {
                            TextBlock pt = new TextBlock();
                            Style stPrt = new Style();
                            //stPrt.Setters.Add(new Setter(TextBlock.FontStyleProperty, FontStyles.Italic));
                            pt.Style = stPrt;
                            pt.Text = Regex.Replace(Regex.Replace(Regex.Replace(m.Prototype, @",", ",\n  "), @"\(", "(\n   "), @"\)", "\n)");

                            desc.Children.Add(pt);
                        }
                        data.Add(new CompletionData(m.Name, desc));
                    }

                    cwnd.Show();
                }
            });
        }

        private void txtCode_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                e.Handled = true;
                return;
            }
            if (cwnd != null && (new[] {Key.Escape, Key.Space, Key.Enter}.Contains(e.Key) || cwnd.CompletionList.ListBox.Items.Count == 0))
           {
               cwnd.Close();
           }
           if (
               cwnd == null 
               && (e.Key >= Key.A && e.Key <= Key.Z) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) // letters
               && ((txtCode.CaretOffset > 1 && Regex.Match(txtCode.Text.Substring(txtCode.CaretOffset - 2, 1), @"\s" ).Success) || txtCode.CaretOffset == 1 )
               )
            {
                ShowBasicSuggestion(txtCode.CaretOffset-1);
            } else if(
                    cwnd == null && dbconfig.SelectedItem != null
                    && e.Key == Key.OemPeriod  && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) // letters
                )
            {
                String pkg = "";
                int i = txtCode.CaretOffset - 2;
                while (i >= 0)
                {
                    String ch = txtCode.Text.Substring(i, 1);
                    if (Regex.Match(ch, @"\s").Success) break;
                    pkg = ch + pkg;
                    i--;
                }
                ShowMemberSuggestion(pkg, txtCode.CaretOffset, null);
            }
            

            activityTimer = 2;
            if (_modified != txtCode.IsModified) {
                if (ModifiedMarkerChanged != null)
                {
                    ModifiedMarkerChanged(this, null);
                }
                _modified = txtCode.IsModified;
            }

            // text macros pocessor
            foreach (TextMacro macro in (App.Current as App).Configuration.Macros)
            {
                if (
                    macro.Key == e.Key.ToString()
                    && macro.Control == Keyboard.Modifiers.HasFlag(ModifierKeys.Control)
                    && macro.Shift == Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
                )
                {
                    int eol = txtCode.Text.IndexOf("\r\n", txtCode.CaretOffset, StringComparison.OrdinalIgnoreCase);
                    int offset = macro.AtEOL ? (eol == -1 ? txtCode.Text.Length : eol) : txtCode.CaretOffset;
                    
                    txtCode.Text = txtCode.Text.Insert(offset, macro.Text);
                    txtCode.CaretOffset = offset;
                    e.Handled = true;
                }
            }
        }

        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
            {
                _query.Focus();
            }
        }

        public void GoSearch(bool inc)
        {
            String text = txtCode.Text;
            String query = _query.Text;

            if (query.Length < 1)
            {
                return;
            }

            int start = txtCode.CaretOffset;

            if (inc) start++;

            int pos;

            txtCode.SelectionLength = 0;

            try
            {
                pos = text.IndexOf(query, start, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception )
            {
                pos = -1;
            }
            if (pos == -1)
            {
                try
                {
                    pos = text.IndexOf(query, 0, StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception )
                {
                    pos = -1;
                }
            }

            if (pos != -1)
            {
                txtCode.SelectionStart = pos;
                txtCode.SelectionLength = query.Length;
                txtCode.CaretOffset = pos;
                txtCode.TextArea.Caret.BringCaretToView();
            }
        }

        private void _query_KeyUp(object sender, KeyEventArgs e)
        {
            GoSearch(e.Key == Key.Enter);
        }

        private void tabOutput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender != e.OriginalSource) e.Handled = true;
        }

        public void ExplainPlan()
        {
            if (Pending) return;
            String code = txtCode.SelectedText.Trim().Length > 0 ? txtCode.SelectedText : txtCode.Text;

            if (Regex.IsMatch(code, @"^\s*select\s+", RegexOptions.IgnoreCase))
            {
                code = Regex.Replace(code, ";$", "");
                DataTable result = Oracle.ExplainPlan(code, dbconfig.SelectedItem as DataBaseConfig);

                lstOutput.Columns.Clear();

                if (result.Columns.Count > 0 && result.Rows.Count > 0)
                {
                    lstOutput.DataContext = result;
                    tabOutput.SelectedIndex = 1;
                }
                else
                {
                    Console.Log("No plan gathered");
                }
            }
            else
            {
                MessageBox.Show("Selected text is not SELECT query", "Warning", MessageBoxButton.OK);
            }
        }

        private void lstOutput_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
        }

        private void MenuCopy_Click(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.Cursor = Cursors.Wait;
            String text = "";

            DataTable data = lstOutput.DataContext as DataTable;
            int[] w = new int[data.Columns.Count];
            Array.ForEach(w, (i) => { i = 0; });

            for (int row = 0; row < lstOutput.SelectedItems.Count; row++)
            {
                for (int col = 0; col < data.Columns.Count; col++)
                {
                    w[col] = Math.Max(w[col], (lstOutput.SelectedItems[row] as DataRowView)[col].ToString().Length);
                }
            }

            for (int row = 0; row < lstOutput.SelectedItems.Count; row++)
            {
                for (int col = 0; col < data.Columns.Count; col++)
                {
                    text += (lstOutput.SelectedItems[row] as DataRowView)[col].ToString().PadRight(w[col]) + " | ";
                }
                text += "\r\n";
            }

            Clipboard.SetText(text);
            App.Current.MainWindow.Cursor = Cursors.Arrow;
            
        }

        private void MenuCopyAll_Click(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.Cursor = Cursors.Wait;
            String text = "";
            DataTable data = lstOutput.DataContext as DataTable;

            int[] w = new int[data.Columns.Count];
            Array.ForEach(w, (i) => { i = 0; });

            for (int row = 0; row < data.Rows.Count; row++)
            {
                for (int col = 0; col < data.Columns.Count; col++)
                {
                    w[col] = Math.Max(w[col], data.Rows[row][col].ToString().Length);
                }
            }

            for (int row = 0; row < data.Rows.Count; row++)
            {
                for (int col = 0; col < data.Columns.Count; col++)
                {
                    text += data.Rows[row][col].ToString().PadRight(w[col]) + " | ";
                }
                text += "\r\n";
            }

            Clipboard.SetText(text);
            App.Current.MainWindow.Cursor = Cursors.Arrow;
        }

        private void MenuCopyAllH_Click(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.Cursor = Cursors.Wait;
            String text = "";
            DataTable data = lstOutput.DataContext as DataTable;

            int[] w = new int[data.Columns.Count];
            Array.ForEach(w, (i) => { i = 0; });

            for (int row = 0; row < data.Rows.Count; row++)
            {
                for (int col = 0; col < data.Columns.Count; col++)
                {
                    w[col] = Math.Max(w[col], data.Rows[row][col].ToString().Length);
                }
            }
            for (int col = 0; col < data.Columns.Count; col++)
            {
                w[col] = Math.Max(w[col], data.Columns[col].Caption.Length);
            }

            for (int col = 0; col < data.Columns.Count; col++)
            {
                text += data.Columns[col].Caption.PadRight(w[col]) + " | ";
            }

            String hspl = "".PadRight(text.Length, '-');
            text += "\r\n" + hspl + "\r\n";

            for (int row = 0; row < data.Rows.Count; row++)
            {
                for (int col = 0; col < data.Columns.Count; col++)
                {
                    text += data.Rows[row][col].ToString().PadRight(w[col]) + " | ";
                }
                text += "\r\n";
            }

            Clipboard.SetText(text);
            App.Current.MainWindow.Cursor = Cursors.Arrow;
        }

        private void SearchExpand_Click(object sender, RoutedEventArgs e)
        {
            if (replacer.Visibility == Visibility.Collapsed)
            {
                replacer.Width = Math.Round(_query.Width / 2);
                replacer.Visibility = Visibility.Visible;
                SearchExpand.Content = ">";
            }
            else
            {
                replacer.Visibility = Visibility.Collapsed;
                SearchExpand.Content = "<";
            }
        }

        private void Replace_Click(object sender, RoutedEventArgs e)
        {
            if (txtCode.SelectedText.ToUpper() == _query.Text.ToUpper())
            {
                int pos = txtCode.SelectionStart;
                int offset = txtCode.CaretOffset;
                txtCode.Text = txtCode.Text.Remove(txtCode.SelectionStart, txtCode.SelectionLength);
                txtCode.Text = txtCode.Text.Insert(pos, _replace.Text);
                txtCode.CaretOffset = offset;
            }
            GoSearch(true);
            if (_modified != txtCode.IsModified)
            {
                if (ModifiedMarkerChanged != null)
                {
                    ModifiedMarkerChanged(this, null);
                }
                _modified = txtCode.IsModified;
            }
        }

        private void ReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.Cursor = Cursors.Wait;
            string query = _query.Text;
            if (query.Length < 1) return;
            txtCode.Text = txtCode.Text.Replace(query, _replace.Text);
            App.Current.MainWindow.Cursor = Cursors.Arrow;
            if (_modified != txtCode.IsModified)
            {
                if (ModifiedMarkerChanged != null)
                {
                    ModifiedMarkerChanged(this, null);
                }
                _modified = txtCode.IsModified;
            }
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            GoSearch(true);
        }

        private void mnuCopy_Click(object sender, RoutedEventArgs e)
        {
            txtCode.Copy();
        }

        private void mnuCut_Click(object sender, RoutedEventArgs e)
        {
            txtCode.Cut();
        }

        private void mnuPaste_Click(object sender, RoutedEventArgs e)
        {
            txtCode.Paste();
        }

        private void mnuExecute_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSelected();
        }

        private void mnuPlan_Click(object sender, RoutedEventArgs e)
        {
            ExplainPlan();
        }

        private void mnuUndo_Click(object sender, RoutedEventArgs e)
        {
            txtCode.Undo();
        }

        private void mnuRedo_Click(object sender, RoutedEventArgs e)
        {
            txtCode.Redo();
        }
        
        private void txtCode_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (txtCode.SelectionLength == 0)
            {
                mnuCopy.IsEnabled = false;
                mnuCut.IsEnabled = false;
                mnuExecute.IsEnabled = false;
                mnuPlan.IsEnabled = false;
            } else
            {
                mnuCopy.IsEnabled = true;
                mnuCut.IsEnabled = true;
                mnuExecute.IsEnabled = true;
                mnuPlan.IsEnabled = true;
            }

            mnuUndo.IsEnabled = txtCode.CanUndo;
            mnuRedo.IsEnabled = txtCode.CanRedo;

            mnuOBody.Visibility = Visibility.Collapsed;
            mnuOHead.Visibility = Visibility.Collapsed;
            mnuOTable.Visibility = Visibility.Collapsed;
            sepOobj.Visibility = Visibility.Collapsed;

            DBObject obj = GetObjectUnderCursor();
            if (obj == null) return;

            if (obj.Type == "TABLE")
            {
                mnuOTable.Visibility = Visibility.Visible;
                sepOobj.Visibility = Visibility.Visible;

                mnuOTable.Header = "Open table " + obj.Name;
                mnuOTable.Tag = obj.Name;
            }
            else if (obj.Type == "PACKAGE")
            {
                mnuOBody.Visibility = Visibility.Visible;
                mnuOHead.Visibility = Visibility.Visible;
                sepOobj.Visibility = Visibility.Visible;

                mnuOHead.Header = "Open package header " + obj.Name;
                mnuOBody.Header = "Open package body " + obj.Name;
                mnuOHead.Tag = obj.Name;
                mnuOBody.Tag = obj.Name;
            }

        }

        private int objOffset;

        private DBObject GetObjectUnderCursor()
        {
            if (dbconfig.SelectedValue == null) return null;
            if (txtCode.Text.Length < 1) return null;

            TextViewPosition? pos = txtCode.GetPositionFromPoint(MousePosition);
            if (!pos.HasValue) return null;
            TextViewPosition p = pos.GetValueOrDefault();

            int offset = txtCode.Document.GetOffset(p.Line, p.Column);
            String word = String.Empty;
            Int64 idx = -1;
            foreach (Match m in Regex.Matches(txtCode.Text, @"(\w+)"))
            {
                if (m.Index <= offset + 1 && m.Index > idx)
                {
                    word = m.Value;
                    idx = m.Index;
                    objOffset = m.Index;
                }
            }

            if (objOffset > 0 && txtCode.Text.Substring(objOffset - 1, 1) == ".") return null;

            foreach (DBObject obj in (dbconfig.SelectedValue as DataBaseConfig).objs)
            {
                if (obj.Name == word.ToUpper())
                    return obj;
            }

            return null;
        }

        private void mnuOTable_Click(object sender, RoutedEventArgs e)
        {
            MainWindow wnd = (MainWindow.GetWindow(this) as MainWindow);
            CustomTab tab = wnd.NewTab(true, dbconfig.SelectedItem as DataBaseConfig);
            (tab.Content as SQLEdit).LoadTable(mnuOTable.Tag as String);
            IDESession.Save();
        }

        private void mnuOHead_Click(object sender, RoutedEventArgs e)
        {
            MainWindow wnd = (MainWindow.GetWindow(this) as MainWindow);
            CustomTab tab = wnd.NewTab(true, dbconfig.SelectedItem as DataBaseConfig);
            (tab.Content as SQLEdit).LoadPackageHead(mnuOHead.Tag as String);
            IDESession.Save();
        }

        private void mnuOBody_Click(object sender, RoutedEventArgs e)
        {
            MainWindow wnd = (MainWindow.GetWindow(this) as MainWindow);
            CustomTab tab = wnd.NewTab(true, dbconfig.SelectedItem as DataBaseConfig);
            (tab.Content as SQLEdit).LoadPackage(mnuOBody.Tag as String);
            IDESession.Save();
        }

        private void txtCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                String memberPart = null;
                String part = "";
                int offset = 0;
                int i = txtCode.CaretOffset - 1;
                while (i >= 0)
                {
                    String ch = txtCode.Text.Substring(i, 1);
                    if (Regex.Match(ch, @"\s").Success) break;
                    if (ch == ".")
                    {
                        memberPart = part;
                        part = "";
                        offset = i + 1;
                        i--;
                        continue;
                    }
                    part = ch + part;
                    i--;
                }
                if (memberPart != null)
                {
                    if (part != "")
                    {
                        e.Handled = true;
                        ShowMemberSuggestion(part, offset, memberPart);
                    }
                }
                else
                {
                    e.Handled = true;
                    ShowBasicSuggestion(i + 1);
                }
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            thread.Stop();
            thread.Start();
        }

        private void Commit_Click(object sender, RoutedEventArgs e)
        {
            thread.Commit();
        }

        private void Rollback_Click(object sender, RoutedEventArgs e)
        {
            thread.Rollback();
        }
    }
}
