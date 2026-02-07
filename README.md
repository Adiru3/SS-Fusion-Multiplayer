# SS Fusion Multiplayer

A comprehensive multiplayer system implementation for **Serious Sam Fusion 2017**, featuring a custom Master Server, Relay Server for NAT traversal, and a standalone Server Browser Launcher.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)
![Status](https://img.shields.io/badge/status-Stable-green.svg)

## 🌟 Features

*   **P2P Multiplayer**: Play with friends for free using Peer-to-Peer connections.
*   **NAT Traversal**: Built-in STUN and Relay server support to bypass strict NATs.
*   **Server Browser**: Custom Launcher with a full-featured server list.
*   **Master Server**: Centralized REST API for server registration and discovery.
*   **Privacy Options**: Public, Friends Only, Invite Only, and Password-protected servers.
*   **Persistent Settings**: Saves player name and preferences to the Windows Registry.

## 📂 Project Structure

The project is organized into two main parts:

*   **Client**: The Launcher application for players.
*   **Server**: Infrastructure components (Master Server, Relay Server).

```
SSFusionMultiplayer/
├── Client/           # Launcher Source Code
├── Server/           # MasterServer & Relay Source Code
├── Core/             # Shared Networking Library (SSFusionNet)
└── Bin/              # Compiled Output
    ├── Client/       # Launcher.exe + DLL
    └── Server/       # Servers + DLL
```

## 🚀 Getting Started

### Prerequisites

*   Windows OS
*   .NET Framework 4.0 or higher
*   Serious Sam Fusion 2017 (installed)

### Installation

1.  **Clone the repository** (or download source).
2.  Navigate to the `SSFusionMultiplayer` folder.
3.  Run `build.bat` to compile all components.

### Usage

#### 🎮 For Players (Client)

1.  Navigate to `Bin\Client`.
2.  Run `Launcher.exe`.
3.  Go to **Settings** and set your **Player Name**.
4.  Use **Server Browser** to find games or **Create Server** to host your own.

#### 🖥️ For Hosters (Infrastructure)

If you want to run your own Master Server (default uses localhost):

1.  Navigate to `Bin\Server`.
2.  Run `MasterServer.exe` (requires Admin rights for port binding or URL registration).
    *   *Tip: Run `register_url.bat` once as Admin to avoid running the server as Admin.*
3.  Update the **Master Server URL** in your Launcher settings.

## 🔧 Technical Details

*   **Communication**: UDP is used for game traffic and NAT traversal. HTTP is used for Master Server API.
*   **Ports**:
    *   `8000`: Master Server (HTTP)
    *   `9000`: Relay Server (UDP)
    *   Dynamic: P2P Game Sessions (UDP)
*   **Configuration**: Stored in Registry at `HKCU\Software\SSFusionMultiplayer`.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1.  Fork the Project
2.  Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3.  Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4.  Push to the Branch (`git push origin feature/AmazingFeature`)
5.  Open a Pull Request

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.

---

## 🌍 Support & Credits

Engine developed by **Adiru3** and the Open Source Community.

[![Donate](https://img.shields.io/badge/Donate-adiru3.github.io-FF0000?style=for-the-badge)](https://adiru3.github.io/Donate/)
[![GitHub](https://img.shields.io/badge/GitHub-Adiru3-181717?style=for-the-badge&logo=github)](https://github.com/adiru3)

---

---
*Built with ❤️ for the Serious Sam community.*


