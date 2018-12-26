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
    public class DatasourceModel : ModelBase
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
            _client.Connected += _client_OnConnected;
            _client.Disconnected += _client_OnDisconnected;
            _client.ConnectingFailed += _client_OnConnectingFailed;
            _client.ApplicationMessageReceived += _client_ApplicationMessageReceived;
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
        public bool IsConnectedToBroker
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

        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;
        public event EventHandler<ConnectionFailedEventArgs> OnConnectionFailed;

        public async void StartClientAsync()
        {
            await _client.StartAsync(_managedOptions);
        }

        public async void StopClientAsync()
        {
            await _client.StopAsync();
        }

        public async void SubscribeToTopic(string topicName)
        {
            await _client.SubscribeAsync(new TopicFilterBuilder().WithTopic(topicName).Build());
        }

        private void _client_OnConnected(object sender, MqttClientConnectedEventArgs e)
        {
            IsConnectedToBroker = true;
        }

        private void _client_OnDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            IsConnectedToBroker = false;
        }

        private void _client_OnConnectingFailed(object sender, MqttManagedProcessFailedEventArgs e)
        {
            IsConnectedToBroker = false;
            OnConnectionFailed(this, new ConnectionFailedEventArgs(ClientId));
        }

        private void _client_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            OnMessageReceived(this,
                new MessageReceivedEventArgs(ClientId, e.ApplicationMessage.Topic,
                    Encoding.Default.GetString(e.ApplicationMessage.Payload)));
        }
    }
    
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(string datasource, string datapoint, string message)
        {
            Datasource = datasource;
            Datapoint = datapoint;
            Message = message;
        }

        public string Datasource { get; set; }
        public string Datapoint { get; set; }
        public string Message { get; set; }
    }

    public class ConnectionFailedEventArgs
    {
        public ConnectionFailedEventArgs(string datasource)
        {
            Datasource = datasource;
        }

        public string Datasource { get; set; }
    }
}
