using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Runtime.Serialization.Json;
using SSFusionMultiplayer.Core;

namespace SSFusionMultiplayer.MasterServer
{
    /// <summary>
    /// Master Server для регистрации и поиска игровых серверов
    /// </summary>
    public class MasterServer
    {
        private HttpListener httpListener;
        private Thread listenerThread;
        private Dictionary<string, ServerEntry> servers;
        private InviteManager inviteManager;
        private bool isRunning;
        
        public int Port { get; private set; }
        public int ServerCount { get { return servers.Count; } }
        
        public event Action<string> OnLog;
        
        public MasterServer(int port = 8080)
        {
            Port = port;
            servers = new Dictionary<string, ServerEntry>();
            inviteManager = new InviteManager();
        }
        
        /// <summary>
        /// Запустить Master Server
        /// </summary>
        public bool Start()
        {
            if (isRunning)
                return false;
                
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add(string.Format("http://+:{0}/", Port));
                httpListener.Start();
                
                isRunning = true;
                
                listenerThread = new Thread(ListenerLoop);
                listenerThread.IsBackground = true;
                listenerThread.Start();
                
                // Запускаем поток очистки
                Thread cleanupThread = new Thread(CleanupLoop);
                cleanupThread.IsBackground = true;
                cleanupThread.Start();
                
                Log("Master Server started on port " + Port);
                return true;
            }
            catch (Exception ex)
            {
                Log("Failed to start Master Server: " + ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Остановить Master Server
        /// </summary>
        public void Stop()
        {
            if (!isRunning)
                return;
                
            isRunning = false;
            
            if (httpListener != null)
            {
                httpListener.Stop();
                httpListener.Close();
            }
            
            if (listenerThread != null && listenerThread.IsAlive)
            {
                listenerThread.Join(1000);
            }
            
            Log("Master Server stopped");
        }
        
        /// <summary>
        /// Основной цикл обработки запросов
        /// </summary>
        private void ListenerLoop()
        {
            while (isRunning)
            {
                try
                {
                    HttpListenerContext context = httpListener.GetContext();
                    ThreadPool.QueueUserWorkItem(HandleRequest, context);
                }
                catch (HttpListenerException)
                {
                    if (!isRunning)
                        break;
                }
                catch (Exception ex)
                {
                    Log("Listener error: " + ex.Message);
                }
            }
        }
        
        /// <summary>
        /// Обработка HTTP запроса
        /// </summary>
        private void HandleRequest(object state)
        {
            HttpListenerContext context = (HttpListenerContext)state;
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            
            try
            {
                string path = request.Url.AbsolutePath.ToLower();
                string method = request.HttpMethod.ToUpper();
                
                Log(string.Format("{0} {1}", method, path));
                
                // CORS headers
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
                
                if (method == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }
                
                // Роутинг
                if (path == "/api/servers" && method == "GET")
                {
                    HandleGetServers(request, response);
                }
                else if (path == "/api/servers" && method == "POST")
                {
                    HandleRegisterServer(request, response);
                }
                else if (path.StartsWith("/api/servers/") && method == "PUT")
                {
                    HandleUpdateServer(request, response);
                }
                else if (path.StartsWith("/api/servers/") && method == "DELETE")
                {
                    HandleUnregisterServer(request, response);
                }
                else if (path == "/api/invite/create" && method == "POST")
                {
                    HandleCreateInvite(request, response);
                }
                else if (path == "/api/invite/validate" && method == "POST")
                {
                    HandleValidateInvite(request, response);
                }
                else if (path == "/api/stats" && method == "GET")
                {
                    HandleGetStats(request, response);
                }
                else
                {
                    SendResponse(response, 404, "{\"error\":\"Not found\"}");
                }
            }
            catch (Exception ex)
            {
                Log("Request error: " + ex.Message);
                SendResponse(response, 500, "{\"error\":\"Internal server error\"}");
            }
        }
        
        /// <summary>
        /// GET /api/servers - Получить список серверов
        /// </summary>
        private void HandleGetServers(HttpListenerRequest request, HttpListenerResponse response)
        {
            var query = ParseQueryString(request.Url.Query);
            
            List<ServerEntry> result = servers.Values.Where(s => s.IsAlive()).ToList();
            
            // Фильтры
            if (query.ContainsKey("gamemode"))
            {
                string gameMode = query["gamemode"];
                result = result.Where(s => s.GameMode.Equals(gameMode, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            if (query.ContainsKey("map"))
            {
                string map = query["map"];
                result = result.Where(s => s.MapName.Equals(map, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            if (query.ContainsKey("notfull"))
            {
                result = result.Where(s => s.CurrentPlayers < s.MaxPlayers).ToList();
            }
            
            if (query.ContainsKey("nopassword"))
            {
                result = result.Where(s => !s.HasPassword).ToList();
            }
            
            string json = SerializeToJson(result);
            SendResponse(response, 200, json);
        }
        
        /// <summary>
        /// POST /api/servers - Зарегистрировать сервер
        /// </summary>
        private void HandleRegisterServer(HttpListenerRequest request, HttpListenerResponse response)
        {
            string body = ReadRequestBody(request);
            ServerEntry server = DeserializeFromJson<ServerEntry>(body);
            
            if (server == null)
            {
                SendResponse(response, 400, "{\"error\":\"Invalid server data\"}");
                return;
            }
            
            // Генерируем ID если нет
            if (string.IsNullOrEmpty(server.ServerId))
                server.ServerId = Guid.NewGuid().ToString();
                
            // Устанавливаем IP из запроса
            if (string.IsNullOrEmpty(server.ExternalIP))
                server.ExternalIP = request.RemoteEndPoint.Address.ToString();
                
            server.LastHeartbeat = DateTime.Now;
            
            servers[server.ServerId] = server;
            
            Log(string.Format("Server registered: {0} ({1})", server.ServerName, server.ServerId));
            
            string json = string.Format("{{\"serverId\":\"{0}\"}}", server.ServerId);
            SendResponse(response, 200, json);
        }
        
        /// <summary>
        /// PUT /api/servers/{id} - Обновить информацию о сервере (heartbeat)
        /// </summary>
        private void HandleUpdateServer(HttpListenerRequest request, HttpListenerResponse response)
        {
            string serverId = request.Url.AbsolutePath.Substring("/api/servers/".Length);
            
            if (!servers.ContainsKey(serverId))
            {
                SendResponse(response, 404, "{\"error\":\"Server not found\"}");
                return;
            }
            
            string body = ReadRequestBody(request);
            ServerEntry updates = DeserializeFromJson<ServerEntry>(body);
            
            ServerEntry server = servers[serverId];
            
            // Обновляем поля
            if (updates != null)
            {
                if (!string.IsNullOrEmpty(updates.ServerName))
                    server.ServerName = updates.ServerName;
                if (updates.CurrentPlayers >= 0)
                    server.CurrentPlayers = updates.CurrentPlayers;
                if (!string.IsNullOrEmpty(updates.MapName))
                    server.MapName = updates.MapName;
            }
            
            server.LastHeartbeat = DateTime.Now;
            
            SendResponse(response, 200, "{\"status\":\"updated\"}");
        }
        
        /// <summary>
        /// DELETE /api/servers/{id} - Удалить сервер
        /// </summary>
        private void HandleUnregisterServer(HttpListenerRequest request, HttpListenerResponse response)
        {
            string serverId = request.Url.AbsolutePath.Substring("/api/servers/".Length);
            
            if (servers.Remove(serverId))
            {
                Log("Server unregistered: " + serverId);
                SendResponse(response, 200, "{\"status\":\"removed\"}");
            }
            else
            {
                SendResponse(response, 404, "{\"error\":\"Server not found\"}");
            }
        }
        
        /// <summary>
        /// POST /api/invite/create - Создать приглашение
        /// </summary>
        private void HandleCreateInvite(HttpListenerRequest request, HttpListenerResponse response)
        {
            string body = ReadRequestBody(request);
            var data = DeserializeFromJson<Dictionary<string, string>>(body);
            
            if (!data.ContainsKey("serverId"))
            {
                SendResponse(response, 400, "{\"error\":\"serverId required\"}");
                return;
            }
            
            string serverId = data["serverId"];
            string steamId = data.ContainsKey("steamId") ? data["steamId"] : "";
            int expires = data.ContainsKey("expires") ? int.Parse(data["expires"]) : 60;
            
            string code = inviteManager.CreateInvite(serverId, steamId, expires);
            
            string json = string.Format("{{\"code\":\"{0}\"}}", code);
            SendResponse(response, 200, json);
        }
        
        /// <summary>
        /// POST /api/invite/validate - Валидировать приглашение
        /// </summary>
        private void HandleValidateInvite(HttpListenerRequest request, HttpListenerResponse response)
        {
            string body = ReadRequestBody(request);
            var data = DeserializeFromJson<Dictionary<string, string>>(body);
            
            if (!data.ContainsKey("code"))
            {
                SendResponse(response, 400, "{\"error\":\"code required\"}");
                return;
            }
            
            string code = data["code"];
            string steamId = data.ContainsKey("steamId") ? data["steamId"] : "";
            
            string serverId;
            bool valid = inviteManager.ValidateInvite(code, steamId, out serverId);
            
            if (valid && servers.ContainsKey(serverId))
            {
                ServerEntry server = servers[serverId];
                string serverJson = SerializeToJson(server);
                string json = string.Format("{{\"valid\":true,\"server\":{0}}}", serverJson);
                SendResponse(response, 200, json);
            }
            else
            {
                SendResponse(response, 200, "{\"valid\":false}");
            }
        }
        
        /// <summary>
        /// GET /api/stats - Статистика
        /// </summary>
        private void HandleGetStats(HttpListenerRequest request, HttpListenerResponse response)
        {
            int totalServers = servers.Count;
            int activeServers = servers.Values.Count(s => s.IsAlive());
            int totalPlayers = servers.Values.Where(s => s.IsAlive()).Sum(s => s.CurrentPlayers);
            
            string json = string.Format("{{\"totalServers\":{0},\"activeServers\":{1},\"totalPlayers\":{2}}}", 
                totalServers, activeServers, totalPlayers);
            SendResponse(response, 200, json);
        }
        
        /// <summary>
        /// Цикл очистки неактивных серверов
        /// </summary>
        private void CleanupLoop()
        {
            while (isRunning)
            {
                try
                {
                    Thread.Sleep(30000); // Каждые 30 секунд
                    
                    List<string> toRemove = new List<string>();
                    
                    foreach (var kvp in servers)
                    {
                        if (!kvp.Value.IsAlive())
                            toRemove.Add(kvp.Key);
                    }
                    
                    foreach (string serverId in toRemove)
                    {
                        servers.Remove(serverId);
                        Log("Server timeout: " + serverId);
                    }
                    
                    inviteManager.CleanupExpired();
                }
                catch (Exception ex)
                {
                    Log("Cleanup error: " + ex.Message);
                }
            }
        }
        
        // Утилиты
        
        private void SendResponse(HttpListenerResponse response, int statusCode, string content)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }
        
        private string ReadRequestBody(HttpListenerRequest request)
        {
            using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                return reader.ReadToEnd();
            }
        }
        
        private Dictionary<string, string> ParseQueryString(string query)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            
            if (string.IsNullOrEmpty(query))
                return result;
                
            query = query.TrimStart('?');
            string[] pairs = query.Split('&');
            
            foreach (string pair in pairs)
            {
                string[] parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    string key = Uri.UnescapeDataString(parts[0]);
                    string value = Uri.UnescapeDataString(parts[1]);
                    result[key] = value;
                }
            }
            
            return result;
        }
        
        private string SerializeToJson(object obj)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
        
        private T DeserializeFromJson<T>(string json) where T : class
        {
            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return serializer.ReadObject(ms) as T;
                }
            }
            catch
            {
                return null;
            }
        }
        
        private void Log(string message)
        {
            if (OnLog != null)
                OnLog("[MasterServer] " + message);
        }
    }
}
