using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;
namespace OpenTRP_Client
{
	[MessagePackObject]
	public class Package
	{

		[Key(0)]
		public string Id { get; set; }
		[Key(1)]
		public string Data { get; set; }

		public Package()
		{

		}

		public Package(string id, string data)
		{
			Id = id;
			Data = data;
		}
	}
}
