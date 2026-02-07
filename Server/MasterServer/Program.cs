using System;
using System.Windows.Forms;

namespace SSFusionMultiplayer.MasterServer
{
    /// <summary>
    /// Точка входа для Master Server
    /// </summary>
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            int port = 8080;
            
            // Парсим аргументы командной строки
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-port" && i + 1 < args.Length)
                {
                    int.TryParse(args[i + 1], out port);
                }
            }
            
            Console.WriteLine("=== SS Fusion Master Server ===");
            Console.WriteLine("Starting on port " + port + "...");
            Console.WriteLine();
            
            MasterServer server = new MasterServer(port);
            server.OnLog += (msg) => Console.WriteLine(msg);
            
            if (server.Start())
            {
                Console.WriteLine("Master Server is running!");
                Console.WriteLine("API Endpoints:");
                Console.WriteLine("  GET    /api/servers        - List servers");
                Console.WriteLine("  POST   /api/servers        - Register server");
                Console.WriteLine("  PUT    /api/servers/{id}   - Update server");
                Console.WriteLine("  DELETE /api/servers/{id}   - Unregister server");
                Console.WriteLine("  POST   /api/invite/create  - Create invite");
                Console.WriteLine("  POST   /api/invite/validate- Validate invite");
                Console.WriteLine("  GET    /api/stats          - Get statistics");
                Console.WriteLine();
                Console.WriteLine("Press any key to stop...");
                Console.ReadKey();
                
                server.Stop();
            }
            else
            {
                Console.WriteLine("Failed to start Master Server!");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
