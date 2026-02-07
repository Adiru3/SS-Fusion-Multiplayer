using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace SSFusionMultiplayer.Core
{
    /// <summary>
    /// P2P хост для игровой сессии
    /// </summary>
    public class P2PHost : IDisposable
    {
        private UdpConnection connection;
        private Dictionary<string, PeerInfo> connectedPeers;
        private Thread heartbeatThread;
        private bool isRunning;
        
        public class PeerInfo
        {
            public string PeerId { get; set; }
            public IPEndPoint EndPoint { get; set; }
            public DateTime LastHeartbeat { get; set; }
            public string PlayerName { get; set; }
            public bool IsReady { get; set; }
        }
        
        public class ServerSettings
        {
            public string ServerName { get; set; }
            public string Password { get; set; }
            public int MaxPlayers { get; set; }
            public string GameMode { get; set; }
            public string MapName { get; set; }
            public PrivacyMode Privacy { get; set; }
            
            public ServerSettings()
            {
                ServerName = "My Server";
                Password = "";
                MaxPlayers = 16;
                GameMode = "Deathmatch";
                MapName = "Default";
                Privacy = PrivacyMode.Public;
            }
        }
        
        public enum PrivacyMode
        {
            Public,
            FriendsOnly,
            InviteOnly,
            PasswordProtected
        }
        
        public ServerSettings Settings { get; set; }
        public int Port { get; private set; }
        public int CurrentPlayers { get { return connectedPeers.Count + 1; } }
        public bool IsRunning { get { return isRunning; } }
        
        public event Action<PeerInfo> OnPeerConnected;
        public event Action<PeerInfo> OnPeerDisconnected;
        public event Action<PeerInfo, byte[]> OnDataReceived;
        public event Action<string> OnLog;
        
        public P2PHost(int port = 0)
        {
            Port = port;
            connectedPeers = new Dictionary<string, PeerInfo>();
            Settings = new ServerSettings();
        }
        
        /// <summary>
        /// Запустить P2P хост
        /// </summary>
        public bool Start()
        {
            if (isRunning)
                return false;
                
            try
            {
                connection = new UdpConnection(Port);
                connection.OnPacketReceived += HandlePacket;
                connection.OnError += (ex) => Log("Error: " + ex.Message);
                connection.Start();
                
                Port = connection.Port;
                isRunning = true;
                
                // Запускаем поток для heartbeat и проверки таймаутов
                heartbeatThread = new Thread(HeartbeatLoop);
                heartbeatThread.IsBackground = true;
                heartbeatThread.Start();
                
                Log("P2P Host started on port " + Port);
                return true;
            }
            catch (Exception ex)
            {
                Log("Failed to start host: " + ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Остановить P2P хост
        /// </summary>
        public void Stop()
        {
            if (!isRunning)
                return;
                
            isRunning = false;
            
            // Отправляем всем disconnect
            foreach (var peer in connectedPeers.Values)
            {
                NetworkPacket disconnect = new NetworkPacket(NetworkPacket.PacketType.Disconnect);
                connection.Send(disconnect, peer.EndPoint);
            }
            
            connectedPeers.Clear();
            
            if (heartbeatThread != null && heartbeatThread.IsAlive)
            {
                heartbeatThread.Join(1000);
            }
            
            if (connection != null)
            {
                connection.Dispose();
                connection = null;
            }
            
            Log("P2P Host stopped");
        }
        
        /// <summary>
        /// Обработка входящих пакетов
        /// </summary>
        private void HandlePacket(NetworkPacket packet, IPEndPoint endpoint)
        {
            string peerId = endpoint.ToString();
            
            switch (packet.Type)
            {
                case NetworkPacket.PacketType.ConnectionRequest:
                    HandleConnectionRequest(packet, endpoint);
                    break;
                    
                case NetworkPacket.PacketType.Heartbeat:
                    if (connectedPeers.ContainsKey(peerId))
                    {
                        connectedPeers[peerId].LastHeartbeat = DateTime.Now;
                    }
                    break;
                    
                case NetworkPacket.PacketType.Disconnect:
                    HandleDisconnect(peerId);
                    break;
                    
                case NetworkPacket.PacketType.PlayerInput:
                case NetworkPacket.PacketType.ChatMessage:
                    if (connectedPeers.ContainsKey(peerId))
                    {
                        // Ретранслируем всем остальным
                        BroadcastToOthers(packet, peerId);
                        
                        if (OnDataReceived != null)
                            OnDataReceived(connectedPeers[peerId], packet.Data);
                    }
                    break;
                    
                case NetworkPacket.PacketType.HolePunch:
                    // Отвечаем на hole punch
                    NetworkPacket response = new NetworkPacket(NetworkPacket.PacketType.HolePunch);
                    connection.Send(response, endpoint);
                    break;
            }
        }
        
        /// <summary>
        /// Обработка запроса на подключение
        /// </summary>
        private void HandleConnectionRequest(NetworkPacket packet, IPEndPoint endpoint)
        {
            string peerId = endpoint.ToString();
            
            // Проверяем, не полон ли сервер
            if (CurrentPlayers >= Settings.MaxPlayers)
            {
                NetworkPacket reject = NetworkPacket.CreateStringPacket(
                    NetworkPacket.PacketType.ConnectionReject, 
                    "Server is full");
                connection.Send(reject, endpoint);
                Log("Connection rejected (server full): " + peerId);
                return;
            }
            
            // Проверяем пароль если нужно
            if (!string.IsNullOrEmpty(Settings.Password))
            {
                string providedPassword = packet.GetString();
                if (providedPassword != Settings.Password)
                {
                    NetworkPacket reject = NetworkPacket.CreateStringPacket(
                        NetworkPacket.PacketType.ConnectionReject, 
                        "Invalid password");
                    connection.Send(reject, endpoint);
                    Log("Connection rejected (wrong password): " + peerId);
                    return;
                }
            }
            
            // Принимаем подключение
            PeerInfo peer = new PeerInfo
            {
                PeerId = peerId,
                EndPoint = endpoint,
                LastHeartbeat = DateTime.Now,
                PlayerName = packet.GetString(),
                IsReady = false
            };
            
            connectedPeers[peerId] = peer;
            
            NetworkPacket accept = NetworkPacket.CreateStringPacket(
                NetworkPacket.PacketType.ConnectionAccept, 
                "Welcome!");
            connection.Send(accept, endpoint);
            
            Log("Peer connected: " + peerId);
            
            if (OnPeerConnected != null)
                OnPeerConnected(peer);
        }
        
        /// <summary>
        /// Обработка отключения
        /// </summary>
        private void HandleDisconnect(string peerId)
        {
            if (connectedPeers.ContainsKey(peerId))
            {
                PeerInfo peer = connectedPeers[peerId];
                connectedPeers.Remove(peerId);
                
                Log("Peer disconnected: " + peerId);
                
                if (OnPeerDisconnected != null)
                    OnPeerDisconnected(peer);
            }
        }
        
        /// <summary>
        /// Отправить данные всем кроме отправителя
        /// </summary>
        private void BroadcastToOthers(NetworkPacket packet, string excludePeerId)
        {
            foreach (var peer in connectedPeers.Values)
            {
                if (peer.PeerId != excludePeerId)
                {
                    connection.Send(packet, peer.EndPoint);
                }
            }
        }
        
        /// <summary>
        /// Отправить данные всем
        /// </summary>
        public void BroadcastToAll(NetworkPacket packet)
        {
            foreach (var peer in connectedPeers.Values)
            {
                connection.Send(packet, peer.EndPoint);
            }
        }
        
        /// <summary>
        /// Цикл heartbeat и проверки таймаутов
        /// </summary>
        private void HeartbeatLoop()
        {
            while (isRunning)
            {
                try
                {
                    List<string> toRemove = new List<string>();
                    
                    // Проверяем таймауты
                    foreach (var kvp in connectedPeers)
                    {
                        if ((DateTime.Now - kvp.Value.LastHeartbeat).TotalSeconds > 30)
                        {
                            toRemove.Add(kvp.Key);
                        }
                    }
                    
                    // Удаляем отключившихся
                    foreach (string peerId in toRemove)
                    {
                        HandleDisconnect(peerId);
                    }
                    
                    Thread.Sleep(5000);
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
                OnLog("[P2PHost] " + message);
        }
        
        public void Dispose()
        {
            Stop();
        }
    }
}
