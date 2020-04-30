using System;
using System.Net;
using CommandLine;

namespace OpenTRP_Client
{
	static class Program
	{

		private static string proxyServer_ip = "";
		private static int proxyServer_port = -1;
		private static string server_ip = "";
		private static int server_port = -1;

		static void Main(string[] args)
		{

			Parser.Default.ParseArguments<Options>(args)
				  .WithParsed<Options>(o =>
				  {
					  proxyServer_ip = o.ProxyServerIp;
					  proxyServer_port = o.ProxyServerPort;
					  server_ip = o.ServerIp;
					  server_port = o.ServerPort;
				  });
			
			if (string.IsNullOrEmpty(proxyServer_ip))
			{
				Console.WriteLine("Enter Proxy Server IP");
				proxyServer_ip = Console.ReadLine();
			}
			if (proxyServer_port == 0)
			{
				Console.WriteLine("Enter Proxy Server Port");
				int.TryParse(Console.ReadLine(), out proxyServer_port);
			}
			if (string.IsNullOrEmpty(server_ip))
			{
				Console.WriteLine("Enter Server IP");
				server_ip = Console.ReadLine();
			}
			if (server_port == 0)
			{
				Console.WriteLine("Enter Server Port");
				int.TryParse(Console.ReadLine(), out server_port);
			}
			

			ProxyClient proxyClient = null;

			try
			{
				proxyClient = new ProxyClient(proxyServer_ip,proxyServer_port,server_ip,server_port);
				proxyClient.ConnectAsync();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
			Console.WriteLine("Started");
			for (; ; )
			{
				string line = Console.ReadLine();
				if (string.IsNullOrEmpty(line))
					continue;

				// Disconnect the client
				if (line == "!")
				{
					Console.Write("Client disconnecting...");
					proxyClient?.DisconnectAsync();
					Console.WriteLine("Done!");
					break;
				}
			}
		}

		public class Options
		{
			[Option("ProxyServerIp", Required = false, HelpText = "Set server IP")]
			public string ProxyServerIp { get; set; }
			[Option("ProxyServerPort", Required = false, HelpText = "Set server port")]
			public int ProxyServerPort { get; set; }
			[Option("ServerIp", Required = false, HelpText = "Set client IP")]
			public string ServerIp { get; set; }
			[Option("ServerPort", Required = false, HelpText = "Set client port")]
			public int ServerPort { get; set; }
		}
	}
}
