using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EchoServer;
using Moq;
using NUnit.Framework;

namespace EchoServerTests
{
    [TestFixture]
    public class EchoServerSpecs
    {
        // ---------------------------
        // helper: очікування умови
        // ---------------------------
        private static async Task SpinWaitUntil(Func<bool> cond, TimeSpan timeout)
        {
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < timeout)
            {
                if (cond()) return;
                await Task.Delay(25);
            }
            Assert.Fail("Timed out waiting for condition.");
        }

        // ---------------------------
        // 1. Сервер стартує і зупиняється
        // ---------------------------
        [Test]
        public async Task Server_Starts_And_Stops_When_No_Clients()
        {
            var server = new EchoServer.EchoServer(50100);
            var runTask = Task.Run(() => server.StartAsync());

            await SpinWaitUntil(() => server.IsRunning, TimeSpan.FromSeconds(2));
            server.Stop();

            await Task.WhenAny(runTask, Task.Delay(1500));
            Assert.That(runTask.IsCompleted, Is.True);
        }

        // ---------------------------
        // 2. TCP ехо працює
        // ---------------------------
        [Test]
        public async Task Echo_Works_Correctly()
        {
            int port = 50101;
            var server = new EchoServer.EchoServer(port);

            var runTask = Task.Run(() => server.StartAsync());
            await SpinWaitUntil(() => server.IsRunning, TimeSpan.FromSeconds(2));

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            using var stream = client.GetStream();

            byte[] msg = System.Text.Encoding.UTF8.GetBytes("hello");
            await stream.WriteAsync(msg, 0, msg.Length);

            byte[] buf = new byte[16];
            int n = await stream.ReadAsync(buf, 0, buf.Length);

            string echoed = System.Text.Encoding.UTF8.GetString(buf, 0, n);

            server.Stop();
            await Task.WhenAny(runTask, Task.Delay(1500));

            Assert.That(echoed, Is.EqualTo("hello"));
        }

        // ---------------------------
        // 3. UdpTimedSender викликає Send
        // ---------------------------
        [Test]
        public void UdpTimedSender_Invokes_Send()
        {
            var udp = new Mock<IUdpClientLite>();
            udp.Setup(u => u.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()))
                .Returns((byte[] b, int i, IPEndPoint ep) => i);

            using var sender = new UdpTimedSender("127.0.0.1", 60000, udp.Object);

            sender.StartSending(10);
            Thread.Sleep(60);
            sender.StopSending();

            udp.Verify(u => u.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()),
                Times.AtLeastOnce);
        }

        // ---------------------------
        // 4. reflection викликає приватний callback
        // ---------------------------
        [Test]
        public void UdpTimedSender_PrivateCallback_CanBeCalled()
        {
            var udp = new Mock<IUdpClientLite>();
            udp.Setup(u => u.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()))
                .Returns((byte[] b, int i, IPEndPoint ep) => i);

            using var sender = new UdpTimedSender("127.0.0.1", 60000, udp.Object);

            MethodInfo mi = typeof(UdpTimedSender)
                .GetMethod("SendMessageCallback", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(mi, Is.Not.Null);

            mi!.Invoke(sender, new object[] { null });

            udp.Verify(u => u.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()),
                Times.AtLeastOnce);
        }

        // ---------------------------
        // 5. Main можна викликати (але не чекати завершення)
        // ---------------------------
        [Test]
        public async Task EchoServer_Main_CanBeInvoked_AndExits()
        {
            var main = typeof(EchoServer.EchoServer)
                .GetMethod("Main", BindingFlags.Public | BindingFlags.Static);

            Assert.That(main, Is.Not.Null);

            // викликаємо з параметром --exit
            var task = (Task)main!.Invoke(null, new object?[] { new[] { "--exit" } });

            // даємо Main час відпрацювати
            bool finished = task.Wait(1000);

            Assert.That(finished, Is.True, "Main did not exit in time");
        }

    }
}
