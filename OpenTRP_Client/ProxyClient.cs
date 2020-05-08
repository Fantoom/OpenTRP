using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MessagePack;
using Newtonsoft.Json;
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
        private int incomingDataSize = -1;
        private int pendingDataLeft { get { return incomingDataSize - pendingBuffer.Count; } }
        private List<byte> pendingBuffer = new List<byte>();

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

            byte[] usefulBuffer = new byte[(int)size];

            Array.Copy(buffer, (int)offset, usefulBuffer, 0, (int)size);

            byte[] cleanBuffer = new byte[(int)size];

            Array.Copy(usefulBuffer, 5, cleanBuffer, 0, (int)size-5);
            if(incomingDataSize  == -1) 
            { 
            incomingDataSize = BitConverter.ToInt32(buffer.Take(4).ToArray(), 0);
            }
            pendingBuffer.AddRange(cleanBuffer);
            if (incomingDataSize == pendingBuffer.Count)
            {
            var options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);
            Package package = new Package("err", "err");
            try
            {
                  package = MessagePackSerializer.Deserialize<Package>(pendingBuffer.ToArray(), options);
                  //package = JsonConvert.DeserializeObject<Package>(data);
                  clients[package.Id].SendAsync(package.Data);
                  CleanBuffer();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            }

            Console.WriteLine(data);
        }
        void CleanBuffer()
        {
            pendingBuffer.Clear();
            incomingDataSize = -1;

        }
        void OnClientDataRecived(string externalId, string RawData)
        {

            /*for (int i = 0; i < RawData.Length; i += OptionSendBufferSize)
            {
                var data = RawData.Substring(i, Math.Min(OptionSendBufferSize, RawData.Length - i));
                var package = new Package(externalId, data);
                //var dataToSend = MessagePackSerializer.Serialize(package);
                var dataToSend = JsonConvert.SerializeObject(package);
                SendAsync(dataToSend);
            }*/

            /*int maxChunkSize = OptionSendBufferSize - 42;
            for (int i = 0; i < RawData.Length; i += maxChunkSize)
            {
                var chunkofString = RawData.Substring(i, Math.Min(maxChunkSize, RawData.Length - i));
                var package = new Package(externalId, chunkofString);
                var dataToSend = MessagePackSerializer.Serialize(package);

                SendAsync(dataToSend);                
                //Thread.Sleep(10);
            }*/
            var package = new Package(externalId, RawData);

            var serializedPackage = MessagePackSerializer.Serialize(package);
            var dataToSend = new List<byte>(BitConverter.GetBytes(serializedPackage.Length));
            dataToSend.AddRange(serializedPackage);
            SendAsync(dataToSend.ToArray());


            //var dataToSend = JsonConvert.SerializeObject(package);
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"ProxyClien caught an error with code {error}");
        }

        private bool _stop;

        static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }
    }
}
