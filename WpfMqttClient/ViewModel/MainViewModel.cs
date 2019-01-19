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
                Messenger.Default.Register<DoCleanupMessage>(this, DoCleanup);

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

                EraseDatasourceCommand = new RelayCommand(OnEraseDatasourceCommandExecuted, OnEraseDatasourceCommandCanExecute);

                RemoveDatapointCommand = new RelayCommand(OnRemoveDatapointCommandExecuted, null);

                UnsubscribeCommand = new RelayCommand<object>(OnUnsubscribeCommandExecuted, null);
                
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

        private readonly object _onMessageReceivedLock = new object();

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

        public RelayCommand RemoveDatapointCommand { get; private set; }

        public RelayCommand<object> UnsubscribeCommand { get; private set; }

        private void OnConnectCommandExecuted()
        {
            SelectedDatasourceModel.StartClientAsync();
        }

        private bool OnConnectCommandCanExecute()
        {
            return SelectedDatasourceModel == null ? false : true;  // true; // !SelectedDatasourceModel.IsConnectedToBroker;
        }

        private void OnDisconnectCommandExecuted()
        {
            SelectedDatasourceModel.StopClientAsync();
        }

        private bool OnDisconnectCommandCanExecute()
        {
            return SelectedDatasourceModel == null ? false : true;  // true; // SelectedDatasourceModel.IsConnectedToBroker;
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
            //var dp = new DatapointModel(SelectedDatasourceModel.ClientId, NewDatapointName, string.Empty);
            //dp.PropertyChanged += Dp_PropertyChanged;
            //Datapoints.Add(dp);
            if(NewDatapointName != String.Empty)
            {
                SelectedDatasourceModel.SubscribeToTopic(NewDatapointName);
            }
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
            ApplicationMessages += WithTls? "TLS eingeschaltet\n" : "TLS ausgeschaltet\n";
        }

        private void OnEvaluateDatasourcesContextMenuExecuted()
        {
            ConnectCommand.RaiseCanExecuteChanged();
            DisconnectCommand.RaiseCanExecuteChanged();
            EraseDatasourceCommand.RaiseCanExecuteChanged();
        }

        private void OnEraseDatasourceCommandExecuted()
        {
            if (SelectedDatasourceModel.IsConnectedToBroker)
            {
                SelectedDatasourceModel.StopClientAsync();
            }
            
            //foreach (var item in Datapoints)
            //{
            //    if(item.ClientId == SelectedDatasourceModel.ClientId)
            //    {
            //        Datapoints.Remove(item);
            //    }
            //}

            Datasources.RemoveAt(DatasourcesView.CurrentPosition);
        }

        private bool OnEraseDatasourceCommandCanExecute()
        {
            return SelectedDatasourceModel == null ? false : true;  // true; // ;
        }

        private void OnRemoveDatapointCommandExecuted()
        {
            OnUnsubscribeCommandExecuted(SelectedDatapointModel.Identifier);

            try
            {
                Datapoints.Remove(SelectedDatapointModel);
            }
            catch (Exception e)
            {
                ApplicationMessages += e.Message;
                throw;
            }
        }

        private void OnUnsubscribeCommandExecuted(object obj)
        {
            var topic = obj as string;
            if(topic != null && topic != String.Empty)
            {
                SelectedDatasourceModel.Unsubscribe(topic);
            }
        }
        #endregion

        private void Ds_OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //ApplicationMessages += "Ds_OnMessageReceived, " + e.Datapoint + ", " + e.Message + "\n";
            lock (_onMessageReceivedLock)
            {
                bool found = false;
                foreach (var item in Datapoints)
                {
                    if (item.ClientId == e.Datasource && item.Identifier == e.Datapoint)
                    {
                        item.Value = e.Message;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    var dp = new DatapointModel(e.Datasource, e.Datapoint, e.Message);
                    dp.PropertyChanged += Dp_PropertyChanged;
                    DispatcherHelper.RunAsync(() => Datapoints.Add(dp));
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
            foreach(DatasourceModel ds in Datasources)
            {
                ds.StopClientAsync();
                ds.Dispose();
            }
        }

        public class DoCleanupMessage
        {

        }
    }
}
