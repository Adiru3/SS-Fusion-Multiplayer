using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SSFusionMultiplayer.Core
{
    /// <summary>
    /// NAT Traversal с поддержкой STUN и UDP Hole Punching
    /// </summary>
    public class NATTraversal
    {
        // Публичные STUN серверы
        private static readonly string[] STUN_SERVERS = new string[]
        {
            "stun.l.google.com:19302",
            "stun1.l.google.com:19302",
            "stun2.l.google.com:19302",
            "stun.stunprotocol.org:3478"
        };
        
        public enum NATType
        {
            Unknown,
            OpenInternet,
            FullCone,
            RestrictedCone,
            PortRestrictedCone,
            Symmetric,
            Blocked
        }
        
        public class NATInfo
        {
            public IPEndPoint PublicEndPoint { get; set; }
            public IPEndPoint LocalEndPoint { get; set; }
            public NATType Type { get; set; }
            public bool CanUseP2P { get; set; }
        }
        
        /// <summary>
        /// Определить внешний IP и тип NAT через STUN
        /// </summary>
        public static NATInfo DetectNAT(int localPort)
        {
            NATInfo info = new NATInfo();
            info.Type = NATType.Unknown;
            info.CanUseP2P = false;
            
            try
            {
                using (UdpClient client = new UdpClient(localPort))
                {
                    client.Client.ReceiveTimeout = 3000;
                    
                    IPEndPoint localEP = (IPEndPoint)client.Client.LocalEndPoint;
                    info.LocalEndPoint = localEP;
                    
                    // Пробуем разные STUN серверы
                    foreach (string stunServer in STUN_SERVERS)
                    {
                        try
                        {
                            string[] parts = stunServer.Split(':');
                            string host = parts[0];
                            int port = int.Parse(parts[1]);
                            
                            IPAddress[] addresses = Dns.GetHostAddresses(host);
                            if (addresses.Length == 0)
                                continue;
                                
                            IPEndPoint stunEP = new IPEndPoint(addresses[0], port);
                            
                            // Отправляем STUN Binding Request
                            byte[] request = CreateSTUNBindingRequest();
                            client.Send(request, request.Length, stunEP);
                            
                            // Ждём ответ
                            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                            byte[] response = client.Receive(ref remoteEP);
                            
                            // Парсим ответ
                            IPEndPoint publicEP = ParseSTUNResponse(response);
                            if (publicEP != null)
                            {
                                info.PublicEndPoint = publicEP;
                                
                                // Определяем тип NAT
                                if (publicEP.Address.Equals(GetLocalIPAddress()) && 
                                    publicEP.Port == localEP.Port)
                                {
                                    info.Type = NATType.OpenInternet;
                                    info.CanUseP2P = true;
                                }
                                else
                                {
                                    // Упрощённое определение - для полного нужны дополнительные тесты
                                    info.Type = NATType.FullCone;
                                    info.CanUseP2P = true;
                                }
                                
                                break;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }
            catch (Exception)
            {
                info.Type = NATType.Blocked;
            }
            
            return info;
        }
        
        /// <summary>
        /// Создать STUN Binding Request
        /// </summary>
        private static byte[] CreateSTUNBindingRequest()
        {
            byte[] request = new byte[20];
            
            // Message Type: Binding Request (0x0001)
            request[0] = 0x00;
            request[1] = 0x01;
            
            // Message Length: 0
            request[2] = 0x00;
            request[3] = 0x00;
            
            // Magic Cookie: 0x2112A442
            request[4] = 0x21;
            request[5] = 0x12;
            request[6] = 0xA4;
            request[7] = 0x42;
            
            // Transaction ID: Random 12 bytes
            Random rnd = new Random();
            for (int i = 8; i < 20; i++)
            {
                request[i] = (byte)rnd.Next(256);
            }
            
            return request;
        }
        
        /// <summary>
        /// Парсинг STUN ответа
        /// </summary>
        private static IPEndPoint ParseSTUNResponse(byte[] response)
        {
            if (response.Length < 20)
                return null;
                
            // Проверяем Magic Cookie
            if (response[4] != 0x21 || response[5] != 0x12 || 
                response[6] != 0xA4 || response[7] != 0x42)
                return null;
                
            int messageLength = (response[2] << 8) | response[3];
            int offset = 20;
            
            while (offset < 20 + messageLength)
            {
                if (offset + 4 > response.Length)
                    break;
                    
                int attrType = (response[offset] << 8) | response[offset + 1];
                int attrLength = (response[offset + 2] << 8) | response[offset + 3];
                offset += 4;
                
                // XOR-MAPPED-ADDRESS (0x0020) или MAPPED-ADDRESS (0x0001)
                if ((attrType == 0x0020 || attrType == 0x0001) && attrLength >= 8)
                {
                    byte family = response[offset + 1];
                    
                    if (family == 0x01) // IPv4
                    {
                        int port = (response[offset + 2] << 8) | response[offset + 3];
                        
                        // XOR с Magic Cookie для XOR-MAPPED-ADDRESS
                        if (attrType == 0x0020)
                        {
                            port ^= 0x2112;
                        }
                        
                        byte[] ipBytes = new byte[4];
                        Array.Copy(response, offset + 4, ipBytes, 0, 4);
                        
                        // XOR с Magic Cookie для XOR-MAPPED-ADDRESS
                        if (attrType == 0x0020)
                        {
                            ipBytes[0] ^= 0x21;
                            ipBytes[1] ^= 0x12;
                            ipBytes[2] ^= 0xA4;
                            ipBytes[3] ^= 0x42;
                        }
                        
                        IPAddress ip = new IPAddress(ipBytes);
                        return new IPEndPoint(ip, port);
                    }
                }
                
                offset += attrLength;
                
                // Padding to 4-byte boundary
                while (offset % 4 != 0)
                    offset++;
            }
            
            return null;
        }
        
        /// <summary>
        /// Получить локальный IP адрес
        /// </summary>
        private static IPAddress GetLocalIPAddress()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address;
                }
            }
            catch
            {
                return IPAddress.Loopback;
            }
        }
        
        /// <summary>
        /// Выполнить UDP Hole Punching
        /// </summary>
        public static void PerformHolePunch(UdpConnection connection, IPEndPoint targetEndPoint, int attempts = 10)
        {
            for (int i = 0; i < attempts; i++)
            {
                NetworkPacket punch = new NetworkPacket(NetworkPacket.PacketType.HolePunch);
                connection.Send(punch, targetEndPoint);
                Thread.Sleep(100);
            }
        }
    }
}
