using System;
using System.Net;
using CommandLine;

namespace OpenTRP_Server
{
	static class Program
	{

		private static int port = -1;

		static void Main(string[] args)
		{
			Parser.Default.ParseArguments<Options>(args)
				  .WithParsed<Options>(o =>
				  {
					  port = o.port;
				  });

			if (port == 0)
			{
				Console.WriteLine("Enter Port");
				int.TryParse(Console.ReadLine(),out port);
			}
			
			PC_Server pC_Server = null;

			try
			{
				pC_Server = new PC_Server(IPAddress.Any, port);
				pC_Server.Start();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
			Console.WriteLine("Server Started");
			for (; ; )
			{
				string line = Console.ReadLine();
				if (string.IsNullOrEmpty(line))
					continue;
				if (line == "!")
				{
					break;
				}
			}

			// Stop the server
			Console.Write("Server stopping...");
			pC_Server?.Stop();
			Console.WriteLine("Done!");
		}

		public class Options
		{
			[Option('p', "port", Required = false, HelpText = "Set port")]
			public int port { get; set; }
		}
	}
}
