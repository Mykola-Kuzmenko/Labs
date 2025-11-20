using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using EchoServer;

namespace EchoServerTests
{
    public class EchoServerTests
    {
        private const int DelayBeforeConnect = 200;

        [Test]
        public async Task Server_StartAndStop_ShouldWork()
        {
            var server = new EchoServer.EchoServer(6100);
            var serverTask = server.StartAsync();

            await Task.Delay(DelayBeforeConnect);

            Assert.That(server.IsRunning, Is.True);

            server.Stop();
            await serverTask;

            Assert.That(server.IsRunning, Is.False);
        }

        [Test]
        public async Task Server_ShouldEchoMessage()
        {
            int port = 6101;
            var server = new EchoServer.EchoServer(port);
            var task = server.StartAsync();

            await Task.Delay(DelayBeforeConnect);

            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port);
            using var stream = client.GetStream();

            byte[] message = Encoding.UTF8.GetBytes("ping");
            await stream.WriteAsync(message);

            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer);

            server.Stop();
            await task;

            Assert.That(Encoding.UTF8.GetString(buffer, 0, bytesRead), Is.EqualTo("ping"));
        }

        [Test]
        public async Task Server_ShouldHandleMultipleClients()
        {
            int port = 6102;
            var server = new EchoServer.EchoServer(port);
            var task = server.StartAsync();

            await Task.Delay(DelayBeforeConnect);

            async Task<string> Send(string msg)
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", port);

                using var stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(msg);
                await stream.WriteAsync(data);

                byte[] buffer = new byte[1024];
                int read = await stream.ReadAsync(buffer);

                return Encoding.UTF8.GetString(buffer, 0, read);
            }

            var r1 = await Send("one");
            var r2 = await Send("two");
            var r3 = await Send("three");

            server.Stop();
            await task;

            Assert.That(r1, Is.EqualTo("one"));
            Assert.That(r2, Is.EqualTo("two"));
            Assert.That(r3, Is.EqualTo("three"));
        }

        [Test]
        public async Task Server_ShouldStopGracefully_WhenStopCalled()
        {
            var server = new EchoServer.EchoServer(6104);
            var task = server.StartAsync();

            await Task.Delay(DelayBeforeConnect);

            Assert.DoesNotThrow(() => server.Stop());
            Assert.DoesNotThrowAsync(async () => await task);
        }

        [Test]
        public async Task Server_ShouldNotEcho_WhenStopped()
        {
            int port = 6105;
            var server = new EchoServer.EchoServer(port);
            var task = server.StartAsync();

            await Task.Delay(DelayBeforeConnect);

            server.Stop();
            await task;

            Assert.ThrowsAsync<SocketException>(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", port);
            });
        }

        [Test]
        public async Task Server_ClientShouldDisconnect_WhenCanceled()
        {
            int port = 6106;
            var server = new EchoServer.EchoServer(port);
            var serverTask = server.StartAsync();

            await Task.Delay(DelayBeforeConnect);

            var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port);

            var stream = client.GetStream();
            await stream.WriteAsync(Encoding.UTF8.GetBytes("hello"));

            server.Stop();
            await serverTask;

            Assert.That(async () =>
            {
                await stream.ReadAsync(new byte[1024]);
            }, Throws.Exception);
        }
    }
}
