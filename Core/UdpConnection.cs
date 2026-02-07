using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SSFusionMultiplayer.Core
{
    /// <summary>
    /// UDP соединение с поддержкой асинхронной отправки/получения
    /// </summary>
    public class UdpConnection : IDisposable
    {
        private UdpClient udpClient;
        private Thread receiveThread;
        private bool isRunning;
        private uint sequenceNumber;
        
        public event Action<NetworkPacket, IPEndPoint> OnPacketReceived;
        public event Action<Exception> OnError;
        
        public int Port { get; private set; }
        public bool IsRunning { get { return isRunning; } }
        
        public UdpConnection(int port = 0)
        {
            Port = port;
            sequenceNumber = 0;
        }
        
        /// <summary>
        /// Запустить UDP сервер
        /// </summary>
        public void Start()
        {
            if (isRunning)
                return;
                
            try
            {
                udpClient = new UdpClient(Port);
                if (Port == 0)
                {
                    IPEndPoint localEP = (IPEndPoint)udpClient.Client.LocalEndPoint;
                    Port = localEP.Port;
                }
                
                isRunning = true;
                receiveThread = new Thread(ReceiveLoop);
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    OnError(ex);
            }
        }
        
        /// <summary>
        /// Остановить UDP сервер
        /// </summary>
        public void Stop()
        {
            isRunning = false;
            
            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(1000);
            }
            
            if (udpClient != null)
            {
                udpClient.Close();
                udpClient = null;
            }
        }
        
        /// <summary>
        /// Отправить пакет
        /// </summary>
        public void Send(NetworkPacket packet, IPEndPoint endpoint)
        {
            if (!isRunning || udpClient == null)
                return;
                
            try
            {
                packet.SequenceNumber = sequenceNumber++;
                byte[] data = packet.Serialize();
                udpClient.Send(data, data.Length, endpoint);
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    OnError(ex);
            }
        }
        
        /// <summary>
        /// Отправить данные напрямую
        /// </summary>
        public void SendRaw(byte[] data, IPEndPoint endpoint)
        {
            if (!isRunning || udpClient == null)
                return;
                
            try
            {
                udpClient.Send(data, data.Length, endpoint);
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    OnError(ex);
            }
        }
        
        /// <summary>
        /// Цикл приёма пакетов
        /// </summary>
        private void ReceiveLoop()
        {
            while (isRunning)
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpClient.Receive(ref remoteEP);
                    
                    if (data.Length > 0)
                    {
                        NetworkPacket packet = NetworkPacket.Deserialize(data);
                        
                        if (OnPacketReceived != null)
                            OnPacketReceived(packet, remoteEP);
                    }
                }
                catch (SocketException)
                {
                    // Нормально при закрытии сокета
                    if (!isRunning)
                        break;
                }
                catch (Exception ex)
                {
                    if (OnError != null)
                        OnError(ex);
                }
            }
        }
        
        public void Dispose()
        {
            Stop();
        }
    }
}
