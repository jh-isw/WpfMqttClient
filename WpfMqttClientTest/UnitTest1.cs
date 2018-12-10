using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using WpfMqttClient.ViewModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace WpfMqttClientTest
{
    [TestClass]
    public class UnitTest1
    {
        private static MainViewModel testVM;
        private static RelayCommand ConnectDisconnectCommand;
        private static RelayCommand NewDatapointCommand;

        [ClassInitialize]
        public static void Initialize(TestContext tc)
        {
            testVM = new MainViewModel();
            ConnectDisconnectCommand = testVM.ConnectDisconnectCommand;
            NewDatapointCommand = testVM.NewDatapointCommand;
        }

        [TestMethod]
        public void TestConnectingDisconnectButtonText()
        {
            Assert.AreEqual(testVM.ConnectDisconnectButtonText, "Connect");
            testVM.BrokerUri = "iot.eclipse.org";
            ConnectDisconnectCommand.Execute(this);
            Assert.AreEqual(testVM.ConnectDisconnectButtonText, "Disconnect");
            ConnectDisconnectCommand.Execute(this);
            Assert.AreEqual(testVM.ConnectDisconnectButtonText, "Connect");
        }

        //[TestMethod]
        //public void TestSubscribing()
        //{
        //    testVM.BrokerUri = "iot.eclipse.org";
        //    ConnectDisconnectCommand.Execute(this); //Connect
        //    testVM.NewDatapointName = "$SYS/broker/uptime";
        //    testVM.ApplicationMessages = String.Empty;
        //    NewDatapointCommand.Execute(this);
        //    StringAssert.Contains(testVM.ApplicationMessages, "seconds");
        //    ConnectDisconnectCommand.Execute(this); //Disconnect
        //}

        [ClassCleanup]
        public static void Cleanup()
        {
            testVM.DoCleanup(new MainViewModel.DoCleanupMessage());
        }
    }
}
