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
            Assert.True(runTask.IsCompletedSuccessfully, "Server did not complete successfully.");
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
            var memory = new ReadOnlyMemory<byte>(message);
            await stream.WriteAsync(memory, CancellationToken.None);  // Використовуємо CancellationToken.None
            var buffer = new Memory<byte>(new byte[4]);  // Використовуємо Memory<byte> замість byte[]
            int bytesRead = await stream.ReadAsync(buffer, CancellationToken.None);  // Використовуємо ReadAsync з Memory<byte>
            var response = System.Text.Encoding.UTF8.GetString(buffer.Span.Slice(0, bytesRead));
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
            server.Stop(); 
            await runTask;
            Assert.True(runTask.IsCompletedSuccessfully, "Server did not stop properly without errors.");
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
            Assert.True(serverTask.IsCompletedSuccessfully, "Server did not complete successfully after client disconnected.");
        }
    }
}
