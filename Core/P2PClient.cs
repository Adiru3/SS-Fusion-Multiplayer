using System;
using System.Net;
using System.Threading;

namespace SSFusionMultiplayer.Core
{
    /// <summary>
    /// P2P клиент для подключения к хосту
    /// </summary>
    public class P2PClient : IDisposable
    {
        private UdpConnection connection;
        private IPEndPoint hostEndPoint;
        private Thread heartbeatThread;
        private bool isConnected;
        private bool isRunning;
        
        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected,
            Failed
        }
        
        public ConnectionState State { get; private set; }
        public string PlayerName { get; set; }
        public string Password { get; set; }
        
        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<byte[]> OnDataReceived;
        public event Action<string> OnLog;
        
        public P2PClient()
        {
            State = ConnectionState.Disconnected;
            PlayerName = "Player";
            Password = "";
        }
        
        /// <summary>
        /// Подключиться к хосту
        /// </summary>
        public bool Connect(string host, int port)
        {
            if (State == ConnectionState.Connected || State == ConnectionState.Connecting)
                return false;
                
            try
            {
                State = ConnectionState.Connecting;
                
                // Резолвим хост
                IPAddress[] addresses = Dns.GetHostAddresses(host);
                if (addresses.Length == 0)
                {
                    Log("Failed to resolve host: " + host);
                    State = ConnectionState.Failed;
                    return false;
                }
                
                hostEndPoint = new IPEndPoint(addresses[0], port);
                
                // Создаём соединение
                connection = new UdpConnection(0);
                connection.OnPacketReceived += HandlePacket;
                connection.OnError += (ex) => Log("Error: " + ex.Message);
                connection.Start();
                
                // Отправляем запрос на подключение
                string requestData = string.IsNullOrEmpty(Password) ? PlayerName : Password;
                NetworkPacket request = NetworkPacket.CreateStringPacket(
                    NetworkPacket.PacketType.ConnectionRequest, 
                    requestData);
                connection.Send(request, hostEndPoint);
                
                Log("Connecting to " + hostEndPoint + "...");
                
                // Ждём ответ (с таймаутом)
                int timeout = 5000; // 5 секунд
                int elapsed = 0;
                while (State == ConnectionState.Connecting && elapsed < timeout)
                {
                    Thread.Sleep(100);
                    elapsed += 100;
                }
                
                if (State == ConnectionState.Connected)
                {
                    // Запускаем heartbeat
                    isRunning = true;
                    heartbeatThread = new Thread(HeartbeatLoop);
                    heartbeatThread.IsBackground = true;
                    heartbeatThread.Start();
                    
                    return true;
                }
                else
                {
                    Log("Connection timeout");
                    State = ConnectionState.Failed;
                    Disconnect();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log("Connection failed: " + ex.Message);
                State = ConnectionState.Failed;
                return false;
            }
        }
        
        /// <summary>
        /// Отключиться от хоста
        /// </summary>
        public void Disconnect()
        {
            if (State == ConnectionState.Disconnected)
                return;
                
            isRunning = false;
            
            if (State == ConnectionState.Connected && connection != null)
            {
                NetworkPacket disconnect = new NetworkPacket(NetworkPacket.PacketType.Disconnect);
                connection.Send(disconnect, hostEndPoint);
            }
            
            State = ConnectionState.Disconnected;
            
            if (heartbeatThread != null && heartbeatThread.IsAlive)
            {
                heartbeatThread.Join(1000);
            }
            
            if (connection != null)
            {
                connection.Dispose();
                connection = null;
            }
            
            Log("Disconnected");
        }
        
        /// <summary>
        /// Отправить данные хосту
        /// </summary>
        public void Send(NetworkPacket packet)
        {
            if (State != ConnectionState.Connected || connection == null)
                return;
                
            connection.Send(packet, hostEndPoint);
        }
        
        /// <summary>
        /// Обработка входящих пакетов
        /// </summary>
        private void HandlePacket(NetworkPacket packet, IPEndPoint endpoint)
        {
            // Принимаем только от хоста
            if (!endpoint.Equals(hostEndPoint))
                return;
                
            switch (packet.Type)
            {
                case NetworkPacket.PacketType.ConnectionAccept:
                    State = ConnectionState.Connected;
                    isConnected = true;
                    Log("Connected: " + packet.GetString());
                    
                    if (OnConnected != null)
                        OnConnected();
                    break;
                    
                case NetworkPacket.PacketType.ConnectionReject:
                    State = ConnectionState.Failed;
                    string reason = packet.GetString();
                    Log("Connection rejected: " + reason);
                    
                    if (OnDisconnected != null)
                        OnDisconnected(reason);
                    break;
                    
                case NetworkPacket.PacketType.Disconnect:
                    Log("Disconnected by host");
                    Disconnect();
                    
                    if (OnDisconnected != null)
                        OnDisconnected("Disconnected by host");
                    break;
                    
                case NetworkPacket.PacketType.GameStateUpdate:
                case NetworkPacket.PacketType.PlayerInput:
                case NetworkPacket.PacketType.ChatMessage:
                    if (OnDataReceived != null)
                        OnDataReceived(packet.Data);
                    break;
                    
                case NetworkPacket.PacketType.HolePunch:
                    // Отвечаем на hole punch
                    NetworkPacket response = new NetworkPacket(NetworkPacket.PacketType.HolePunch);
                    connection.Send(response, endpoint);
                    break;
            }
        }
        
        /// <summary>
        /// Цикл отправки heartbeat
        /// </summary>
        private void HeartbeatLoop()
        {
            while (isRunning && State == ConnectionState.Connected)
            {
                try
                {
                    NetworkPacket heartbeat = new NetworkPacket(NetworkPacket.PacketType.Heartbeat);
                    connection.Send(heartbeat, hostEndPoint);
                    Thread.Sleep(10000); // Каждые 10 секунд
                }
                catch (Exception ex)
                {
                    Log("Heartbeat error: " + ex.Message);
                }
            }
        }
        
        private void Log(string message)
        {
            if (OnLog != null)
                OnLog("[P2PClient] " + message);
        }
        
        public void Dispose()
        {
            Disconnect();
        }
    }
}
