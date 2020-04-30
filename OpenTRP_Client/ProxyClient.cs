using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MessagePack;
using TcpClient = NetCoreServer.TcpClient;

namespace OpenTRP_Client
{
    public class ProxyClient : TcpClient
    {
        public delegate void OnDataRecived(string data);

        public event OnDataRecived onDataRecived;

        public Dictionary<string, Client> clients = new Dictionary<string, Client>();

        public string Server_address { get; private set; }
        public int Server_port { get; private set; }


        public ProxyClient(string ProxyServer_address, int ProxyServer_port, string Server_address, int Server_port) : base(ProxyServer_address, ProxyServer_port) 
        {
            this.Server_address = Server_address;
            this.Server_port = Server_port;
        }

        public void DisconnectAndStop()
        {
            _stop = true;
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"ProxyClient connected a new session with Id {Id}");
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"ProxyClient disconnected a session with Id {Id}");

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                ConnectAsync();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string data = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            Console.WriteLine(data);

            if (onDataRecived != null)
            onDataRecived(data);

            if(data.Contains("CONNECT"))
            {
                Console.WriteLine($"Server gave you the port {data.Split(':')[1]}");
                return;
            }
            if (data.Contains("NEWCON"))
            {
                string id = data.Split(':')[1];
                Console.WriteLine($"New Client connected to proxy server ID:{id}");

                Client client = new Client(Server_address, Server_port, id);
                client.onDataRecived += OnClientDataRecived;
                client.ConnectAsync();
                clients.Add(id, client);
                
                return;
            }

            var options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);
            Package package = new Package("err", "err");
            try
            {
                package = MessagePackSerializer.Deserialize<Package>(buffer, options);
                clients[package.Id].SendAsync(package.Data);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            Console.WriteLine(data);
        }

        void OnClientDataRecived(string externalId, string data)
        {
            var package = new Package(externalId, data);
            var dataToSend = MessagePackSerializer.Serialize(package);

            SendAsync(dataToSend);
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"ProxyClien caught an error with code {error}");
        }

        private bool _stop;
    }
}
