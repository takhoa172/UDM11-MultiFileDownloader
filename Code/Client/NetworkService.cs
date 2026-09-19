using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Shared;

namespace Client
{
    public class NetworkService : IDisposable
    {
        private static int ConnectTimeoutMs => ClientConfig.Settings.Network.ConnectTimeoutMs;
        private static int ReadTimeoutMs => ClientConfig.Settings.Network.ReadTimeoutMs;
        private static int WriteTimeoutMs => ClientConfig.Settings.Network.WriteTimeoutMs;

        private TcpClient? _client;
        private NetworkStream? _stream;
        private StreamReader? _reader;

        public Socket? ClientSocket => _client?.Client;

        public async Task ConnectAsync(string ip, int port)
        {
            _client = new TcpClient();

            using CancellationTokenSource connectCts =
                new(TimeSpan.FromMilliseconds(ConnectTimeoutMs));

            await _client.ConnectAsync(
                ip,
                port,
                connectCts.Token);

            _stream = _client.GetStream();
            _reader = new StreamReader(_stream);
        }

        public async Task SendPacketAsync(
            ProtocolPacket packet,
            CancellationToken cancellationToken = default)
        {
            if (_stream == null)
                throw new InvalidOperationException("Chưa kết nối Server.");

            byte[] data = PacketHelper.Encode(packet);

            using CancellationTokenSource writeCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            writeCts.CancelAfter(WriteTimeoutMs);

            await _stream.WriteAsync(data.AsMemory(), writeCts.Token);
            await _stream.FlushAsync(writeCts.Token);
        }

        public async Task<ProtocolPacket> ReadPacketAsync(
            CancellationToken cancellationToken = default)
        {
            if (_reader == null)
                throw new InvalidOperationException("Chưa kết nối Server.");

            using CancellationTokenSource readCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            readCts.CancelAfter(ReadTimeoutMs);

            string? line = await _reader.ReadLineAsync(readCts.Token);

            if (line == null)
            {
                throw new IOException("Server đã đóng kết nối.");
            }

            return PacketHelper.Decode(line);
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _stream?.Dispose();
            _client?.Close();
            _client?.Dispose();

            _reader = null;
            _stream = null;
            _client = null;
        }
    }
}