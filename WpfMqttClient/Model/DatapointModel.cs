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
            ClientId = clientId;
            Identifier = identifier;
            Value = value;
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
#if DEBUG
                Console.WriteLine("clientId :" + value);
#endif
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
#if DEBUG
                Console.WriteLine("identifier :" + value);
#endif
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
#if DEBUG
                Console.WriteLine("value: " + value);
#endif
                OnPropertyChanged();
            }
        }

        public override string ToString()
        {
            return ClientId + ", " + Identifier + ", " + Value;
        }
    }
}
