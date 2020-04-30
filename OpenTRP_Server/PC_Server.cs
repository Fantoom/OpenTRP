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
	class PC_Server : TcpServer
    {

        private Dictionary<string,Server> proxy_clients = new Dictionary<string,Server>();

        public static Server instance { get; private set; }

        public PC_Server(IPAddress address, int port) : base(address, port) { }

        protected override TcpSession CreateSession() { return new ProxyClientSession(this); }

        protected override void OnConnected(TcpSession session)
        {
            var server = new Server(System.Net.IPAddress.Any, FreeTcpPort(), (ProxyClientSession)session);
           // var server = new Server(System.Net.IPAddress.Any, 0);
            session.SendAsync($"CONNECT:{server.Endpoint.Port.ToString()}");
            ((ProxyClientSession)session).onDataRecivedByte += onDataRecived;
            server.Start();
            proxy_clients.Add(session.Id.ToString(), server);
        }

        private void onDataRecived(string id, byte[] buffer)
        {
            var options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);
            Package package = new Package("err","err");
            try
            {
                package = MessagePackSerializer.Deserialize<Package>(buffer, options);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message); 
            }
           
            proxy_clients[id].SendToClient(package);
        }

        protected override void OnDisconnected(TcpSession session)
        {
            // base.OnDisconnected(session);
            proxy_clients[session.Id.ToString()].Stop();
            proxy_clients.Remove(session.Id.ToString());
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"ProxyServer TCP caught an error with code {error}");
        }

        private static int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }

    class ProxyClientSession : TcpSession
    {
        public delegate void OnDataRecivedStr(string id, string data);
        public event OnDataRecivedStr onDataRecivedStr = delegate { };

        public delegate void OnDataRecivedByte(string id, byte[] buffer);
        public event OnDataRecivedByte onDataRecivedByte = delegate { };

        public ProxyClientSession(TcpServer server) : base(server) 
        {

        }
        
        protected override void OnConnected()
        {
            Console.WriteLine($"ProxyClient session with Id {Id} connected!");
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"ProxyClient session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            Console.WriteLine("Incoming: " + message);

            onDataRecivedByte(Id.ToString(),buffer);
            onDataRecivedStr(Id.ToString(),message);

            // If the buffer starts with '!' the disconnect the current session
            if (message == "!")
                Disconnect();
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"ProxyClient session caught an error with code {error}");
        }
    }

}
