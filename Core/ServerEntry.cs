using System;
using System.Collections.Generic;

namespace SSFusionMultiplayer.Core
{
    /// <summary>
    /// Информация о сервере в мастер-листе
    /// </summary>
    [Serializable]
    public class ServerEntry
    {
        public string ServerId { get; set; }
        public string ServerName { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public string ExternalIP { get; set; }
        
        public string GameMode { get; set; }
        public string MapName { get; set; }
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        
        public PrivacyMode Privacy { get; set; }
        public bool HasPassword { get; set; }
        
        public string HostSteamId { get; set; }
        public List<string> AllowedSteamIds { get; set; }
        
        public DateTime LastHeartbeat { get; set; }
        public int Ping { get; set; }
        
        public Dictionary<string, string> CustomData { get; set; }
        
        public bool IsDedicated { get; set; }
        public string Version { get; set; }
        
        public enum PrivacyMode
        {
            Public = 0,
            FriendsOnly = 1,
            InviteOnly = 2,
            PasswordProtected = 3
        }
        
        public ServerEntry()
        {
            ServerId = Guid.NewGuid().ToString();
            ServerName = "Unnamed Server";
            GameMode = "Deathmatch";
            MapName = "Unknown";
            CurrentPlayers = 0;
            MaxPlayers = 16;
            Privacy = PrivacyMode.Public;
            HasPassword = false;
            LastHeartbeat = DateTime.Now;
            Ping = 0;
            CustomData = new Dictionary<string, string>();
            AllowedSteamIds = new List<string>();
            IsDedicated = false;
            Version = "1.0";
        }
        
        /// <summary>
        /// Проверка, активен ли сервер (получал heartbeat недавно)
        /// </summary>
        public bool IsAlive()
        {
            return (DateTime.Now - LastHeartbeat).TotalSeconds < 60;
        }
        
        /// <summary>
        /// Проверка, может ли игрок присоединиться
        /// </summary>
        public bool CanJoin(string steamId = null)
        {
            if (CurrentPlayers >= MaxPlayers)
                return false;
                
            if (Privacy == PrivacyMode.FriendsOnly && !string.IsNullOrEmpty(steamId))
            {
                return AllowedSteamIds.Contains(steamId);
            }
            
            if (Privacy == PrivacyMode.InviteOnly && !string.IsNullOrEmpty(steamId))
            {
                return AllowedSteamIds.Contains(steamId);
            }
            
            return true;
        }
        
        /// <summary>
        /// Получить полный адрес сервера
        /// </summary>
        public string GetAddress()
        {
            return string.Format("{0}:{1}", ExternalIP ?? HostName, Port);
        }
    }
}
