using System;
using System.IO;
using System.Text;

namespace SSFusionMultiplayer.Core
{
    /// <summary>
    /// Базовый класс для сетевых пакетов
    /// </summary>
    public class NetworkPacket
    {
        public enum PacketType : byte
        {
            // Connection
            ConnectionRequest = 0,
            ConnectionAccept = 1,
            ConnectionReject = 2,
            Disconnect = 3,
            Heartbeat = 4,
            
            // P2P Discovery
            PeerDiscovery = 10,
            PeerInfo = 11,
            HolePunch = 12,
            
            // Game State
            GameStateUpdate = 20,
            PlayerInput = 21,
            PlayerJoin = 22,
            PlayerLeave = 23,
            
            // Server Browser
            ServerQuery = 30,
            ServerInfo = 31,
            ServerList = 32,
            
            // Invite System
            InviteRequest = 40,
            InviteResponse = 41,
            
            // Relay
            RelayData = 50,
            
            // Chat
            ChatMessage = 60
        }
        
        public PacketType Type { get; set; }
        public uint SequenceNumber { get; set; }
        public byte[] Data { get; set; }
        
        public NetworkPacket()
        {
            Data = new byte[0];
        }
        
        public NetworkPacket(PacketType type, byte[] data = null)
        {
            Type = type;
            Data = data ?? new byte[0];
        }
        
        /// <summary>
        /// Сериализация пакета в байты
        /// </summary>
        public byte[] Serialize()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write((byte)Type);
                writer.Write(SequenceNumber);
                writer.Write(Data.Length);
                writer.Write(Data);
                return ms.ToArray();
            }
        }
        
        /// <summary>
        /// Десериализация пакета из байтов
        /// </summary>
        public static NetworkPacket Deserialize(byte[] buffer)
        {
            using (MemoryStream ms = new MemoryStream(buffer))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                NetworkPacket packet = new NetworkPacket();
                packet.Type = (PacketType)reader.ReadByte();
                packet.SequenceNumber = reader.ReadUInt32();
                int dataLength = reader.ReadInt32();
                packet.Data = reader.ReadBytes(dataLength);
                return packet;
            }
        }
        
        /// <summary>
        /// Создать пакет со строковыми данными
        /// </summary>
        public static NetworkPacket CreateStringPacket(PacketType type, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            return new NetworkPacket(type, data);
        }
        
        /// <summary>
        /// Получить строку из данных пакета
        /// </summary>
        public string GetString()
        {
            return Encoding.UTF8.GetString(Data);
        }
    }
}
