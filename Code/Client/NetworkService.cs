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
        private readonly SemaphoreSlim _requestLock = new(1, 1);

        public Socket? ClientSocket => _client?.Client;

        public async Task ConnectAsync(string ip, int port)
        {
            DisposeConnection();

            TcpClient client = new();
            try
            {
                using CancellationTokenSource connectCts =
                    new(TimeSpan.FromMilliseconds(ConnectTimeoutMs));

                await client.ConnectAsync(ip, port, connectCts.Token);

                _client = client;
                _stream = client.GetStream();
                _reader = new StreamReader(_stream);
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        public async Task<ProtocolPacket> RequestAsync(
            ProtocolPacket packet,
            CancellationToken cancellationToken = default)
        {
            await _requestLock.WaitAsync(cancellationToken);
            try
            {
                await SendPacketCoreAsync(packet, cancellationToken);
                return await ReadPacketCoreAsync(cancellationToken);
            }
            finally
            {
                _requestLock.Release();
            }
        }

        public async Task SendPacketAsync(
            ProtocolPacket packet,
            CancellationToken cancellationToken = default)
        {
            await _requestLock.WaitAsync(cancellationToken);
            try
            {
                await SendPacketCoreAsync(packet, cancellationToken);
            }
            finally
            {
                _requestLock.Release();
            }
        }

        private async Task SendPacketCoreAsync(
            ProtocolPacket packet,
            CancellationToken cancellationToken)
        {
            if (_stream == null)
                throw new InvalidOperationException("Chưa kết nối máy chủ.");

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
            await _requestLock.WaitAsync(cancellationToken);
            try
            {
                return await ReadPacketCoreAsync(cancellationToken);
            }
            finally
            {
                _requestLock.Release();
            }
        }

        private async Task<ProtocolPacket> ReadPacketCoreAsync(
            CancellationToken cancellationToken)
        {
            if (_reader == null)
                throw new InvalidOperationException("Chưa kết nối máy chủ.");

            using CancellationTokenSource readCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            readCts.CancelAfter(ReadTimeoutMs);

            string? line = await _reader.ReadLineAsync(readCts.Token);

            if (line == null)
            {
                throw new IOException("Máy chủ đã đóng kết nối.");
            }

            return PacketHelper.Decode(line);
        }

        public void Dispose()
        {
            DisposeConnection();
        }

        private void DisposeConnection()
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
