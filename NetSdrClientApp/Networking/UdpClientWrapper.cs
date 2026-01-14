using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class UdpClientWrapper : IUdpClient, IDisposable
    {
        private readonly IPEndPoint _localEndPoint;
        private CancellationTokenSource? _cts;
        private UdpClient? _udpClient;

        public event EventHandler<byte[]>? MessageReceived;

        public UdpClientWrapper(int port)
        {
            _localEndPoint = new IPEndPoint(IPAddress.Any, port);
        }

        public async Task StartListeningAsync()
        {

            _cts = new CancellationTokenSource();
            Console.WriteLine("UDP Listening started.");
            
            try 
            {
                _udpClient = new UdpClient(_localEndPoint);
                await ReceiveLoopAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UDP Init Error: {ex.Message}");
            }
        }


        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _udpClient != null)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync(token);
                    MessageReceived?.Invoke(this, result.Buffer);
                    Console.WriteLine($"UDP Packet from {result.RemoteEndPoint}");
                }
                catch (OperationCanceledException)
                {
                    break; 
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"UDP Receive Error: {ex.Message}");
                }
            }
        }

        public void StopListening()
        {
            SafeDispose();
            Console.WriteLine("Stopped listening for UDP.");
        }

        public void Exit()
        {
            SafeDispose();
        }

        public void Dispose()
        {
            SafeDispose();
            GC.SuppressFinalize(this);
        }


        private void SafeDispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
        }

        public override int GetHashCode()
        {
            var payload = $"{nameof(UdpClientWrapper)}|{_localEndPoint}";
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return BitConverter.ToInt32(hash, 0);
        }

        public override bool Equals(object? obj)
        {
            return obj is UdpClientWrapper other && 
                   _localEndPoint.ToString() == other._localEndPoint.ToString();
        }
    }    
}
