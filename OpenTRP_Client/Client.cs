using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MessagePack;
using TcpClient = NetCoreServer.TcpClient;

namespace OpenTRP_Client
{
    public class Client : TcpClient
    {
        public delegate void OnDataRecived(string id , string data);

        public event OnDataRecived onDataRecived;

        public string ExternalId { get; private set; }

        public Client(string address, int port, string externalId) : base(address, port) { ExternalId = externalId; }

        public void DisconnectAndStop()
        {
            _stop = true;
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"client connected a new session with Id {Id}");
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"client disconnected a session with Id {Id}");

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                ConnectAsync();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {

            string data = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            onDataRecived(ExternalId, data);


            Console.WriteLine(data);
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"client caught an error with code {error}");
        }

        private bool _stop;
    }
}
