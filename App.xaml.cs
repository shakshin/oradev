using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace oradev
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string[] Args;
        public Config Configuration;
        public ObjCache.Cache Cache;

        public static event EventHandler NewFiles;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (SingleInstanceEnforcer.IsFirst(new SingleInstanceEnforcer.CommandLineDelegate(myReceive)))
            {
                // first instance
                Args = e.Args;
                
                Cache = ObjCache.Cache.LoadFromFile();
                Configuration = Config.LoadFromFile();
                Cache.RunWorker();
                this.Exit += delegate { SingleInstanceEnforcer.Cleanup(); };
                this.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);

                Stream inp = Assembly.GetExecutingAssembly().GetManifestResourceStream("oradev.ctags.exe");
                byte[] bytes = new byte[(int)inp.Length];
                inp.Read(bytes, 0, bytes.Length);
                File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ctags.exe"), bytes);
                inp.Close();
                
            }   
            // second instance
            else
            {
                // send command line args to running app, then terminate
                SingleInstanceEnforcer.PassCommandLine(e.Args);
                SingleInstanceEnforcer.Cleanup();
                this.Shutdown();
            }            
        }

        



        static void myReceive(string[] args)
        {
            if (NewFiles != null)
            {
                NewFilesEventArgs _args = new NewFilesEventArgs();
                _args.Files = args;
                NewFiles(null, _args);
            }
        }
    }
}
