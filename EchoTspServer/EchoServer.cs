using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer
{
    public interface ILogger
    {
        void Log(string message);
    }

    public class ConsoleLogger : ILogger
    {
        public void Log(string message) => Console.WriteLine(message);
    }

    public class EchoServer
    {
        private readonly int _port;
        private readonly ILogger _logger;
        private TcpListener _listener;
        private readonly CancellationTokenSource _cts;

        public EchoServer(int port, ILogger logger = null)
        {
            _port = port;
            _logger = logger ?? new ConsoleLogger();
            _cts = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _logger.Log($"Server started on port {_port}");

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client, _cts.Token));
                }
                catch (ObjectDisposedException)
                {
                    // Виникає, коли listener зупиняється — ігноруємо
                    break;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
                {
                    // Виникає при Stop() — ігноруємо
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log($"Unexpected error: {ex.Message}");
                }
            }

            _logger.Log("Server stopped.");
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using var stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                while (!token.IsCancellationRequested &&
                       (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, bytesRead, token);
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Client handler error: {ex.Message}");
            }
            finally
            {
                client.Close();
                _logger.Log("Client disconnected.");
            }
        }

        public void Stop()
        {
            try
            {
                _cts.Cancel();
                _listener?.Stop();
            }
            catch (Exception ex)
            {
                _logger.Log($"Error stopping server: {ex.Message}");
            }
            finally
            {
                _cts.Dispose();
            }
        }
    }
}
