using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NetCoreServer;
using System.Linq;
using MessagePack;

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
                var dataToSend = MessagePackSerializer.Serialize<Package>(new Package(id, data));
                ProxyClientSession.SendAsync(dataToSend);
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

            onDataRecived(Id.ToString(), message);

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
