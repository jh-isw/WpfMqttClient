using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfMqttClient.Model
{
    class DatasourceModel : ModelBase
    {
        public DatasourceModel(string brokerUri, bool withTls)
        {
            BrokerUri = brokerUri;
            ClientId = Guid.NewGuid().ToString();
            IMqttClientOptions options;
            if (withTls)
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

            _managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(options)
                .Build();

            _client = new MqttFactory().CreateManagedMqttClient();
            _client.Connected += OnConnected;
            _client.Disconnected += OnDisconnected;
            _client.ConnectingFailed += OnConnectingFailed;
        }

        private IManagedMqttClient _client { get; }
        private ManagedMqttClientOptions _managedOptions;

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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }

        public async void StartClientAsync()
        {
            await _client.StartAsync(_managedOptions);
        }

        public async void StopClientAsync()
        {
            await _client.StopAsync();
        }

        private void OnConnected(object sender, MqttClientConnectedEventArgs e)
        {
            ConnectedToBroker = true;
        }

        private void OnDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            ConnectedToBroker = false;
        }

        private void OnConnectingFailed(object sender, MqttManagedProcessFailedEventArgs e)
        {
            ConnectedToBroker = true;
        }
    }
}
