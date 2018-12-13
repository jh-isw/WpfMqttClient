using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Documents;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
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

                DispatcherHelper.Initialize();

                ConnectDisconnectButtonText = "Connect";
                ConnectDisconnectCommand = new RelayCommand(OnConnectDisconnectExecuted, OnConnectDisconnectCanExecute);
                ConnectDisconnectReturnKeyCommand = new RelayCommand(OnConnectDisconnectExecuted, null);

                NewDatapointCommand = new RelayCommand(OnNewDatapointExecuted, OnNewDatapointCanExecute);
                NewDatapointReturnKeyCommand = new RelayCommand(OnNewDatapointExecuted, OnNewDatapointCanExecute);

                RestoreDefaultsCommand = new RelayCommand(OnRestoreDefaultsExecuted, null);

                ClearOutputBoxContent = new RelayCommand(OnClearOutputBoxContentExecuted, null);

                WithTlsCommand = new RelayCommand(OnWithTlsExecuted, null);

                ClientId = Guid.NewGuid().ToString();
                ApplicationMessages = "Disconnected.\nClientId: " + ClientId + "\n";
                Messenger.Default.Register<DoCleanupMessage>(this, DoCleanup);
                var dpList = new List<DatapointModel>();
                dpList.Add(new DatapointModel
                {
                    Identifier = "$SYS/broker/uptime",
                    Value = ""
                });
                Datapoints = new ObservableCollection<DatapointModel>(dpList);
                foreach(var item in Datapoints)
                {
                    item.PropertyChanged += DatapointsPropertyChanged;
                }
                //Datapoints.CollectionChanged += UpdateDatapointList;
                DatapointsView = CollectionViewSource.GetDefaultView(Datapoints) as ListCollectionView;
                DatapointsView.CurrentChanged += (s, e) =>
                {
                    RaisePropertyChanged(() => SelectedDatapointModel);
                };
            }
        }

        private void DatapointsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //DatapointsView.Refresh();
            DispatcherHelper.RunAsync(() => DatapointsView.Refresh());
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

        private bool _connectedToBroker;
        public bool ConnectedToBroker
        {
            get
            {
                return _connectedToBroker;
            }
            set
            {
                if (value == _connectedToBroker)
                {
                    return;
                }
                _connectedToBroker = value;
                RaisePropertyChanged();
            }
        }

        private bool _withTls = true;
        public bool WithTls
        {
            get
            {
                return _withTls;
            }
            set
            {
                if (value == _withTls)
                {
                    return;
                }
                _withTls = value;
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
        public RelayCommand ConnectDisconnectCommand { get; private set; }
        public static RelayCommand ConnectDisconnectReturnKeyCommand { get; private set; }

        public RelayCommand NewDatapointCommand { get; private set; }
        public static RelayCommand NewDatapointReturnKeyCommand { get; private set; }

        public RelayCommand RestoreDefaultsCommand { get; private set; }

        public RelayCommand ClearOutputBoxContent { get; private set; }

        public RelayCommand WithTlsCommand { get; private set; }

        private async void OnConnectDisconnectExecuted()
        {
            if ((Client == null) || (Client != null && !Client.IsStarted)) // kein Client bisher erzeugt oder angehalten
            {
                try
                {
                    IMqttClientOptions options;
                    if (WithTls)
                    {
                        options = new MqttClientOptionsBuilder()
                            .WithClientId(ClientId)
                            .WithTcpServer(BrokerUri)
                            .WithTls()
                            .Build();
                    }
                    else
                    {
                        options = new MqttClientOptionsBuilder()
                            .WithClientId(ClientId)
                            .WithTcpServer(BrokerUri)
                            .Build();
                    }

                    var managedOptions = new ManagedMqttClientOptionsBuilder()
                        .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                        .WithClientOptions(options)
                        .Build();
                    Client = new MqttFactory().CreateManagedMqttClient();
                    Client.ApplicationMessageReceived += OnMessageReceived;
                    Client.Connected += OnConnected;
                    Client.Disconnected += OnDisconnected;
                    Client.ConnectingFailed += OnConnectingFailed;
                    await Client.StartAsync(managedOptions);
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
            NewDatapointName = "";
        }

        private bool OnNewDatapointCanExecute()
        {
            return true;
        }

        private void OnRestoreDefaultsExecuted()
        {
            BrokerUri = "iot.eclipse.org";
            NewDatapointName = "$SYS/broker/uptime";
        }
        
        private void OnClearOutputBoxContentExecuted()
        {
            ApplicationMessages = string.Empty;
        }
        
        private void OnWithTlsExecuted()
        {
            WithTls = !WithTls;
            Console.WriteLine(WithTls? "TLS enabled" : "TLS disabled");
        }
        #endregion

        private void OnConnectingFailed(object sender, MqttManagedProcessFailedEventArgs e)
        {
            ConnectedToBroker = false;
#if DEBUG
            Console.WriteLine("OnConnectingFailed called with e: " + e.ToString());
#endif
            ApplicationMessages += "Connection failed\n";
            ConnectDisconnectButtonText = "Stop trying";
        }

        private void OnConnected(object sender, MqttClientConnectedEventArgs e)
        {
            ApplicationMessages += "Connection success\n";
            ConnectedToBroker = true;
        }

        private void OnDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            ConnectedToBroker = false;
        }

        private void OnMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            var message = System.Text.Encoding.Default.GetString(e.ApplicationMessage.Payload);
            Console.WriteLine(message);
            ApplicationMessages += message + "\n";
            // Liste nach Topic durchlaufen und Wert updaten
            Datapoints[0].Value = message;
        }

        public void DoCleanup(DoCleanupMessage obj)
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
