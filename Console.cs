using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace oradev
{
    public class ConsoleMessage : DependencyObject
    {
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ConsoleMessage), new PropertyMetadata(string.Empty));




        public int Id
        {
            get { return (int)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Id.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IdProperty =
            DependencyProperty.Register("Id", typeof(int), typeof(ConsoleMessage), new PropertyMetadata(0));


        public override String ToString()
        {
            return Text;
        }


    }

    public class Console : DependencyObject
    {
        public static ObservableCollection<ConsoleMessage> Messages = new ObservableCollection<ConsoleMessage>();

        private static int _cnt = 0;

        public static void Log(string text)
        {
            App.Current.Dispatcher.Invoke((Action) delegate {
                _cnt++;
                ConsoleMessage msg = new ConsoleMessage() {
                    Id = _cnt,
                    Text = text
                };

                Messages.Add(msg);
                (App.Current.MainWindow as MainWindow).lstConsole.SelectedItem = msg;
                (App.Current.MainWindow as MainWindow).lstConsole.ScrollIntoView((App.Current.MainWindow as MainWindow).lstConsole.SelectedItem);
            });
        }
    }
}
