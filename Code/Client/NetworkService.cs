using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Shared;

namespace Client
{
    public class NetworkService : IDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private StreamReader? _reader;

        public bool IsConnected => _client != null && _client.Connected;

        public async Task ConnectAsync(string ip, int port)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();
            _reader = new StreamReader(_stream);
        }

        public async Task SendPacketAsync(ProtocolPacket packet)
        {
            if (_stream == null) throw new InvalidOperationException("Chưa kết nối Server.");
            byte[] data = PacketHelper.Encode(packet);
            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();
        }

        public async Task<ProtocolPacket> ReadPacketAsync()
        {
            if (_reader == null) throw new InvalidOperationException("Chưa kết nối Server.");
            string? line = await _reader.ReadLineAsync();
            if (line == null)
                throw new IOException("Server đã đóng kết nối.");

            return PacketHelper.Decode(line);
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _stream?.Dispose();
            _client?.Close();
        }
    }
}