using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfMqttClient.Model
{
    public class DatapointModel : ModelBase
    {
        public DatapointModel(string clientId, string identifier, string value)
        {
            ClientTime = DateTime.Now;
            ClientId = clientId;
            Identifier = identifier;
            Value = value;
        }

        private DateTime _clientTime;
        public DateTime ClientTime
        {
            get
            {
                return _clientTime;
            }
            set
            {
                if (value == _clientTime)
                {
                    return;
                }
                _clientTime = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }

        private string _identifier;
        public string Identifier
        {
            get
            {
                return _identifier;
            }
            set
            {
                if (value == _identifier)
                {
                    return;
                }
                _identifier = value;
                OnPropertyChanged();
            }
        }

        private string _value;
        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (value == _value)
                {
                    return;
                }
                _value = value;
                ClientTime = DateTime.Now;
                OnPropertyChanged();
            }
        }

        public override string ToString()
        {
            return ClientTime.ToString() + ", " + ClientId + ", " + Identifier + ", " + Value;
        }
    }
}
