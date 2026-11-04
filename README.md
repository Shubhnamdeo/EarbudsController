# 🎧 Earbuds Sound & ANC Controller (v1.0)

[![Release](https://img.shields.io/badge/release-v1.0.0-blue.svg)](https://github.com/Shubhnamdeo)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%2F%2011-0078D6.svg)](https://microsoft.com/windows)
[![Architecture](https://img.shields.io/badge/arch-x64-green.svg)]()
[![License](https://img.shields.io/badge/license-MIT-purple.svg)](LICENSE)

A lightweight, minimalistic Windows desktop application to control master volume and toggle **Game Mode**, **Normal Mode**, **Active Noise Cancellation (ANC)**, and **Ambient / Transparency** for Bluetooth earbuds and headphones.

---

## 🌟 Key Features

- **🎮 OnePlus Modes Front & Center:**
  - `⚪ Normal Audio Mode`: Balanced standard audio profile.
  - `🎮 Game Mode (Low Latency)`: Ultra-low latency audio sync (~47ms–94ms) optimized for gaming and video streaming.
- **🛡️ Noise Control for ANC Earbuds & Headphones:**
  - `🛡️ Active Noise Cancelling (ANC)`: Hardware noise reduction for supported models (Beats Studio Pro, Apple AirPods Pro/Max, Sony WH/WF-1000XM, Samsung Galaxy Buds, Bose).
  - `🍃 Ambient / Transparency`: External awareness pass-through mode.
  - Smart fallback with friendly pop-up explanations if a device hardware does not feature ANC microphones.
- **🔊 Zero-Latency Universal Audio Control:**
  - Directly binds to Windows Core Audio COM APIs (`IAudioEndpointVolume`, `IMMDeviceEnumerator`).
  - Master volume slider, percentage indicator (0–100%), and one-click mute.
  - **`⚖️ L/R Equal`**: One-click stereo channel balance reset tool.
- **🤖 Gemini AI Device Intelligence (With Toggle):**
  - Identifies device brand, model, and hardware capabilities automatically.
  - Includes a dedicated **`[ 🤖 AI Search: ON / OFF ]`** toggle so you can pause or disable AI network queries anytime and run 100% offline.
- **🪟 Fluent Minimizable Desktop UI:**
  - Spacious dark-mode window with custom title bar.
  - **`—` Minimize**: Smoothly minimizes to the Windows taskbar.
  - **`✕` Close**: Cleanly shuts down the process so no background tasks linger.
- **📦 Ultra-Lightweight & Portable:**
  - Single standalone `.exe` (~48 KB).
  - Zero installer required, zero third-party runtimes, no Electron bloat (~20 MB RAM footprint).

---

## 🚀 Quick Start

1. Download **`EarbudsController.exe`**.
2. Double-click **`EarbudsController.exe`** (or `Restart_EarbudsController.bat`).
3. Click **`🔍 Search / Scan Devices`** to detect your connected Bluetooth earbuds.
4. Select your sound mode (`Normal`, `Game Mode`, `ANC`, or `Ambient`) and adjust volume!

---

## 🛠️ Building from Source

This project uses Windows' built-in C# compiler (`csc.exe`) included with every modern Windows installation. No Visual Studio installation or heavy SDKs required.

Simply double-click:
```cmd
build.bat
```
or run:
```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /t:winexe /optimize+ ^
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
    /r:System.Web.Extensions.dll /r:System.Net.Http.dll ^
    /out:EarbudsController.exe *.cs
```

---

## ⚙️ AI Configuration (Optional)

To enable Gemini AI identification:
1. Click the **⚙️ Settings** icon in the title bar.
2. Enter your free Gemini API key from [Google AI Studio](https://aistudio.google.com/).
3. Click **Save** (saved locally in your private `gemini_config.json`).
*Note: The app works 100% offline with built-in detection even without an API key.*

---

## 📄 License

This project is licensed under the [MIT License](LICENSE) - created by [Shubh Namdeo](https://github.com/Shubhnamdeo).
