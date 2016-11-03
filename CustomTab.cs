using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace oradev
{
    public class CustomTab : TabItem
    {
        public event EventHandler CloseButtonPressed;

        public ObservableCollection<SourceCodeTag> Tags = new ObservableCollection<SourceCodeTag>();


        public void MarkComplete()
        {
            if ((App.Current.MainWindow as MainWindow)._tabs.SelectedItem != this)
                (Header as CustomTabHeader).CompleteMarker = Visibility.Visible;
        }

        public void MarkErrors()
        {
            if ((App.Current.MainWindow as MainWindow)._tabs.SelectedItem != this)
                (Header as CustomTabHeader).ErrorsMarker = Visibility.Visible;
        }

        public void UnMark()
        {
            (Header as CustomTabHeader).CompleteMarker = Visibility.Collapsed;
            (Header as CustomTabHeader).ErrorsMarker = Visibility.Collapsed;
        }

        public Encoding FileEncoding
        {
            get { return (Encoding)GetValue(FileEncodingProperty); }
            set { SetValue(FileEncodingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FileEncoding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FileEncodingProperty =
            DependencyProperty.Register("FileEncoding", typeof(Encoding), typeof(CustomTab), new PropertyMetadata(Encoding.GetEncoding(866)));




        public String SaveFile
        {
            get { return (String)GetValue(SaveFileProperty); }
            set { SetValue(SaveFileProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SaveFile.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SaveFileProperty =
            DependencyProperty.Register("SaveFile", typeof(String), typeof(CustomTab), new PropertyMetadata(string.Empty));



        

        private void TagsRescan()
        {
            if (Tags.Count > 0)
            {
                Tags.Clear();
            }
            try
            {
                String code = (this.Content as SQLEdit).GetCodeText();


                String tempFile = Path.GetTempFileName() + ".sql";
                StreamWriter stream = new StreamWriter(tempFile, false, Encoding.UTF8);
                stream.Write(code);
                stream.Close();

                ProcessStartInfo cmd = new ProcessStartInfo(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ctags.exe"));
                cmd.CreateNoWindow = true;
                cmd.UseShellExecute = false;
                cmd.RedirectStandardOutput = true;
                cmd.Arguments = "-n --format=2 --fields=+K -f " + tempFile + ".tags " + tempFile;

                Process proc = Process.Start(cmd);
                proc.WaitForExit();

                StreamReader reader = new StreamReader(tempFile + ".tags", Encoding.UTF8);
                String _tags = reader.ReadToEnd();
                reader.Close();

                File.Delete(tempFile);
                File.Delete(tempFile + ".tags");

                foreach (String line in Regex.Split(_tags, "\r\n"))
                {
                    if (!String.IsNullOrEmpty(line) && line.Substring(0, 1) != "!")
                    {
                        String[] elements = Regex.Split(line, "\t");
                        if (elements[3] == "function" || elements[3] == "procedure" || elements[3] == "cursor")
                        {
                            SourceCodeTag tag = new SourceCodeTag();
                            tag.Name = elements[0];
                            tag.Type = elements[3];
                            tag.Line = int.Parse(elements[2].Replace(";\"", ""));
                            Tags.Add(tag);
                        }
                    }
                }
            }
            catch (Exception ) { 

            }
            
        }

        public CustomTab(DataBaseConfig db)
        {
            this.Header = new CustomTabHeader();
            this.Content = new SQLEdit(this, db);
            try
            {
                this.FileEncoding = Encoding.GetEncoding((App.Current as App).Configuration.FileEncoding);
            } catch (Exception)
            {
                this.FileEncoding = Encoding.GetEncoding(866);
            }
            (this.Header as CustomTabHeader).Title = (this.Content as SQLEdit).Header;
            (this.Content as SQLEdit).HeaderChanged += delegate { (this.Header as CustomTabHeader).Title = (this.Content as SQLEdit).Header; };
            (this.Content as SQLEdit).ModifiedMarkerChanged += delegate { (this.Header as CustomTabHeader).ModifiedMarker = (this.Content as SQLEdit).txtCode.IsModified ? "*" : " "; };
            (this.Content as SQLEdit).PendingMarkerChanged += delegate { (this.Header as CustomTabHeader).PendingMarker = (this.Content as SQLEdit).Pending ? Visibility.Visible : Visibility.Collapsed; };
            (this.Content as SQLEdit).TagsRescan += delegate
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    TagsRescan();
                });
            };
            (this.Header as CustomTabHeader).CloseButtonPressed += delegate
            {
                if (this.CloseButtonPressed != null)
                {
                    this.CloseButtonPressed(this, null);
                }
            };
        }

        
        public void LoadFile(String fileName)
        {
            FileEncoding = Encoding.GetEncoding((App.Current as App).Configuration.FileEncoding);
            (Content as SQLEdit).LoadFile(fileName, FileEncoding);
            SaveFile = fileName;
            
            
        }

        public void Save() 
        {
            (Content as SQLEdit).SaveFile(SaveFile, FileEncoding);
            
        }

        public void LoadFile(String fileName, Encoding encoding)
        {
            (Content as SQLEdit).LoadFile(fileName, encoding);
            SaveFile = fileName;
            FileEncoding = encoding;
            
        }

        public void CloseRequest() 
        {
            if (CloseButtonPressed != null)
            {
                CloseButtonPressed(this, null);
            }
        }
    }
}
