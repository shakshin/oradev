using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace oradev
{
    [Serializable]
    class SingleInstanceEnforcer : MarshalByRefObject
    {
        private static IpcChannel _mIpcChannel;
        private static Mutex _mMutex;

        private const string PORT_NAME = "CPORADEV";
        private const string SERVICE_NAME = "IPDelegates";
        private const string SERVICE_URL = "ipc://" + PORT_NAME + "/" + SERVICE_NAME;
        private const string UNIQUE_IDENTIFIER = "C2C365FA-78B3-45E4-B801-6D413AD9B83B";

        public delegate void CommandLineDelegate(string[] args);

        static private CommandLineDelegate _mCommandLine;
        static public CommandLineDelegate CommandLineHandler
        {
            get
            {
                return _mCommandLine;
            }
            set
            {
                _mCommandLine = value;
            }
        }

        public static bool IsFirst(CommandLineDelegate r)
        {
            if (IsFirst())
            {
                CommandLineHandler += r;
                return true;
            }

            return false;
        }

        public static bool IsFirst()
        {
            _mMutex = new Mutex(false, UNIQUE_IDENTIFIER);

            if (_mMutex.WaitOne(1, true))
            {
                //We locked it! We are the first instance
                CreateInstanceChannel();
                return true;
            }

            //Not the first instance
            _mMutex.Close();
            _mMutex = null;
            return false;
        }

        private static void CreateInstanceChannel()
        {
            // correct serialization of delegates
            BinaryServerFormatterSinkProvider serverProv = new BinaryServerFormatterSinkProvider
            {
                TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full
            };

            BinaryClientFormatterSinkProvider clientProv = new BinaryClientFormatterSinkProvider();

            // Create and register the channel
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties["portName"] = PORT_NAME;
            _mIpcChannel = new IpcChannel(properties, clientProv, serverProv);

            ChannelServices.RegisterChannel(_mIpcChannel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(SingleInstanceEnforcer),
                SERVICE_NAME,
                WellKnownObjectMode.SingleCall);
        }

        public static void Cleanup()
        {
            if (_mMutex != null)
            {
                _mMutex.Close();
            }

            if (_mIpcChannel != null)
            {
                _mIpcChannel.StopListening(null);
            }

            _mMutex = null;
            _mIpcChannel = null;
        }

        public static void PassCommandLine(string[] s)
        {
            IpcChannel channel = new IpcChannel("IPC_Client");
            ChannelServices.RegisterChannel(channel, false);
            SingleInstanceEnforcer ctrl = (SingleInstanceEnforcer)Activator.GetObject(typeof(SingleInstanceEnforcer), SERVICE_URL);
            ctrl.ReceiveCommandLine(s);
        }

        public void ReceiveCommandLine(string[] s)
        {
            if (_mCommandLine != null)
            {
                _mCommandLine(s);
            }
        }
    }
}
