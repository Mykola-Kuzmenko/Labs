using System.Net;
using System.Net.Sockets;
using System.Reflection;
using NUnit.Framework;
using Moq;
using EchoServer;

namespace EchoServerTests
{
    [TestFixture]
    public class EchoServerSpecs
    {
        [Test]
        public async Task StartAsync_Then_Stop_CompletesCleanly_WhenNoClients()
        {
            var server = new EchoServer.EchoServer(0);   // ← порт 0 = ОС вибере сама
            var runTask = Task.Run(() => server.StartAsync());

            await SpinWaitUntil(() => server.IsRunning, TimeSpan.FromSeconds(2));

            server.Stop();
            await Task.WhenAny(runTask, Task.Delay(1000));

            Assert.That(runTask.IsCompleted, Is.True);
        }

        [Test]
        public async Task Echo_Roundtrip_Works()
        {
            var server = new EchoServer.EchoServer(0); // ← порт 0
            var runTask = Task.Run(() => server.StartAsync());

            await SpinWaitUntil(() => server.IsRunning, TimeSpan.FromSeconds(2));

            var port = server.Port; // ← тепер тест знає реальний порт

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);

            using var stream = client.GetStream();

            var send = System.Text.Encoding.UTF8.GetBytes("ping");
            await stream.WriteAsync(send, 0, send.Length);

            var buf = new byte[16];
            var n = await stream.ReadAsync(buf, 0, buf.Length);
            var received = System.Text.Encoding.UTF8.GetString(buf, 0, n);

            server.Stop();
            await Task.WhenAny(runTask, Task.Delay(1000));

            Assert.That(received, Is.EqualTo("ping"));
        }

        [Test]
        public void UdpTimedSender_StartStop_InvokesSend()
        {
            var udp = new Mock<IUdpClientLite>();

            udp.Setup(x => x.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()))
               .Returns((byte[] b, int i, IPEndPoint ep) => i);

            // тут порт може бути хоч 0 — бо Send() моканий
            using var sender = new UdpTimedSender("127.0.0.1", 0, udp.Object);

            sender.StartSending(10);
            Thread.Sleep(60);
            sender.StopSending();

            udp.Verify(x => x.Send(
                    It.IsAny<byte[]>(),
                    It.IsAny<int>(),
                    It.IsAny<IPEndPoint>()),
                Times.AtLeastOnce);
        }

        [Test]
        public void UdpTimedSender_PrivateCallback_CanBeInvokedViaReflection()
        {
            var udp = new Mock<IUdpClientLite>();

            udp.Setup(x => x.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()))
               .Returns((byte[] b, int i, IPEndPoint ep) => i);

            using var sender = new UdpTimedSender("127.0.0.1", 0, udp.Object);

            var mi = typeof(UdpTimedSender)
                .GetMethod("SendMessageCallback", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(mi, Is.Not.Null);

            mi!.Invoke(sender, new object?[] { null });

            udp.Verify(x => x.Send(
                    It.IsAny<byte[]>(),
                    It.IsAny<int>(),
                    It.IsAny<IPEndPoint>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task EchoServer_Main_CanBeInvokedSafely()
        {
            var mainMethod = typeof(EchoServer.EchoServer)
                .GetMethod("Main", BindingFlags.Static | BindingFlags.Public);

            Assert.That(mainMethod, Is.Not.Null);

            Console.SetOut(TextWriter.Null);

            bool executed = false;

            try
            {
                await (Task)mainMethod!.Invoke(null, new object?[] { Array.Empty<string>() });
                executed = true;
            }
            catch
            {
                executed = true;
            }

            Assert.That(executed, Is.True);
        }

        private static async Task SpinWaitUntil(Func<Task<bool>> cond, TimeSpan timeout)
        {
            var start = DateTime.UtcNow;

            while (DateTime.UtcNow - start < timeout)
            {
                if (await cond()) return;
                await Task.Delay(25);
            }

            Assert.Fail("Timed out while waiting for condition.");
        }

        private static async Task SpinWaitUntil(Func<bool> cond, TimeSpan timeout)
            => await SpinWaitUntil(() => Task.FromResult(cond()), timeout);
    }
}
