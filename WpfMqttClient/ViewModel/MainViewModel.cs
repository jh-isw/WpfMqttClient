using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using WpfMqttClient.Model;

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
                WindowTitle = "Generic MQTT Client using WPF and MQTTnet (Designmode)";
                BrokerUri = "test.mosquitto.org";
                ConnectDisconnectButtonText = "Connect";
                ApplicationMessages = "The" + Environment.NewLine
                    + "quick" + Environment.NewLine
                    + "brown" + Environment.NewLine
                    + "fox" + Environment.NewLine
                    + "jumps" + Environment.NewLine
                    + "over" + Environment.NewLine
                    + "the"  + Environment.NewLine
                    + "lazy" + Environment.NewLine
                    + "dog" + Environment.NewLine;
                Datapoints.Add(new DatapointModel { Identifier = "foo", Value = "bar" });
            }
            else
            {
                WindowTitle = "Generic MQTT Client using WPF and MQTTnet";

                ConnectDisconnectButtonText = "Connect";
                ConnectDisconnetCommand = new RelayCommand(OnConnectDisconnectExecuted, OnConnectDisconnectCanExecute);
                ConnectDisconnectReturnKeyCommand = new RelayCommand(OnConnectDisconnectExecuted, null);

                NewDatapointCommand = new RelayCommand(OnNewDatapointExecuted, OnNewDatapintCanExecute);
                NewDatapointReturnKeyCommand = new RelayCommand(OnNewDatapointExecuted, OnNewDatapintCanExecute);

                ClientId = Guid.NewGuid().ToString();
                ApplicationMessages = "Disconnected.\nClientId: " + ClientId + "\n";
                Messenger.Default.Register<DoCleanupMessage>(this, DoCleanup);
                Datapoints = new ObservableCollection<DatapointModel>();
                //Datapoints.CollectionChanged += UpdateDatapointList;
                DatapointsView = CollectionViewSource.GetDefaultView(Datapoints) as ListCollectionView;
                DatapointsView.CurrentChanged += (s, e) =>
                {
                    RaisePropertyChanged(() => SelectedDatapointModel);
                };
            }
        }

        

        //private void UpdateDatapointList(object sender, NotifyCollectionChangedEventArgs e)
        //{
        //    foreach (DatapointModel var in e.NewItems)
        //    {
        //        Console.WriteLine(var.ToString());
        //    }
        //}

        public string WindowTitle { get; private set; }

        private string _connectDisconnectButtonText;
        public string ConnectDisconnectButtonText
        {
            get
            {
                return _connectDisconnectButtonText;
            }
            set
            {
                if (value == _connectDisconnectButtonText)
                {
                    return;
                }
                _connectDisconnectButtonText = value;
                RaisePropertyChanged();
            }
        }

        private string _brokerUri;
        public string BrokerUri
        {
            get
            {
                return _brokerUri;
            }
            set
            {
                if (value == _brokerUri)
                {
                    return;
                }
                _brokerUri = value;
                RaisePropertyChanged();
            }
        }

        private string _applicationMessages;
        public string ApplicationMessages
        {
            get
            {
                return _applicationMessages;
            }
            set
            {
                if (value == _applicationMessages)
                {
                    return;
                }
                _applicationMessages = value;
                RaisePropertyChanged();
            }
        }

        private string _clientId;
        public string ClientId
        {
            get
            {
                return _clientId;
            }
            set
            {
                if (value == _clientId)
                {
                    return;
                }
                _clientId = value;
                RaisePropertyChanged();
            }
        }

        private string _newDatapointName;
        public string NewDatapointName
        {
            get
            {
                return _newDatapointName;
            }
            set
            {
                if (value == _newDatapointName)
                {
                    return;
                }
                _newDatapointName = value;
                RaisePropertyChanged();
            }
        }

        private IManagedMqttClient Client;

        private ObservableCollection<DatapointModel> Datapoints { get; }

        public DatapointModel SelectedDatapointModel
        {
            get => DatapointsView.CurrentItem as DatapointModel;
            set
            {
                DatapointsView.MoveCurrentTo(value);
                RaisePropertyChanged();
            }
        }

        public ICollectionView DatapointsView { get; }

        #region Commands
        public static RelayCommand ConnectDisconnetCommand { get; private set; }
        public static RelayCommand ConnectDisconnectReturnKeyCommand { get; private set; }

        public static RelayCommand NewDatapointCommand { get; private set; }
        public static RelayCommand NewDatapointReturnKeyCommand { get; private set; }
        #endregion
        private async void OnConnectDisconnectExecuted()
        {
            if ((Client == null) || (Client != null && !Client.IsStarted)) // kein Client bisher erzeugt oder angehalten
            {
                try
                {
                    var options = new ManagedMqttClientOptionsBuilder()
                        .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                        .WithClientOptions(new MqttClientOptionsBuilder()
                            .WithClientId(ClientId)
                            .WithTcpServer(BrokerUri)
                            /*.WithTls()*/.Build())
                        .Build();
                    Client = new MqttFactory().CreateManagedMqttClient();
                    Client.ApplicationMessageReceived += OnMessageReceived;
                    Client.Connected += OnConnected;
                    Client.ConnectingFailed += OnConnectingFailed;
                    await Client.StartAsync(options);
                    ConnectDisconnectButtonText = "Disconnect";
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else // Client ist erzeugt
            {
                await Client.StopAsync();
                ConnectDisconnectButtonText = "Connect";
            }
        }

        private bool OnConnectDisconnectCanExecute()
        {
            return true;
        }

        private async void OnNewDatapointExecuted()
        {
            await Client.SubscribeAsync(new TopicFilterBuilder().WithTopic(/*"$SYS/broker/uptime"*/NewDatapointName).Build());
        }

        private bool OnNewDatapintCanExecute()
        {
            return true;
        }

        private void OnConnectingFailed(object sender, MqttManagedProcessFailedEventArgs e)
        {
            Console.WriteLine("OnConnectingFailed called with e: " + e.ToString());
        }

        private void OnConnected(object sender, MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("OnConnected called with e: " + e.ToString());
        }

        private void OnMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            var message = System.Text.Encoding.Default.GetString(e.ApplicationMessage.Payload);
            Console.WriteLine(message);
            ApplicationMessages += message + "\n";
        }

        private void DoCleanup(DoCleanupMessage obj)
        {
            if (Client != null && Client.IsConnected)
            {
                Client.StopAsync();
                Client.Dispose();
            }
        }

        public class DoCleanupMessage
        {

        }
    }
}
