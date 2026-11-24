# 🎧 Earbuds Sound & ANC Controller (v1.0)

[![Release](https://img.shields.io/badge/release-v1.0.0-blue.svg)](https://github.com/Shubhnamdeo/EarbudsController)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%2F%2011-0078D6.svg)](https://microsoft.com/windows)
[![Architecture](https://img.shields.io/badge/arch-x64-green.svg)]()
[![License](https://img.shields.io/badge/license-MIT-purple.svg)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/Shubhnamdeo/EarbudsController/pulls)

A high-performance, ultra-lightweight Windows desktop application to control master volume, monitor real-time battery, and toggle **Game Mode**, **Normal Mode**, **Active Noise Cancellation (ANC)**, and **Ambient / Transparency** for Bluetooth earbuds and headphones.

---

## 🌟 Key Features

- **🎮 OnePlus Modes Front & Center:**
  - `⚪ Normal Audio Mode`: Balanced standard audio profile.
  - `🎮 Game Mode (Low Latency)`: Ultra-low latency audio sync (~47ms–94ms) with Windows Multimedia Class Scheduler (MMCSS) real-time prioritization for gaming and video streaming.
- **🔋 Live Battery Percentage Reporting:**
  - Real-time battery indicator (`🔋 60%`) in the header via Windows SetupAPI (`DEVPKEY_Device_BatteryLevel`).
  - Automatically updates without battery drain or external driver dependencies.
- **🛡️ Noise Control for ANC Earbuds & Headphones:**
  - `🛡️ Active Noise Cancelling (ANC)`: Hardware noise reduction for supported models (Beats Studio Pro, Apple AirPods Pro/Max, Sony WH/WF-1000XM, Samsung Galaxy Buds, Bose).
  - `🍃 Ambient / Transparency`: External awareness pass-through mode.
  - Smart fallback with friendly pop-up explanations if a device hardware does not feature ANC microphones.
- **🔊 Zero-Latency Universal Audio Control:**
  - Directly binds to Windows Core Audio COM APIs (`IAudioEndpointVolume`, `IMMDeviceEnumerator`).
  - Master volume slider, percentage indicator (0–100%), and one-click mute.
  - **`⚖️ L/R Equal`**: One-click stereo channel balance reset tool to restore equal balance if one earbud drifts.
- **🤖 Gemini AI Device Intelligence (With Toggle):**
  - Identifies device brand, model, and hardware capabilities automatically.
  - Includes a dedicated **`[ 🤖 AI Search: ON / OFF ]`** toggle so you can pause or disable AI network queries anytime and run 100% offline.
- **🪟 Fluent Minimizable Desktop UI:**
  - Spacious dark-mode window with custom title bar.
  - **`—` Minimize**: Smoothly minimizes to the Windows taskbar.
  - **`✕` Close**: Cleanly shuts down the process so no background tasks linger.
- **📦 Ultra-Lightweight & Portable:**
  - Single standalone `.exe` (~51 KB).
  - Zero installer required, zero third-party runtimes, no Electron bloat (~20 MB RAM footprint).

---

## 🚀 Quick Start

1. Download **`EarbudsController.exe`**.
2. Double-click **`EarbudsController.exe`** (or run `Restart_EarbudsController.bat`).
3. Click **`🔍 Search / Scan Devices`** to detect your connected Bluetooth earbuds.
4. Select your sound mode (`Normal`, `Game Mode`, `ANC`, or `Ambient`) and adjust volume!

---

## 🗺️ Architecture & Engineering Plan

For a complete breakdown of how the application interfaces with Windows audio endpoints, reads battery levels, and schedules low-latency game audio, see the **[Architecture & Engineering Plan](PLAN_AND_ROADMAP.md)**.

```
┌─────────────────────────────────────────────────────────────┐
│                 MainFlyoutForm (Fluent UI)                   │
│   • Drag/Drop Title Bar   • Minimize / Exit                 │
│   • Real-Time Battery     • Volume & Channel Balance Reset  │
│   • Mode Toggles (Normal, Game Mode, ANC, Ambient)          │
└──────────────┬──────────────────────────────┬───────────────┘
               │                              │
┌──────────────▼─────────────┐ ┌──────────────▼───────────────┐
│     AudioEngine.cs         │ │      BatteryService.cs        │
│ • Windows Core Audio COM   │ │ • Windows SetupAPI Enum      │
│ • IMMDeviceEnumerator      │ │ • DEVPKEY_Device_BatteryLevel│
│ • IAudioEndpointVolume     │ │ • Real-Time Percentage Read  │
│ • IPropertyStore (BTHENUM) │ └───────────────────────────────┘
└──────────────┬─────────────┘
               │
┌──────────────▼──────────────────────────────┐
│       BluetoothProtocolManager.cs           │
│ • Windows MMCSS Audio Prioritization (avrt) │
│ • Hardware Mode Mapping (ANC/Ambient/Game)  │
│ • Target-Device Protocol Dispatch           │
└──────────────┬──────────────────────────────┘
               │
┌──────────────▼──────────────────────────────┐
│            GeminiService.cs                 │
│ • AI-Powered Hardware Capability Discovery  │
│ • Local Heuristic Database Fallback         │
│ • Offline Mode & Local Cache Management     │
└─────────────────────────────────────────────┘
```

---

## 🤝 Call for Contributors — Help Solve Open Hardware Challenges!

We believe Windows users deserve a seamless, manufacturer-agnostic audio controller for all wireless earbuds. We are actively inviting the open-source community, reverse engineers, and audio developers to join us!

### 🎯 Open Challenges & Good First Issues:
1. **OnePlus / Oppo BLE Protocol (`oppointeraction`):**
   - Reverse engineer and dispatch the exact over-the-air BLE GATT packets for OnePlus Nord Buds / Buds Pro (HeyMelody protocol).
   - Test Low Latency Game Mode packets (`0x55, 0x0C, 0x01` / `0x00`) over WinRT GATT.
2. **Apple / Beats Accessory Protocol (AAP):**
   - Implement over-the-air packet switching for ANC and Transparency mode on Beats Studio Pro and AirPods.
3. **Sony MDR Protocol (SPP / RFCOMM):**
   - Implement Sony headphone communication over RFCOMM UUID `00001101-0000-1000-8000-00805F9B34FB`.
4. **Samsung Galaxy Buds Manager:**
   - Integrate Galaxy Buds RFCOMM protocol for Ambient and ANC toggle.
5. **Modern Fluent UI Enhancements:**
   - Acrylic blur / Mica backdrop effects and customizable global hotkeys.

### How to Contribute:
1. **Fork this repository** to your GitHub account.
2. **Clone** your fork locally:
   ```bash
   git clone https://github.com/<your-username>/EarbudsController.git
   ```
3. Make your changes and test using `build.bat`.
4. Submit a **Pull Request (PR)** with a description of the earbuds tested and the feature added.

Every contribution, whether code, bug reports, or Bluetooth protocol captures, is deeply appreciated!

---

## 🛠️ Building from Source

This project uses Windows' built-in C# compiler (`csc.exe`) included with every modern Windows installation. No Visual Studio installation or heavy SDKs required.

Simply double-click:
```cmd
build.bat
```
or compile directly in PowerShell / Command Prompt:
```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /t:winexe /optimize+ ^
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
    /r:System.Web.Extensions.dll /r:System.Net.Http.dll ^
    /out:EarbudsController.exe *.cs
```

---

## ⚙️ AI Configuration (Optional)

To enable Gemini AI capability discovery:
1. Click the **⚙️ Settings** icon in the title bar.
2. Enter your free Gemini API key from [Google AI Studio](https://aistudio.google.com/).
3. Click **Save** (saved locally in your private `gemini_config.json`).
*Note: The app works 100% offline with built-in detection even without an API key.*

---

## 📄 License

This project is licensed under the [MIT License](LICENSE) - created by [Shubh Namdeo](https://github.com/Shubhnamdeo).
