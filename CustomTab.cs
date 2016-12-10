using oradev.Parser;
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



        private List<StructureElement> elements = new List<StructureElement>();

        private void TagsRescan()
        {
            if (Tags.Count > 0)
            {
                Tags.Clear();
            }
            String code = (this.Content as SQLEdit).GetCodeText();
            Parser.Parser parser = new Parser.Parser(code);
            StructureParser sparser = new StructureParser(parser.GetLexemes());
            (Content as SQLEdit).cbTags.Refresh(sparser.GetStructure());

            if (sparser.GetStructure().Children.Count > 0)
                if (sparser.GetStructure().Children[0].Type == Parser.StructureElement.ElementType.Package ||
                    sparser.GetStructure().Children[0].Type == Parser.StructureElement.ElementType.PackageBody)
                    foreach (StructureElement elem in sparser.GetStructure().Children[0].Children)
                    {
                        SourceCodeTag tag = new SourceCodeTag();
                        tag.Name = elem.Identifier;
                        tag.Type = elem.Type.ToString();
                        tag.Offset = elem.Expression.Lexemes[0].Offset;
                        Tags.Add(tag);
                    }
            elements.Clear();
            FillElements(sparser.GetStructure());
            (Content as SQLEdit).SetTags(elements);
        }

        private void FillElements(StructureElement el)
        {
            elements.Add(el);
            foreach (StructureElement e in el.Children)
                FillElements(e);
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
