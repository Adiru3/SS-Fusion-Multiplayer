using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using SSFusionMultiplayer.Core;

namespace SSFusionMultiplayer.Relay
{
    /// <summary>
    /// Relay сервер для пересылки пакетов между пирами с проблемным NAT
    /// </summary>
    public class RelayServer
    {
        private UdpConnection connection;
        private Dictionary<string, RelaySession> sessions;
        private Thread cleanupThread;
        private bool isRunning;
        
        public int Port { get; private set; }
        
        public event Action<string> OnLog;
        
        private class RelaySession
        {
            public string SessionId { get; set; }
            public IPEndPoint Peer1 { get; set; }
            public IPEndPoint Peer2 { get; set; }
            public DateTime LastActivity { get; set; }
            public long BytesRelayed { get; set; }
        }
        
        public RelayServer(int port = 9000)
        {
            Port = port;
            sessions = new Dictionary<string, RelaySession>();
        }
        
        /// <summary>
        /// Запустить Relay сервер
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
                
                // Запускаем поток очистки
                cleanupThread = new Thread(CleanupLoop);
                cleanupThread.IsBackground = true;
                cleanupThread.Start();
                
                Log("Relay Server started on port " + Port);
                return true;
            }
            catch (Exception ex)
            {
                Log("Failed to start Relay Server: " + ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Остановить Relay сервер
        /// </summary>
        public void Stop()
        {
            if (!isRunning)
                return;
                
            isRunning = false;
            
            if (cleanupThread != null && cleanupThread.IsAlive)
            {
                cleanupThread.Join(1000);
            }
            
            if (connection != null)
            {
                connection.Dispose();
                connection = null;
            }
            
            Log("Relay Server stopped");
        }
        
        /// <summary>
        /// Обработка пакетов
        /// </summary>
        private void HandlePacket(NetworkPacket packet, IPEndPoint endpoint)
        {
            if (packet.Type == NetworkPacket.PacketType.RelayData)
            {
                // Находим сессию для этого endpoint
                RelaySession session = FindSession(endpoint);
                
                if (session != null)
                {
                    // Пересылаем другому пиру
                    IPEndPoint targetEndpoint = session.Peer1.Equals(endpoint) ? session.Peer2 : session.Peer1;
                    connection.SendRaw(packet.Data, targetEndpoint);
                    
                    session.LastActivity = DateTime.Now;
                    session.BytesRelayed += packet.Data.Length;
                }
            }
        }
        
        /// <summary>
        /// Найти сессию по endpoint
        /// </summary>
        private RelaySession FindSession(IPEndPoint endpoint)
        {
            foreach (var session in sessions.Values)
            {
                if (session.Peer1.Equals(endpoint) || session.Peer2.Equals(endpoint))
                    return session;
            }
            return null;
        }
        
        /// <summary>
        /// Создать новую relay сессию
        /// </summary>
        public string CreateSession(IPEndPoint peer1, IPEndPoint peer2)
        {
            string sessionId = Guid.NewGuid().ToString();
            
            RelaySession session = new RelaySession
            {
                SessionId = sessionId,
                Peer1 = peer1,
                Peer2 = peer2,
                LastActivity = DateTime.Now,
                BytesRelayed = 0
            };
            
            sessions[sessionId] = session;
            
            Log(string.Format("Session created: {0} <-> {1}", peer1, peer2));
            
            return sessionId;
        }
        
        /// <summary>
        /// Цикл очистки неактивных сессий
        /// </summary>
        private void CleanupLoop()
        {
            while (isRunning)
            {
                try
                {
                    Thread.Sleep(30000); // Каждые 30 секунд
                    
                    List<string> toRemove = new List<string>();
                    
                    foreach (var kvp in sessions)
                    {
                        if ((DateTime.Now - kvp.Value.LastActivity).TotalMinutes > 5)
                        {
                            toRemove.Add(kvp.Key);
                        }
                    }
                    
                    foreach (string sessionId in toRemove)
                    {
                        sessions.Remove(sessionId);
                        Log("Session timeout: " + sessionId);
                    }
                }
                catch (Exception ex)
                {
                    Log("Cleanup error: " + ex.Message);
                }
            }
        }
        
        private void Log(string message)
        {
            if (OnLog != null)
                OnLog("[RelayServer] " + message);
        }
    }
    
    /// <summary>
    /// Точка входа для Relay Server
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            int port = 9000;
            
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-port" && i + 1 < args.Length)
                {
                    int.TryParse(args[i + 1], out port);
                }
            }
            
            Console.WriteLine("=== SS Fusion Relay Server ===");
            Console.WriteLine("Starting on port " + port + "...");
            Console.WriteLine();
            
            RelayServer server = new RelayServer(port);
            server.OnLog += (msg) => Console.WriteLine(msg);
            
            if (server.Start())
            {
                Console.WriteLine("Relay Server is running!");
                Console.WriteLine("Press any key to stop...");
                Console.ReadKey();
                
                server.Stop();
            }
            else
            {
                Console.WriteLine("Failed to start Relay Server!");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
