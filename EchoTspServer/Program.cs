using System;
using System.Threading.Tasks;

namespace EchoServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var server = new EchoServer(5000);
            _ = Task.Run(() => server.StartAsync());

            Console.WriteLine("Press 'q' to quit...");
            while (Console.ReadKey(true).Key != ConsoleKey.Q) { }

            server.Stop();
        }
    }
}