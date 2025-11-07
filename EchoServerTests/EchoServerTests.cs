using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using EchoServer;

namespace EchoServerTests
{
    public class EchoServerTests
    {
        [Fact]
        public async Task Server_StartAndStop_ShouldWork()
        {
            // 🔹 Перевіряє, що сервер можна запустити та зупинити без помилок
            var server = new EchoServer.EchoServer(6100);
            var task = server.StartAsync();

            await Task.Delay(300);
            server.Stop();
            await task;

            Assert.True(true); // Якщо без помилок — тест успішний
        }

        [Fact]
        public async Task Server_ShouldEchoMessage()
        {
            // 🔹 Перевіряє, що сервер повертає клієнту те саме повідомлення
            int port = 6101;
            var server = new EchoServer.EchoServer(port);
            var task = server.StartAsync();
            await Task.Delay(300);

            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port);
            using var stream = client.GetStream();

            var message = Encoding.UTF8.GetBytes("ping");
            await stream.WriteAsync(message, 0, message.Length);

            var buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            server.Stop();
            await task;

            Assert.Equal("ping", response);
        }

        [Fact]
        public async Task Server_ShouldHandleMultipleClients()
        {
            // 🔹 Перевіряє, що сервер може обробляти кілька клієнтів одночасно
            int port = 6102;
            var server = new EchoServer.EchoServer(port);
            var task = server.StartAsync();
            await Task.Delay(300);

            async Task<string> SendMessage(string msg)
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", port);
                using var stream = client.GetStream();

                byte[] data = Encoding.UTF8.GetBytes(msg);
                await stream.WriteAsync(data, 0, data.Length);

                byte[] buffer = new byte[1024];
                int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer, 0, read);
            }

            string r1 = await SendMessage("one");
            string r2 = await SendMessage("two");
            string r3 = await SendMessage("three");

            server.Stop();
            await task;

            Assert.Equal("one", r1);
            Assert.Equal("two", r2);
            Assert.Equal("three", r3);
        }

        [Fact]
        public void Stop_WithoutStart_ShouldNotThrow()
        {
            // 🔹 Перевіряє, що виклик Stop() без запуску сервера не спричиняє помилок
            var server = new EchoServer.EchoServer(6103);
            var ex = Record.Exception(() => server.Stop());
            Assert.Null(ex);
        }

        [Fact]
        public async Task Server_ShouldStopGracefully_WhenStopCalled()
        {
            // 🔹 Перевіряє, що сервер завершує роботу коректно після виклику Stop()
            var server = new EchoServer.EchoServer(6104);
            var task = server.StartAsync();
            await Task.Delay(300);

            server.Stop();
            var ex = await Record.ExceptionAsync(() => task);

            Assert.Null(ex);
        }

        [Fact]
        public async Task Server_ShouldNotEcho_WhenStopped()
        {
            // 🔹 Перевіряє, що після зупинки сервер більше не приймає підключення
            int port = 6105;
            var server = new EchoServer.EchoServer(port);
            var task = server.StartAsync();
            await Task.Delay(300);

            server.Stop();
            await task;

            await Assert.ThrowsAsync<SocketException>(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", port);
            });
        }
    }
}
