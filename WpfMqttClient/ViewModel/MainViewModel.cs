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
                ApplicationMessages = "The" + Environment.NewLine
                    + "quick" + Environment.NewLine
                    + "brown" + Environment.NewLine
                    + "fox" + Environment.NewLine
                    + "jumps" + Environment.NewLine
                    + "over" + Environment.NewLine
                    + "the"  + Environment.NewLine
                    + "lazy" + Environment.NewLine
                    + "dog" + Environment.NewLine;
            }
            else
            {
                WindowTitle = "Generic MQTT Client using WPF and MQTTnet";

                DispatcherHelper.Initialize();

                ConnectCommand = new RelayCommand(OnConnectCommandExecuted, OnConnectCommandCanExecute);
                
                DisconnectCommand = new RelayCommand(OnDisconnectCommandExecuted, OnDisconnectCommandCanExecute);

                AddDatasourceCommand = new RelayCommand(OnAddDatasourceExecuted, OnAddDatasourceCanExecute);
                AddDatasourceReturnKeyCommand = new RelayCommand(OnAddDatasourceExecuted, null);

                NewDatapointCommand = new RelayCommand(OnNewDatapointExecuted, OnNewDatapointCanExecute);
                NewDatapointReturnKeyCommand = new RelayCommand(OnNewDatapointExecuted, OnNewDatapointCanExecute);

                RestoreDefaultsCommand = new RelayCommand(OnRestoreDefaultsExecuted, null);

                ClearOutputBoxContent = new RelayCommand(OnClearOutputBoxContentExecuted, null);

                WithTlsCommand = new RelayCommand(OnWithTlsExecuted, null);

                EvaluateDatasourcesContextMenu = new RelayCommand(OnEvaluateDatasourcesContextMenuExecuted, null);

                EraseDatasourceCommand = new RelayCommand(OnEraseDatasourceCommandExecuted, null);

                Messenger.Default.Register<DoCleanupMessage>(this, DoCleanup);

                var dsList = new List<DatasourceModel>();
                Datasources = new ObservableCollection<DatasourceModel>(dsList);
                DatasourcesView = CollectionViewSource.GetDefaultView(Datasources) as ListCollectionView;
                DatasourcesView.CurrentChanged += (s, e) =>
                {
                    RaisePropertyChanged(() => SelectedDatasourceModel);
                };
                
                var dpList = new List<DatapointModel>();
                Datapoints = new ObservableCollection<DatapointModel>(dpList);
                DatapointsView = CollectionViewSource.GetDefaultView(Datapoints) as ListCollectionView;
                DatapointsView.CurrentChanged += (s, e) =>
                {
                    RaisePropertyChanged(() => SelectedDatapointModel);
                };
            }
        }
        
        private void DatapointsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            DispatcherHelper.RunAsync(() => DatapointsView.Refresh());
        }

        public string WindowTitle { get; private set; }

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

        private ObservableCollection<DatasourceModel> Datasources;
        public DatasourceModel SelectedDatasourceModel
        {
            get => DatasourcesView.CurrentItem as DatasourceModel;
            set
            {
                DatasourcesView.MoveCurrentTo(value);
                RaisePropertyChanged();
            }
        }
        
        public ICollectionView DatasourcesView { get; }

        #region Commands
        public RelayCommand ConnectCommand { get; private set; }

        public RelayCommand DisconnectCommand { get; private set; }

        public RelayCommand AddDatasourceCommand { get; private set; }
        public static RelayCommand AddDatasourceReturnKeyCommand { get; private set; }

        public RelayCommand NewDatapointCommand { get; private set; }
        public static RelayCommand NewDatapointReturnKeyCommand { get; private set; }

        public RelayCommand RestoreDefaultsCommand { get; private set; }

        public RelayCommand ClearOutputBoxContent { get; private set; }

        public RelayCommand WithTlsCommand { get; private set; }

        public RelayCommand EvaluateDatasourcesContextMenu { get; private set; }

        public RelayCommand EraseDatasourceCommand { get; private set; }

        private void OnConnectCommandExecuted()
        {
            SelectedDatasourceModel.StartClientAsync();
        }

        private bool OnConnectCommandCanExecute()
        {
            return true; // !SelectedDatasourceModel.IsConnectedToBroker;
        }

        private void OnDisconnectCommandExecuted()
        {
            SelectedDatasourceModel.StopClientAsync();
        }

        private bool OnDisconnectCommandCanExecute()
        {
            return true; // SelectedDatasourceModel.IsConnectedToBroker;
        }
        
        private void OnAddDatasourceExecuted()
        {
            var ds = new DatasourceModel(BrokerUri, WithTls);
            ds.OnMessageReceived += Ds_OnMessageReceived;
            ds.OnConnectionFailed += Ds_OnConnectionFailed;
            Datasources.Add(ds);
            BrokerUri = "";
        }
        
        private bool OnAddDatasourceCanExecute()
        {
            return true;
        }

        private void OnNewDatapointExecuted()
        {
            var dp = new DatapointModel(SelectedDatasourceModel.ClientId, NewDatapointName, String.Empty);
            dp.PropertyChanged += Dp_PropertyChanged;
            Datapoints.Add(dp);
            SelectedDatasourceModel.SubscribeToTopic(NewDatapointName);
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

        private void OnEvaluateDatasourcesContextMenuExecuted()
        {
            ConnectCommand.RaiseCanExecuteChanged();
            DisconnectCommand.RaiseCanExecuteChanged();
        }

        private void OnEraseDatasourceCommandExecuted()
        {
            // TODO
            //foreach (var item in Datapoints)
            //{
            //    if(item.ClientId == SelectedDatasourceModel.ClientId)
            //    {
            //        Datapoints.Remove(item); // will cause stress
            //    }
            //}
            ////unsubscribe ds
            //Datasources.RemoveAt(DatasourcesView.CurrentPosition);
        }
        #endregion

        private void Ds_OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            foreach(var item in Datapoints)
            {
                if(item.ClientId == e.Datasource && item.Identifier == e.Datapoint)
                {
                    item.Value = e.Message;
                    break;
                }
            }
        }

        private void Ds_OnConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            ApplicationMessages += e.Datasource + ": Connection failed." + Environment.NewLine;
        }

        private void Dp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            DispatcherHelper.RunAsync(() => DatapointsView.Refresh());
        }

        public void DoCleanup(DoCleanupMessage obj)
        {

        }

        public class DoCleanupMessage
        {

        }
    }
}
