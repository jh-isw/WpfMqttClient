using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace WpfMqttClient.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                WindowTitle = "Generic MQTT Client using WPF and Eclipse paho M2MQTT (Designmode)";
                BrokerUri = "test.mosquitto.org";
                ConnectDisconnectButtonText = "Connect";
                ApplicationMessages = "Last Message here...";
            }
            else
            {
                WindowTitle = "Generic MQTT Client using WPF and Eclipse paho M2MQTT";
                ConnectDisconnectButtonText = "Connect";
                ConnectDisconnetCommand = new RelayCommand(OnConnectDisconnectExecuted, OnConnectDisconnectCanExecute);
                EnterKeyCommand = new RelayCommand(OnConnectDisconnectExecuted, null);
                ClientId = Guid.NewGuid().ToString();
                ApplicationMessages = "Disconnected.\nClientId: " + ClientId + "\n";
                Messenger.Default.Register<DoCleanupMessage>(this, DoCleanup);
            }
        }

        public string WindowTitle { get; private set; }
        public string ConnectDisconnectButtonText { get; set; }
        public string BrokerUri { get; set; }
        public string ApplicationMessages { get; set; }
        public string ClientId;

        private MqttClient Client;

        public static RelayCommand ConnectDisconnetCommand { get; private set; }
        public static RelayCommand EnterKeyCommand { get; private set; }

        private void OnConnectDisconnectExecuted()
        {
            if (Client == null)
            {
                try
                {
                    Client = new MqttClient(BrokerUri);
                }
                catch (Exception e)
                {
                    //Client = null;
                    Console.WriteLine(e.Message);
                }
            } 
            
            if (Client != null && Client.IsConnected)
            {
                Client.Disconnect();
                ConnectDisconnectButtonText = "Connect";
            }
            else
            {
                try
                {
                    Client.Connect(ClientId);
                    Client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                    Client.Subscribe(new string[] { "$SYS/broker/uptime" },
                        new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                
                ConnectDisconnectButtonText = "Disconnect";
            }
        }

        private bool OnConnectDisconnectCanExecute()
        {
            return true;
        }

        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            ApplicationMessages += System.Text.Encoding.Default.GetString(e.Message) + "\n";
        }

        private void DoCleanup(DoCleanupMessage obj)
        {
            if (Client != null && Client.IsConnected)
            {
                Client.Disconnect();
            }
        }

        public class DoCleanupMessage
        {

        }
    }
}
