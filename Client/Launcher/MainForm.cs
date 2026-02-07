using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;
using SSFusionMultiplayer.Core;
using SSFusionMultiplayer.Core;

namespace SSFusionMultiplayer.Launcher
{
    /// <summary>
    /// Главная форма лаунчера
    /// </summary>
    public class MainForm : Form
    {
        private TabControl tabControl;
        private TabPage serverBrowserTab;
        private TabPage createServerTab;
        private TabPage settingsTab;
        
        private DataGridView serverList;
        private Button refreshButton;
        private Button joinButton;
        private Button quickJoinButton;
        private TextBox filterTextBox;
        
        // Create Server
        private TextBox serverNameBox;
        private ComboBox privacyModeBox;
        private TextBox passwordBox;
        private NumericUpDown maxPlayersBox;
        private ComboBox gameModeBox;
        private Button createP2PButton;
        private Button createDedicatedButton;
        private Label inviteCodeLabel;
        
        // Settings
        private TextBox masterServerUrlBox;
        private TextBox playerNameBox;
        private CheckBox enableSteamBox;
        
        private string masterServerUrl = "http://localhost:8000";
        private P2PHost currentHost;
        private P2PClient currentClient;
        
        public MainForm()
        {
            InitializeComponent();
            LoadSettings();
        }
        
        private void InitializeComponent()
        {
            this.Text = "SS Fusion Multiplayer Launcher";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            
            // Tab Control
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            
            // Server Browser Tab
            serverBrowserTab = new TabPage("Server Browser");
            InitializeServerBrowser();
            tabControl.TabPages.Add(serverBrowserTab);
            
            // Create Server Tab
            createServerTab = new TabPage("Create Server");
            InitializeCreateServer();
            tabControl.TabPages.Add(createServerTab);
            
            // Settings Tab
            settingsTab = new TabPage("Settings");
            InitializeSettings();
            tabControl.TabPages.Add(settingsTab);
            
            this.Controls.Add(tabControl);
            
            this.FormClosing += MainForm_FormClosing;
        }
        
        private void InitializeServerBrowser()
        {
            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 40;
            
            filterTextBox = new TextBox();
            filterTextBox.Location = new Point(10, 10);
            filterTextBox.Width = 200;
            filterTextBox.Text = "Filter servers...";
            filterTextBox.ForeColor = Color.Gray;
            filterTextBox.Enter += (s, e) => {
                if (filterTextBox.Text == "Filter servers...")
                {
                    filterTextBox.Text = "";
                    filterTextBox.ForeColor = Color.Black;
                }
            };
            filterTextBox.Leave += (s, e) => {
                if (string.IsNullOrWhiteSpace(filterTextBox.Text))
                {
                    filterTextBox.Text = "Filter servers...";
                    filterTextBox.ForeColor = Color.Gray;
                }
            };
            topPanel.Controls.Add(filterTextBox);
            
            refreshButton = new Button();
            refreshButton.Text = "Refresh";
            refreshButton.Location = new Point(220, 8);
            refreshButton.Click += RefreshButton_Click;
            topPanel.Controls.Add(refreshButton);
            
            quickJoinButton = new Button();
            quickJoinButton.Text = "Quick Join";
            quickJoinButton.Location = new Point(310, 8);
            quickJoinButton.Click += QuickJoinButton_Click;
            topPanel.Controls.Add(quickJoinButton);
            
            serverBrowserTab.Controls.Add(topPanel);
            
            // Server List
            serverList = new DataGridView();
            serverList.Dock = DockStyle.Fill;
            serverList.ReadOnly = true;
            serverList.AllowUserToAddRows = false;
            serverList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            serverList.MultiSelect = false;
            serverList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            
            serverList.Columns.Add("ServerName", "Server Name");
            serverList.Columns.Add("GameMode", "Game Mode");
            serverList.Columns.Add("Map", "Map");
            serverList.Columns.Add("Players", "Players");
            serverList.Columns.Add("Ping", "Ping");
            serverList.Columns.Add("Privacy", "Privacy");
            
            serverList.DoubleClick += ServerList_DoubleClick;
            
            serverBrowserTab.Controls.Add(serverList);
            
            // Bottom Panel
            Panel bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 50;
            
            joinButton = new Button();
            joinButton.Text = "Join Server";
            joinButton.Location = new Point(10, 10);
            joinButton.Width = 120;
            joinButton.Click += JoinButton_Click;
            bottomPanel.Controls.Add(joinButton);
            
            serverBrowserTab.Controls.Add(bottomPanel);
        }
        
        private void InitializeCreateServer()
        {
            int y = 20;
            
            // Server Name
            Label nameLabel = new Label();
            nameLabel.Text = "Server Name:";
            nameLabel.Location = new Point(20, y);
            nameLabel.AutoSize = true;
            createServerTab.Controls.Add(nameLabel);
            
            serverNameBox = new TextBox();
            serverNameBox.Location = new Point(150, y);
            serverNameBox.Width = 300;
            serverNameBox.Text = "My Server";
            createServerTab.Controls.Add(serverNameBox);
            
            y += 30;
            
            // Privacy Mode
            Label privacyLabel = new Label();
            privacyLabel.Text = "Privacy:";
            privacyLabel.Location = new Point(20, y);
            privacyLabel.AutoSize = true;
            createServerTab.Controls.Add(privacyLabel);
            
            privacyModeBox = new ComboBox();
            privacyModeBox.Location = new Point(150, y);
            privacyModeBox.Width = 200;
            privacyModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            privacyModeBox.Items.AddRange(new object[] { "Public", "Friends Only", "Invite Only", "Password Protected" });
            privacyModeBox.SelectedIndex = 0;
            privacyModeBox.SelectedIndexChanged += PrivacyModeBox_SelectedIndexChanged;
            createServerTab.Controls.Add(privacyModeBox);
            
            y += 30;
            
            // Password
            Label passwordLabel = new Label();
            passwordLabel.Text = "Password:";
            passwordLabel.Location = new Point(20, y);
            passwordLabel.AutoSize = true;
            createServerTab.Controls.Add(passwordLabel);
            
            passwordBox = new TextBox();
            passwordBox.Location = new Point(150, y);
            passwordBox.Width = 200;
            passwordBox.Enabled = false;
            passwordBox.UseSystemPasswordChar = true;
            createServerTab.Controls.Add(passwordBox);
            
            y += 30;
            
            // Max Players
            Label maxPlayersLabel = new Label();
            maxPlayersLabel.Text = "Max Players:";
            maxPlayersLabel.Location = new Point(20, y);
            maxPlayersLabel.AutoSize = true;
            createServerTab.Controls.Add(maxPlayersLabel);
            
            maxPlayersBox = new NumericUpDown();
            maxPlayersBox.Location = new Point(150, y);
            maxPlayersBox.Width = 100;
            maxPlayersBox.Minimum = 2;
            maxPlayersBox.Maximum = 32;
            maxPlayersBox.Value = 16;
            createServerTab.Controls.Add(maxPlayersBox);
            
            y += 30;
            
            // Game Mode
            Label gameModeLabel = new Label();
            gameModeLabel.Text = "Game Mode:";
            gameModeLabel.Location = new Point(20, y);
            gameModeLabel.AutoSize = true;
            createServerTab.Controls.Add(gameModeLabel);
            
            gameModeBox = new ComboBox();
            gameModeBox.Location = new Point(150, y);
            gameModeBox.Width = 200;
            gameModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            gameModeBox.Items.AddRange(new object[] { "Deathmatch", "Team Deathmatch", "Capture The Flag", "Survival", "Co-op" });
            gameModeBox.SelectedIndex = 0;
            createServerTab.Controls.Add(gameModeBox);
            
            y += 50;
            
            // Create Buttons
            createP2PButton = new Button();
            createP2PButton.Text = "Create P2P Server (Free)";
            createP2PButton.Location = new Point(20, y);
            createP2PButton.Width = 200;
            createP2PButton.Height = 40;
            createP2PButton.Click += CreateP2PButton_Click;
            createServerTab.Controls.Add(createP2PButton);
            
            createDedicatedButton = new Button();
            createDedicatedButton.Text = "Create Dedicated Server";
            createDedicatedButton.Location = new Point(240, y);
            createDedicatedButton.Width = 200;
            createDedicatedButton.Height = 40;
            createDedicatedButton.Click += CreateDedicatedButton_Click;
            createServerTab.Controls.Add(createDedicatedButton);
            
            y += 60;
            
            // Invite Code Label
            inviteCodeLabel = new Label();
            inviteCodeLabel.Text = "";
            inviteCodeLabel.Location = new Point(20, y);
            inviteCodeLabel.AutoSize = true;
            inviteCodeLabel.Font = new Font(inviteCodeLabel.Font.FontFamily, 12, FontStyle.Bold);
            inviteCodeLabel.ForeColor = Color.Green;
            createServerTab.Controls.Add(inviteCodeLabel);
        }
        
        private void InitializeSettings()
        {
            int y = 20;
            
            // Player Name
            Label playerNameLabel = new Label();
            playerNameLabel.Text = "Player Name:";
            playerNameLabel.Location = new Point(20, y);
            playerNameLabel.AutoSize = true;
            settingsTab.Controls.Add(playerNameLabel);
            
            playerNameBox = new TextBox();
            playerNameBox.Location = new Point(150, y);
            playerNameBox.Width = 300;
            playerNameBox.Text = "Player";
            settingsTab.Controls.Add(playerNameBox);
            
            y += 30;
            
            // Master Server URL
            Label masterServerLabel = new Label();
            masterServerLabel.Text = "Master Server:";
            masterServerLabel.Location = new Point(20, y);
            masterServerLabel.AutoSize = true;
            settingsTab.Controls.Add(masterServerLabel);
            
            masterServerUrlBox = new TextBox();
            masterServerUrlBox.Location = new Point(150, y);
            masterServerUrlBox.Width = 300;
            masterServerUrlBox.Text = masterServerUrl;
            settingsTab.Controls.Add(masterServerUrlBox);
            
            y += 30;
            
            // Steam Integration
            enableSteamBox = new CheckBox();
            enableSteamBox.Text = "Enable Steam Integration";
            enableSteamBox.Location = new Point(20, y);
            enableSteamBox.AutoSize = true;
            settingsTab.Controls.Add(enableSteamBox);
            
            y += 50;
            
            // Save Button
            Button saveButton = new Button();
            saveButton.Text = "Save Settings";
            saveButton.Location = new Point(20, y);
            saveButton.Width = 120;
            saveButton.Click += SaveButton_Click;
            settingsTab.Controls.Add(saveButton);
        }
        
        // Event Handlers
        
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            RefreshServerList();
        }
        
        private void QuickJoinButton_Click(object sender, EventArgs e)
        {
            // TODO: Implement quick join
            MessageBox.Show("Quick Join not implemented yet", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void JoinButton_Click(object sender, EventArgs e)
        {
            if (serverList.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a server", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            JoinSelectedServer();
        }
        
        private void ServerList_DoubleClick(object sender, EventArgs e)
        {
            JoinSelectedServer();
        }
        
        private void CreateP2PButton_Click(object sender, EventArgs e)
        {
            CreateP2PServer();
        }
        
        private void CreateDedicatedButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Dedicated server creation not implemented yet.\nUse Server Manager application.", 
                "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void PrivacyModeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            passwordBox.Enabled = (privacyModeBox.SelectedIndex == 3); // Password Protected
        }
        
        private void SaveButton_Click(object sender, EventArgs e)
        {
            masterServerUrl = masterServerUrlBox.Text;
            SaveSettings();
            MessageBox.Show("Settings saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (currentHost != null)
            {
                currentHost.Stop();
                currentHost.Dispose();
            }
            
            if (currentClient != null)
            {
                currentClient.Disconnect();
                currentClient.Dispose();
            }
        }
        
        // Methods
        
        private void RefreshServerList()
        {
            try
            {
                string url = masterServerUrl + "/api/servers";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 5000;
                
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string json = reader.ReadToEnd();
                    List<ServerEntry> servers = DeserializeFromJson<List<ServerEntry>>(json);
                    
                    serverList.Rows.Clear();
                    
                    foreach (ServerEntry server in servers)
                    {
                        string players = string.Format("{0}/{1}", server.CurrentPlayers, server.MaxPlayers);
                        string privacy = server.Privacy.ToString();
                        
                        serverList.Rows.Add(
                            server.ServerName,
                            server.GameMode,
                            server.MapName,
                            players,
                            server.Ping + " ms",
                            privacy
                        );
                        
                        serverList.Rows[serverList.Rows.Count - 1].Tag = server;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to refresh server list: " + ex.Message, "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void JoinSelectedServer()
        {
            if (serverList.SelectedRows.Count == 0)
                return;
                
            ServerEntry server = serverList.SelectedRows[0].Tag as ServerEntry;
            if (server == null)
                return;
                
            string ip = server.ExternalIP;
            int port = server.Port;
            
            // Если IP локальный, используем localhost
            if (ip == "::1" || ip == "0.0.0.0")
                ip = "127.0.0.1";

            string args = string.Format("+connect {0}:{1}", ip, port);
            
            // Если есть пароль
            if (server.HasPassword && !string.IsNullOrEmpty(passwordBox.Text))
            {
                args += string.Format(" +password \"{0}\"", passwordBox.Text);
            }

            LaunchGame(args);
        }

        private void LaunchGame(string arguments)
        {
            try
            {
                // Путь к игре (на 2 уровня выше от папки лаунчера)
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string gamePath = Path.GetFullPath(Path.Combine(basePath, "..\\Bin\\x64\\Sam2017.exe"));
                
                if (!File.Exists(gamePath))
                {
                    // Попытка найти в стандартном пути если запускаем из другой папки
                    gamePath = @"G:\SteamLibrary\steamapps\common\Serious Sam Fusion 2017\Bin\x64\Sam2017.exe";
                }

                if (!File.Exists(gamePath))
                {
                    MessageBox.Show("Game executable not found!\nExpected at: " + gamePath, 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
                psi.FileName = gamePath;
                psi.Arguments = arguments;
                psi.WorkingDirectory = Path.GetDirectoryName(gamePath);
                
                // Добавляем имя игрока
                if (!string.IsNullOrEmpty(playerNameBox.Text))
                {
                    psi.Arguments += string.Format(" +name \"{0}\"", playerNameBox.Text);
                }

                // Пропускаем интро для скорости
                psi.Arguments += " +skipintro 1";

                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to launch game: " + ex.Message, 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void CreateP2PServer()
        {
            if (currentHost != null)
            {
                MessageBox.Show("Server already running!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                currentHost = new P2PHost(0);
                currentHost.Settings.ServerName = serverNameBox.Text;
                currentHost.Settings.MaxPlayers = (int)maxPlayersBox.Value;
                currentHost.Settings.GameMode = gameModeBox.SelectedItem.ToString();
                
                // Privacy mode
                switch (privacyModeBox.SelectedIndex)
                {
                    case 0: currentHost.Settings.Privacy = P2PHost.PrivacyMode.Public; break;
                    case 1: currentHost.Settings.Privacy = P2PHost.PrivacyMode.FriendsOnly; break;
                    case 2: currentHost.Settings.Privacy = P2PHost.PrivacyMode.InviteOnly; break;
                    case 3: 
                        currentHost.Settings.Privacy = P2PHost.PrivacyMode.PasswordProtected;
                        currentHost.Settings.Password = passwordBox.Text;
                        break;
                }
                
                currentHost.OnLog += (msg) => Console.WriteLine(msg);
                
                if (currentHost.Start())
                {
                    MessageBox.Show(string.Format("P2P Server started on port {0}!\nLaunching game...", currentHost.Port), 
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Register on master server
                    RegisterServerOnMaster();
                    
                    createP2PButton.Text = "Stop Server";
                    createP2PButton.Click -= CreateP2PButton_Click;
                    createP2PButton.Click += StopP2PButton_Click;

                    // Запуск игры в режиме сервера
                    // +server 1 запускает выделенный сервер, но для P2P хостинга
                    // обычно используют просто запуск уровня и ожидание игроков.
                    // Для Fusion: +menu_NetGameMode "Deathmatch" +StartServer "LevelName"
                    
                    string mapName = "Levels/SeriousSamHD/JewelOfTheNile/b_sl_v_level_01.wld"; // Пример карты
                    string mode = currentHost.Settings.GameMode;
                    
                    // Формируем аргументы для хостинга
                    string hostArgs = string.Format("+wait 1 +server 1 +maxplayers {0}", 
                        currentHost.Settings.MaxPlayers);
                        
                    LaunchGame(hostArgs);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to create server: " + ex.Message, "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void StopP2PButton_Click(object sender, EventArgs e)
        {
            if (currentHost != null)
            {
                currentHost.Stop();
                currentHost.Dispose();
                currentHost = null;
                
                createP2PButton.Text = "Create P2P Server (Free)";
                createP2PButton.Click -= StopP2PButton_Click;
                createP2PButton.Click += CreateP2PButton_Click;
                
                inviteCodeLabel.Text = "";
                
                MessageBox.Show("Server stopped", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        private void RegisterServerOnMaster()
        {
            try
            {
                if (currentHost == null)
                    return;

                ServerEntry entry = new ServerEntry();
                entry.ServerId = Guid.NewGuid().ToString();
                entry.ServerName = currentHost.Settings.ServerName;
                entry.Port = currentHost.Port;
                entry.GameMode = currentHost.Settings.GameMode;
                entry.MapName = "Default";
                entry.MaxPlayers = currentHost.Settings.MaxPlayers;
                entry.CurrentPlayers = 0;
                entry.Privacy = (ServerEntry.PrivacyMode)currentHost.Settings.Privacy;
                entry.HasPassword = !string.IsNullOrEmpty(currentHost.Settings.Password);

                string json = SerializeToJson(entry);
                
                string url = masterServerUrl + "/api/servers";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";
                
                byte[] data = Encoding.UTF8.GetBytes(json);
                request.ContentLength = data.Length;
                
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine("Server registered: " + result);
                    
                    // Show invite code if needed
                    if (currentHost.Settings.Privacy == P2PHost.PrivacyMode.InviteOnly)
                    {
                        inviteCodeLabel.Text = "Invite Code: XXXX-XXXX (TODO: Get from master)";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to register server on master: " + ex.Message, 
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private void LoadSettings()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\SSFusionMultiplayer"))
                {
                    if (key != null)
                    {
                        string savedUrl = key.GetValue("MasterServerUrl") as string;
                        if (!string.IsNullOrEmpty(savedUrl))
                            masterServerUrl = savedUrl;
                            
                        string savedName = key.GetValue("PlayerName") as string;
                        if (!string.IsNullOrEmpty(savedName) && playerNameBox != null)
                            playerNameBox.Text = savedName;
                            
                        object steamEnabled = key.GetValue("SteamEnabled");
                        if (steamEnabled != null && enableSteamBox != null)
                            enableSteamBox.Checked = (int)steamEnabled == 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load settings: " + ex.Message);
            }
        }
        
        private void SaveSettings()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\SSFusionMultiplayer"))
                {
                    if (key != null)
                    {
                        key.SetValue("MasterServerUrl", masterServerUrl);
                        key.SetValue("PlayerName", playerNameBox.Text);
                        key.SetValue("SteamEnabled", enableSteamBox.Checked ? 1 : 0);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save settings: " + ex.Message, 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private string SerializeToJson(object obj)
        {
            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
                using (MemoryStream ms = new MemoryStream())
                {
                    serializer.WriteObject(ms, obj);
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            catch
            {
                return "{}";
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
    }
    
    /// <summary>
    /// Точка входа для Launcher
    /// </summary>
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
