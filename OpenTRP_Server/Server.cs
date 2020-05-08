using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NetCoreServer;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using System.Threading;

namespace OpenTRP_Server
{
	class Server : TcpServer
    {

        private Dictionary<string,ClientSession> clients = new Dictionary<string, ClientSession>();

        public ProxyClientSession ProxyClientSession { get; private set; }

        public static Server instance { get; private set; }

        public Server(IPAddress address, int port) : base(address, port) { }

        public Server(IPAddress address, int port, ProxyClientSession PClient_Session) : base(address, port) { ProxyClientSession = PClient_Session; }

        public void SendToClient(Package data)
        {
            clients[data.Id].SendAsync(data.Data);
        }

        protected override TcpSession CreateSession() { return new ClientSession(this); }

        protected override void OnConnected(TcpSession session)
        {
            ((ClientSession)session).onDataRecived += OnDataRecived;
            clients.Add(session.Id.ToString(),(ClientSession)session);
            var dataToSend = $"NEWCON:{session.Id.ToString()}";
            ProxyClientSession.SendAsync(dataToSend);
        }

        protected override void OnDisconnected(TcpSession session)
        {
            // base.OnDisconnected(session);
            clients.Remove(session.Id.ToString());
        }

        private void OnDataRecived(string id, string data) 
        {
            try
            {

                var serializedPackage = MessagePackSerializer.Serialize<Package>(new Package(id, data));
                var dataToSend = new List<byte>(BitConverter.GetBytes(serializedPackage.Length));
                dataToSend.AddRange(serializedPackage);

                //var dataToSend = JsonConvert.SerializeObject(new Package(id, data));
                ProxyClientSession.SendAsync(dataToSend.ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
           
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Server TCP server caught an error with code {error}");
        }


    }

    class ClientSession : TcpSession
    {

        public delegate void OnDataRecived(string id, string data);
        public event OnDataRecived onDataRecived;


        public ClientSession(TcpServer server) : base(server) 
        {
            
        }
        
        protected override void OnConnected()
        {
            Console.WriteLine($"Client session with Id {Id} connected!");
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Client session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            Console.WriteLine("Incoming: " + message);
            int maxChunkSize = OptionSendBufferSize - 42;
            for (int i = 0; i < message.Length; i += maxChunkSize)
            {
                onDataRecived(Id.ToString(), message.Substring(i, Math.Min(maxChunkSize, message.Length - i)));
               // Thread.Sleep(10);

            }
            // If the buffer starts with '!' the disconnect the current session
            if (message == "!")
                Disconnect();
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Client session caught an error with code {error}");
        }
    }

}
