using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using EchoServer;

namespace EchoServerTests
{
    public class EchoServerTests
    {
        //  Перевірка, що сервер стартує і зупиняється без винятків
        [Fact]
        public async Task Server_StartAndStop_ShouldNotThrow()
        {
            var server = new EchoServer.EchoServer(5003);
            var runTask = server.StartAsync();
            await Task.Delay(300);
            server.Stop();
            await runTask;
        }

        //  Перевірка ехо-відповіді
        [Fact]
        public async Task Server_ShouldEchoBackMessage()
        {
            int port = 5004;
            var server = new EchoServer.EchoServer(port);
            var serverTask = server.StartAsync();
            await Task.Delay(300);

            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port);
            using var stream = client.GetStream();

            var message = System.Text.Encoding.UTF8.GetBytes("ping");
            await stream.WriteAsync(message, 0, message.Length);

            var buffer = new byte[4];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            var response = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

            server.Stop();
            await serverTask;

            Assert.Equal("ping", response);
        }

        //  Перевірка повторного виклику Stop (щоб покрити "catch ObjectDisposedException")
        [Fact]
        public async Task Stop_CanBeCalledTwice_WithoutError()
        {
            var server = new EchoServer.EchoServer(5005);
            var runTask = server.StartAsync();
            await Task.Delay(300);
            server.Stop();
            server.Stop(); // друга спроба
            await runTask;
        }

        //  Перевірка, що HandleClient закінчує роботу при відключенні клієнта
        [Fact]
        public async Task HandleClient_ShouldEnd_WhenClientDisconnects()
        {
            int port = 5006;
            var server = new EchoServer.EchoServer(port);
            var serverTask = server.StartAsync();
            await Task.Delay(300);

            var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port);
            client.Close(); // клієнт одразу відключається

            server.Stop();
            await serverTask;
        }
    }
}
