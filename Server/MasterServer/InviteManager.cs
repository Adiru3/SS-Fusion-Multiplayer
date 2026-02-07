using System;
using System.Collections.Generic;
using System.Linq;

namespace SSFusionMultiplayer.MasterServer
{
    /// <summary>
    /// Менеджер приглашений для invite-only серверов
    /// </summary>
    public class InviteManager
    {
        private Dictionary<string, InviteCode> invites;
        private Random random;
        
        public class InviteCode
        {
            public string Code { get; set; }
            public string ServerId { get; set; }
            public string CreatorSteamId { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public int MaxUses { get; set; }
            public int UsedCount { get; set; }
            public List<string> UsedBy { get; set; }
            
            public InviteCode()
            {
                UsedBy = new List<string>();
            }
            
            public bool IsValid()
            {
                if (DateTime.Now > ExpiresAt)
                    return false;
                    
                if (MaxUses > 0 && UsedCount >= MaxUses)
                    return false;
                    
                return true;
            }
            
            public bool CanUse(string steamId)
            {
                if (!IsValid())
                    return false;
                    
                // Можно использовать повторно
                return true;
            }
        }
        
        public InviteManager()
        {
            invites = new Dictionary<string, InviteCode>();
            random = new Random();
        }
        
        /// <summary>
        /// Создать новый код приглашения
        /// </summary>
        public string CreateInvite(string serverId, string creatorSteamId, int expiresInMinutes = 60, int maxUses = 0)
        {
            string code = GenerateCode();
            
            InviteCode invite = new InviteCode
            {
                Code = code,
                ServerId = serverId,
                CreatorSteamId = creatorSteamId,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(expiresInMinutes),
                MaxUses = maxUses,
                UsedCount = 0
            };
            
            invites[code] = invite;
            
            return code;
        }
        
        /// <summary>
        /// Валидировать код приглашения
        /// </summary>
        public bool ValidateInvite(string code, string steamId, out string serverId)
        {
            serverId = null;
            
            if (!invites.ContainsKey(code))
                return false;
                
            InviteCode invite = invites[code];
            
            if (!invite.CanUse(steamId))
                return false;
                
            // Отмечаем использование
            invite.UsedCount++;
            if (!invite.UsedBy.Contains(steamId))
                invite.UsedBy.Add(steamId);
                
            serverId = invite.ServerId;
            return true;
        }
        
        /// <summary>
        /// Отозвать приглашение
        /// </summary>
        public bool RevokeInvite(string code)
        {
            return invites.Remove(code);
        }
        
        /// <summary>
        /// Получить все приглашения для сервера
        /// </summary>
        public List<InviteCode> GetServerInvites(string serverId)
        {
            return invites.Values.Where(i => i.ServerId == serverId).ToList();
        }
        
        /// <summary>
        /// Очистить истёкшие приглашения
        /// </summary>
        public void CleanupExpired()
        {
            List<string> toRemove = new List<string>();
            
            foreach (var kvp in invites)
            {
                if (!kvp.Value.IsValid())
                    toRemove.Add(kvp.Key);
            }
            
            foreach (string code in toRemove)
            {
                invites.Remove(code);
            }
        }
        
        /// <summary>
        /// Генерация уникального кода
        /// </summary>
        private string GenerateCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Без похожих символов
            string code;
            
            do
            {
                char[] result = new char[8];
                for (int i = 0; i < 8; i++)
                {
                    result[i] = chars[random.Next(chars.Length)];
                }
                code = new string(result);
            }
            while (invites.ContainsKey(code));
            
            // Форматируем как XXXX-XXXX
            return code.Substring(0, 4) + "-" + code.Substring(4, 4);
        }
    }
}
