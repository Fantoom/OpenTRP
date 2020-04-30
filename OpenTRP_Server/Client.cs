using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using NetCoreServer;
namespace TicTacToe_Server
{
	class Client
	{
		public Guid id { get; }
		public TcpSession session { get; }
		public Client(Guid id, Socket socket)
		{
			this.id = id;
			//this.socket = socket;
		}
		public Client(Guid id, TcpSession session)
		{
			this.id = id;
			this.session = session;
		}
	}
}
