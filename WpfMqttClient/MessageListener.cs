using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfMqttClient
{
    class MessageListener
    {
        public MessageListener()
        {

        }

        /// <summary>
        /// We need this property so that this type can be put into the resources.
        /// </summary>
        public bool BindableProperty => true;
    }
}
