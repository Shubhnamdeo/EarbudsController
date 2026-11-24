using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EarbudsController
{
    public class MainFlyoutForm : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private readonly AudioEngine _audioEngine;
        private readonly GeminiService _geminiService;
        private readonly BluetoothProtocolManager _protocolManager;
        private readonly BatteryService _batteryService = new BatteryService();
        private int _batteryPollCounter = 0;

        private Label _lblDeviceTitle;
        private Label _lblBatteryBadge;
        private Label _lblDeviceSub;
        private ComboBox _comboDevices;
        private Button _btnSearchDevices;
        private Label _lblVolumePercent;
        private TrackBar _sliderVolume;
        private Button _btnMute;
        private Panel _cardAiInfo;
        private Label _lblAiTitle;
        private Label _lblAiDesc;
        private Label _lblAiSource;
        private Button _btnAiToggle;
        private Button _btnReidentify;
        private Panel _panelModes;
        private Button _btnModeOff;
        private Button _btnModeGame;
        private Button _btnModeAnc;
        private Button _btnModeAmbient;
        private Label _lblNoAncNote;
        private Button _btnRefresh;
        private Button _btnSettings;
        private Button _btnMinimize;
        private Button _btnClose;
        private Timer _pollTimer;

        private List<AudioDevice> _allDevices = new List<AudioDevice>();
        private AudioDevice _selectedDevice;
        private DeviceProfile _currentProfile;
        private bool _isUpdatingUi = false;

        public MainFlyoutForm(AudioEngine audio, GeminiService gemini, BluetoothProtocolManager proto)
        {
            _audioEngine = audio;
            _geminiService = gemini;
            _protocolManager = proto;

            InitializeComponent();
            ScanDevices();

            _pollTimer = new Timer();
            _pollTimer.Interval = 800;
            _pollTimer.Tick += (s, e) =>
            {
                SyncVolumeFromSystem();
                _batteryPollCounter++;
                if (_batteryPollCounter >= 6)
                {
                    _batteryPollCounter = 0;
                    UpdateBatteryDisplay();
                }
            };
            _pollTimer.Start();
        }

        private void InitializeComponent()
        {
            // Large, spacious, minimizable window
            this.Text = "Earbuds Sound & ANC Controller";
            this.Size = new Size(480, 650);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(20, 20, 24); // Fluent Dark
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // Truly exit on close
            this.FormClosing += (s, e) =>
            {
                try
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "formclosing.log"),
                        "FormClosing reason=" + e.CloseReason + "\nStack:\n" + Environment.StackTrace);
                }
                catch { }
                Application.Exit();
            };

            // 1. --- TITLE BAR (Draggable, Minimizable, Closable) ---
            var panelHeader = new Panel
            {
                Location = new Point(16, 12),
                Size = new Size(448, 46),
                BackColor = Color.Transparent
            };
            panelHeader.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };

            var picIcon = new Label
            {
                Text = "🎧",
                Font = new Font("Segoe UI Emoji", 16F),
                Location = new Point(0, 4),
                Size = new Size(34, 34),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelHeader.Controls.Add(picIcon);

            _lblDeviceTitle = new Label
            {
                Text = "Earbuds Controller",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(38, 4),
                Size = new Size(196, 22),
                AutoEllipsis = true,
                ForeColor = Color.White
            };
            panelHeader.Controls.Add(_lblDeviceTitle);

            _lblBatteryBadge = new Label
            {
                Text = "🔋 --%",
                Font = new Font("Segoe UI", 8.2F, FontStyle.Bold),
                Location = new Point(236, 5),
                Size = new Size(72, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(36, 40, 52),
                ForeColor = Color.FromArgb(74, 222, 128),
                Visible = false
            };
            _lblBatteryBadge.Paint += (s, pe) => DrawRoundedBorder(pe.Graphics, _lblBatteryBadge.ClientRectangle, 4, Color.FromArgb(65, 75, 95));
            panelHeader.Controls.Add(_lblBatteryBadge);

            _lblDeviceSub = new Label
            {
                Text = "OnePlus Game Mode, Sound Modes & Volume",
                Font = new Font("Segoe UI", 8.2F),
                Location = new Point(38, 26),
                Size = new Size(270, 16),
                ForeColor = Color.FromArgb(161, 161, 170)
            };
            panelHeader.Controls.Add(_lblDeviceSub);

            _btnRefresh = CreateIconButton("🔄", 314, 8);
            _btnRefresh.Click += (s, e) => ScanDevices();
            panelHeader.Controls.Add(_btnRefresh);

            _btnSettings = CreateIconButton("⚙️", 348, 8);
            _btnSettings.Click += (s, e) => ShowSettingsDialog();
            panelHeader.Controls.Add(_btnSettings);

            // Minimize Button: Minimizes window to taskbar
            _btnMinimize = CreateIconButton("—", 382, 8);
            _btnMinimize.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            panelHeader.Controls.Add(_btnMinimize);

            // Close Button: Exits app
            _btnClose = CreateIconButton("✕", 416, 8);
            _btnClose.Click += (s, e) => Application.Exit();
            panelHeader.Controls.Add(_btnClose);

            this.Controls.Add(panelHeader);

            // 2. --- CONNECTED AUDIO DEVICE CARD ---
            var panelDeviceSection = new Panel
            {
                Location = new Point(16, 64),
                Size = new Size(448, 88),
                BackColor = Color.FromArgb(28, 28, 34)
            };
            panelDeviceSection.Paint += (s, pe) => DrawRoundedBorder(pe.Graphics, panelDeviceSection.ClientRectangle, 8, Color.FromArgb(45, 45, 55));

            var lblDevSectionTitle = new Label
            {
                Text = "Connected Audio Device",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(12, 8),
                AutoSize = true,
                ForeColor = Color.FromArgb(212, 212, 216)
            };
            panelDeviceSection.Controls.Add(lblDevSectionTitle);

            _comboDevices = new ComboBox
            {
                Location = new Point(12, 26),
                Size = new Size(424, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(38, 38, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            _comboDevices.SelectedIndexChanged += (s, e) => OnDeviceSelectionChanged();
            panelDeviceSection.Controls.Add(_comboDevices);

            _btnSearchDevices = new Button
            {
                Text = "🔍 Search / Scan Devices",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(12, 54),
                Size = new Size(424, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(79, 70, 229), // Indigo
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnSearchDevices.FlatAppearance.BorderSize = 0;
            _btnSearchDevices.Click += (s, e) => ScanDevices();
            panelDeviceSection.Controls.Add(_btnSearchDevices);

            this.Controls.Add(panelDeviceSection);

            // 3. --- MASTER VOLUME CARD ---
            var panelVolume = new Panel
            {
                Location = new Point(16, 158),
                Size = new Size(448, 76),
                BackColor = Color.FromArgb(28, 28, 34)
            };
            panelVolume.Paint += (s, pe) => DrawRoundedBorder(pe.Graphics, panelVolume.ClientRectangle, 8, Color.FromArgb(45, 45, 55));

            var lblVolTitle = new Label
            {
                Text = "Master Volume",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(12, 8),
                AutoSize = true,
                ForeColor = Color.FromArgb(212, 212, 216)
            };
            panelVolume.Controls.Add(lblVolTitle);

            _lblVolumePercent = new Label
            {
                Text = "100%",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Location = new Point(360, 6),
                Size = new Size(76, 20),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(129, 140, 248)
            };
            panelVolume.Controls.Add(_lblVolumePercent);

            _btnMute = new Button
            {
                Text = "🔊",
                Font = new Font("Segoe UI Emoji", 11F),
                Location = new Point(12, 34),
                Size = new Size(36, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 40, 50),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnMute.FlatAppearance.BorderSize = 0;
            _btnMute.Click += (s, e) => ToggleMute();
            panelVolume.Controls.Add(_btnMute);

            _sliderVolume = new TrackBar
            {
                Location = new Point(54, 34),
                Size = new Size(268, 30),
                Minimum = 0,
                Maximum = 100,
                TickStyle = TickStyle.None,
                Cursor = Cursors.Hand
            };
            _sliderVolume.Scroll += (s, e) => OnSliderVolumeChanged();
            panelVolume.Controls.Add(_sliderVolume);

            // Channel Balance Reset Button
            var btnBalance = new Button
            {
                Text = "⚖️ L/R Equal",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Location = new Point(330, 34),
                Size = new Size(106, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(36, 36, 46),
                ForeColor = Color.FromArgb(212, 212, 220),
                Cursor = Cursors.Hand
            };
            btnBalance.FlatAppearance.BorderSize = 1;
            btnBalance.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 85);
            btnBalance.Click += (s, e) =>
            {
                _audioEngine.ResetChannelBalance(_selectedDevice != null ? _selectedDevice.Id : null);
                MessageBox.Show("Windows Left and Right audio channels have been reset to 100% equal balance.\n\nTroubleshooting Single Earbud Audio:\nIf one earbud is still silent, place both earbuds in the charging case for 5 seconds and take them out together to re-sync the stereo pair.",
                    "Audio Balance Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            panelVolume.Controls.Add(btnBalance);

            this.Controls.Add(panelVolume);

            // 4. --- SOUND MODES SECTION (OnePlus Normal/Game Mode & ANC) ---
            _panelModes = new Panel
            {
                Location = new Point(16, 240),
                Size = new Size(448, 200),
                BackColor = Color.FromArgb(28, 28, 34)
            };
            _panelModes.Paint += (s, pe) => DrawRoundedBorder(pe.Graphics, _panelModes.ClientRectangle, 8, Color.FromArgb(45, 45, 55));

            var lblModesTitle = new Label
            {
                Text = "Earbud Sound Modes & Controls",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(12, 8),
                AutoSize = true,
                ForeColor = Color.FromArgb(228, 228, 231)
            };
            _panelModes.Controls.Add(lblModesTitle);

            // OnePlus Modes Label (Row 1)
            var lblOnePlusHeader = new Label
            {
                Text = "OnePlus Modes (Supported on OnePlus Nord Buds & Earbuds):",
                Font = new Font("Segoe UI", 7.8F, FontStyle.Bold),
                Location = new Point(12, 28),
                Size = new Size(424, 16),
                ForeColor = Color.FromArgb(251, 191, 36) // Gold
            };
            _panelModes.Controls.Add(lblOnePlusHeader);

            _btnModeOff = CreateLargeModeButton("⚪ Normal Audio Mode", 12, 46, 208);
            _btnModeOff.Click += (s, e) => SetSoundMode(SoundMode.Off);
            _panelModes.Controls.Add(_btnModeOff);

            _btnModeGame = CreateLargeModeButton("🎮 Game Mode (Low Latency)", 228, 46, 208);
            _btnModeGame.Click += (s, e) => SetSoundMode(SoundMode.GameMode);
            _panelModes.Controls.Add(_btnModeGame);

            // ANC Modes Label (Row 2)
            var lblAncHeader = new Label
            {
                Text = "Noise Cancellation (Beats Studio Pro, AirPods, Sony):",
                Font = new Font("Segoe UI", 7.8F, FontStyle.Bold),
                Location = new Point(12, 94),
                Size = new Size(424, 16),
                ForeColor = Color.FromArgb(165, 180, 252) // Light Indigo
            };
            _panelModes.Controls.Add(lblAncHeader);

            _btnModeAnc = CreateLargeModeButton("🛡️ Active Noise Cancelling", 12, 112, 208);
            _btnModeAnc.Click += (s, e) => SetSoundMode(SoundMode.NoiseCancelling);
            _panelModes.Controls.Add(_btnModeAnc);

            _btnModeAmbient = CreateLargeModeButton("🍃 Ambient Sound", 228, 112, 208);
            _btnModeAmbient.Click += (s, e) => SetSoundMode(SoundMode.Ambient);
            _panelModes.Controls.Add(_btnModeAmbient);

            // Status feedback banner
            _lblNoAncNote = new Label
            {
                Text = "Select your earbuds and click any mode to activate.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(12, 162),
                Size = new Size(424, 28),
                ForeColor = Color.FromArgb(161, 161, 170)
            };
            _panelModes.Controls.Add(_lblNoAncNote);

            this.Controls.Add(_panelModes);

            // 5. --- AI INSIGHT CARD & AI CONTROL BUTTON ---
            _cardAiInfo = new Panel
            {
                Location = new Point(16, 448),
                Size = new Size(448, 172),
                BackColor = Color.FromArgb(24, 24, 30)
            };
            _cardAiInfo.Paint += (s, pe) => DrawRoundedBorder(pe.Graphics, _cardAiInfo.ClientRectangle, 8, Color.FromArgb(45, 45, 55));

            _lblAiTitle = new Label
            {
                Text = "AI Device Intelligence",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(12, 8),
                Size = new Size(180, 16),
                ForeColor = Color.FromArgb(167, 139, 250)
            };
            _cardAiInfo.Controls.Add(_lblAiTitle);

            _lblAiSource = new Label
            {
                Text = "Gemini",
                Font = new Font("Segoe UI", 7.5F),
                Location = new Point(240, 8),
                Size = new Size(196, 16),
                TextAlign = ContentAlignment.TopRight,
                ForeColor = Color.FromArgb(147, 197, 253)
            };
            _cardAiInfo.Controls.Add(_lblAiSource);

            // Stop/Start AI button
            _btnAiToggle = new Button
            {
                Location = new Point(12, 28),
                Size = new Size(424, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnAiToggle.FlatAppearance.BorderSize = 0;
            _btnAiToggle.Click += (s, e) => ToggleAiSearch();
            UpdateAiToggleButton();
            _cardAiInfo.Controls.Add(_btnAiToggle);

            _lblAiDesc = new Label
            {
                Text = "Detecting earbud capabilities...",
                Font = new Font("Segoe UI", 8F),
                Location = new Point(12, 60),
                Size = new Size(424, 66),
                ForeColor = Color.FromArgb(212, 212, 216)
            };
            _cardAiInfo.Controls.Add(_lblAiDesc);

            _btnReidentify = new Button
            {
                Text = "⚡ Re-Analyze Device with AI",
                Font = new Font("Segoe UI", 8F),
                Location = new Point(12, 134),
                Size = new Size(424, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(36, 36, 46),
                ForeColor = Color.FromArgb(200, 200, 210),
                Cursor = Cursors.Hand
            };
            _btnReidentify.FlatAppearance.BorderSize = 0;
            _btnReidentify.Click += (s, e) => ReidentifyCurrentDevice();
            _cardAiInfo.Controls.Add(_btnReidentify);

            this.Controls.Add(_cardAiInfo);
        }

        private void UpdateAiToggleButton()
        {
            if (_geminiService.AiEnabled)
            {
                _btnAiToggle.Text = "🤖 AI Search: ACTIVE (Click to Stop/Disable)";
                _btnAiToggle.BackColor = Color.FromArgb(67, 56, 202);
                _btnAiToggle.ForeColor = Color.White;
            }
            else
            {
                _btnAiToggle.Text = "⏹️ AI Search: DISABLED (Offline Mode - Click to Enable)";
                _btnAiToggle.BackColor = Color.FromArgb(55, 65, 81);
                _btnAiToggle.ForeColor = Color.FromArgb(209, 213, 219);
            }
        }

        private void ToggleAiSearch()
        {
            _geminiService.AiEnabled = !_geminiService.AiEnabled;
            UpdateAiToggleButton();

            if (_selectedDevice != null)
            {
                TriggerDeviceIdentification(_selectedDevice);
            }
        }

        private void ReidentifyCurrentDevice()
        {
            _geminiService.ClearCache();
            if (_selectedDevice != null)
            {
                TriggerDeviceIdentification(_selectedDevice);
            }
        }

        private Button CreateIconButton(string text, int x, int y)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI Emoji", 10F),
                Location = new Point(x, y),
                Size = new Size(28, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(32, 32, 36),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CreateLargeModeButton(string text, int x, int y, int width)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(x, y),
                Size = new Size(width, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(36, 36, 46),
                ForeColor = Color.White,
                Enabled = true, // ALWAYS CLICKABLE
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 85);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 75);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(85, 85, 110);
            return btn;
        }

        public void RefreshState()
        {
            ScanDevices();
        }

        public void ScanDevices()
        {
            _btnSearchDevices.Text = "⏳ Searching Devices...";
            _btnSearchDevices.Enabled = false;

            try
            {
                _allDevices = _audioEngine.GetAllOutputDevices();

                _comboDevices.Items.Clear();

                int bluetoothIndex = -1;
                for (int i = 0; i < _allDevices.Count; i++)
                {
                    var dev = _allDevices[i];
                    string prefix = dev.IsBluetooth ? "🎧 " : "🔊 ";
                    string itemText = prefix + dev.CleanName + (dev.IsBluetooth ? " (Bluetooth)" : "");
                    _comboDevices.Items.Add(itemText);

                    if (dev.IsBluetooth && bluetoothIndex == -1)
                    {
                        bluetoothIndex = i;
                    }
                }

                if (_allDevices.Count > 0)
                {
                    int selectIdx = (bluetoothIndex != -1) ? bluetoothIndex : 0;
                    _comboDevices.SelectedIndex = selectIdx;
                }
                else
                {
                    _lblDeviceTitle.Text = "No Audio Devices Found";
                    _lblDeviceSub.Text = "Please connect earbuds or headphones";
                }
            }
            finally
            {
                _btnSearchDevices.Text = "🔍 Search / Scan Devices";
                _btnSearchDevices.Enabled = true;
            }
        }

        private void OnDeviceSelectionChanged()
        {
            int idx = _comboDevices.SelectedIndex;
            if (idx >= 0 && idx < _allDevices.Count)
            {
                _selectedDevice = _allDevices[idx];
                _lblDeviceTitle.Text = _selectedDevice.CleanName;
                _lblDeviceSub.Text = (_selectedDevice.IsBluetooth ? "Bluetooth Earbuds" : "System Audio") + " • Connected";

                UpdateBatteryDisplay();
                TriggerDeviceIdentification(_selectedDevice);
                SyncVolumeFromSystem();
            }
        }

        private void UpdateBatteryDisplay()
        {
            if (_selectedDevice == null || !_selectedDevice.IsBluetooth)
            {
                _lblBatteryBadge.Visible = false;
                if (_selectedDevice != null)
                {
                    _lblDeviceSub.Text = (_selectedDevice.IsBluetooth ? "Bluetooth Earbuds" : "System Audio") + " • Connected";
                }
                return;
            }

            int battery = _batteryService.GetBatteryPercentage(_selectedDevice.CleanName);
            if (battery >= 0)
            {
                _lblBatteryBadge.Text = "🔋 " + battery + "%";
                _lblBatteryBadge.ForeColor = (battery > 40) ? Color.FromArgb(74, 222, 128) :
                                             (battery > 20) ? Color.FromArgb(251, 191, 36) :
                                             Color.FromArgb(248, 113, 113);
                _lblBatteryBadge.Visible = true;
                _lblDeviceSub.Text = "Bluetooth Earbuds • 🔋 " + battery + "% Battery • Connected";
            }
            else
            {
                _lblBatteryBadge.Visible = false;
                _lblDeviceSub.Text = "Bluetooth Earbuds • Connected";
            }
        }

        private void TriggerDeviceIdentification(AudioDevice dev)
        {
            if (dev == null) return;
            _lblAiDesc.Text = _geminiService.AiEnabled ? "Analyzing device with AI..." : "Local mode active.";

            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    _currentProfile = _geminiService.IdentifyDevice(dev.CleanName, dev.IsBluetooth);
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.BeginInvoke((MethodInvoker)UpdateProfileUi);
                    }
                }
                catch { }
            });
        }

        private void UpdateProfileUi()
        {
            if (_currentProfile != null)
            {
                _lblAiDesc.Text = _currentProfile.Description ?? "Audio device recognized.";
                _lblAiSource.Text = _currentProfile.Source ?? "";
            }

            // Ensure all buttons remain ALWAYS 100% clickable
            _btnModeOff.Enabled = true;
            _btnModeGame.Enabled = true;
            _btnModeAnc.Enabled = true;
            _btnModeAmbient.Enabled = true;

            HighlightActiveModeButton(_protocolManager.CurrentMode);
        }

        private void SyncVolumeFromSystem()
        {
            if (_isUpdatingUi || _selectedDevice == null) return;

            float vol = _audioEngine.GetMasterVolume(_selectedDevice.Id);
            bool mute = _audioEngine.GetMute(_selectedDevice.Id);

            int volPct = (int)Math.Round(vol * 100f);

            _sliderVolume.Value = Math.Max(0, Math.Min(100, volPct));
            _lblVolumePercent.Text = volPct + "%";
            _btnMute.Text = (mute || volPct == 0) ? "🔇" : "🔊";
        }

        private void OnSliderVolumeChanged()
        {
            if (_selectedDevice == null) return;
            _isUpdatingUi = true;
            try
            {
                float level = _sliderVolume.Value / 100f;
                _audioEngine.SetMasterVolume(level, _selectedDevice.Id);
                _lblVolumePercent.Text = _sliderVolume.Value + "%";
                _btnMute.Text = _sliderVolume.Value == 0 ? "🔇" : "🔊";
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void ToggleMute()
        {
            if (_selectedDevice == null) return;
            bool currentMute = _audioEngine.GetMute(_selectedDevice.Id);
            bool newMute = !currentMute;
            _audioEngine.SetMute(newMute, _selectedDevice.Id);
            _btnMute.Text = newMute ? "🔇" : "🔊";
        }

        private void SetSoundMode(SoundMode mode)
        {
            if (!_protocolManager.IsModeSupported(_currentProfile, mode))
            {
                string devName = (_selectedDevice != null) ? _selectedDevice.CleanName : "this device";
                MessageBox.Show(
                    string.Format("'{0}' is not supported by {1}.\n\nYour earbuds hardware supports:\n• ⚪ Normal Audio Mode\n• 🎮 Game Mode (Low Latency)\n\n(Active Noise Cancellation & Ambient Sound require hardware ANC microphones)",
                        _protocolManager.GetModeName(mode), devName),
                    "Mode Not Supported",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch { }

            string message;
            _protocolManager.SwitchMode(_currentProfile, mode, out message);
            HighlightActiveModeButton(mode);

            _lblNoAncNote.Text = message;
            _lblNoAncNote.ForeColor = (mode == SoundMode.GameMode) ? Color.FromArgb(251, 191, 36) :
                                      (mode == SoundMode.NoiseCancelling) ? Color.FromArgb(165, 180, 252) :
                                      (mode == SoundMode.Ambient) ? Color.FromArgb(125, 211, 252) :
                                      Color.FromArgb(209, 213, 219);
        }

        private void HighlightActiveModeButton(SoundMode mode)
        {
            Color activeOff = Color.FromArgb(75, 85, 99);       // Gray
            Color activeGame = Color.FromArgb(234, 88, 12);     // Vibrant Orange
            Color activeAnc = Color.FromArgb(79, 70, 229);      // Vibrant Indigo
            Color activeAmbient = Color.FromArgb(14, 165, 233);  // Vibrant Sky Blue

            Color inactiveBg = Color.FromArgb(36, 36, 46);
            Color borderInactive = Color.FromArgb(70, 70, 85);
            Color borderActive = Color.White;

            bool isAncSupported = _protocolManager.IsModeSupported(_currentProfile, SoundMode.NoiseCancelling);

            // Normal Audio Mode
            _btnModeOff.Text = (mode == SoundMode.Off) ? "⚪ Normal  [ ACTIVE ]" : "⚪ Normal Audio Mode";
            _btnModeOff.BackColor = (mode == SoundMode.Off) ? activeOff : inactiveBg;
            _btnModeOff.FlatAppearance.BorderColor = (mode == SoundMode.Off) ? borderActive : borderInactive;
            _btnModeOff.FlatAppearance.BorderSize = (mode == SoundMode.Off) ? 2 : 1;
            _btnModeOff.Refresh();

            // Game Mode (Low Latency)
            _btnModeGame.Text = (mode == SoundMode.GameMode) ? "🎮 Game Mode  [ ACTIVE ]" : "🎮 Game Mode (Low Latency)";
            _btnModeGame.BackColor = (mode == SoundMode.GameMode) ? activeGame : inactiveBg;
            _btnModeGame.FlatAppearance.BorderColor = (mode == SoundMode.GameMode) ? borderActive : borderInactive;
            _btnModeGame.FlatAppearance.BorderSize = (mode == SoundMode.GameMode) ? 2 : 1;
            _btnModeGame.Refresh();

            // Active Noise Cancelling (ANC)
            string ancLabel = isAncSupported ? "🛡️ Active Noise Cancelling" : "🛡️ ANC (Not Supported)";
            _btnModeAnc.Text = (mode == SoundMode.NoiseCancelling && isAncSupported) ? "🛡️ ANC  [ ACTIVE ]" : ancLabel;
            _btnModeAnc.BackColor = (mode == SoundMode.NoiseCancelling && isAncSupported) ? activeAnc : inactiveBg;
            _btnModeAnc.FlatAppearance.BorderColor = (mode == SoundMode.NoiseCancelling && isAncSupported) ? borderActive : borderInactive;
            _btnModeAnc.FlatAppearance.BorderSize = (mode == SoundMode.NoiseCancelling && isAncSupported) ? 2 : 1;
            _btnModeAnc.Refresh();

            // Ambient / Transparency Mode
            string ambientLabel = isAncSupported ? "🍃 Ambient Sound" : "🍃 Ambient (Not Supported)";
            _btnModeAmbient.Text = (mode == SoundMode.Ambient && isAncSupported) ? "🍃 Ambient  [ ACTIVE ]" : ambientLabel;
            _btnModeAmbient.BackColor = (mode == SoundMode.Ambient && isAncSupported) ? activeAmbient : inactiveBg;
            _btnModeAmbient.FlatAppearance.BorderColor = (mode == SoundMode.Ambient && isAncSupported) ? borderActive : borderInactive;
            _btnModeAmbient.FlatAppearance.BorderSize = (mode == SoundMode.Ambient && isAncSupported) ? 2 : 1;
            _btnModeAmbient.Refresh();
        }

        private void ShowSettingsDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Gemini AI Settings";
                dlg.Size = new Size(380, 200);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterScreen;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.FromArgb(28, 28, 32);
                dlg.ForeColor = Color.White;
                dlg.Font = new Font("Segoe UI", 9F);

                var lblPrompt = new Label
                {
                    Text = "Gemini API Key (stored locally for device intelligence):",
                    Location = new Point(16, 16),
                    Size = new Size(340, 24)
                };
                dlg.Controls.Add(lblPrompt);

                var txtKey = new TextBox
                {
                    Text = _geminiService.ApiKey ?? "",
                    Location = new Point(16, 44),
                    Size = new Size(330, 26),
                    BackColor = Color.FromArgb(40, 40, 48),
                    ForeColor = Color.White,
                    UseSystemPasswordChar = true
                };
                dlg.Controls.Add(txtKey);

                var lblHint = new Label
                {
                    Text = "Leave empty to use built-in local detection without AI.",
                    Location = new Point(16, 78),
                    Size = new Size(330, 20),
                    ForeColor = Color.FromArgb(161, 161, 170)
                };
                dlg.Controls.Add(lblHint);

                var btnSave = new Button
                {
                    Text = "Save",
                    Location = new Point(170, 114),
                    Size = new Size(80, 28),
                    BackColor = Color.FromArgb(99, 102, 241),
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.OK
                };
                btnSave.FlatAppearance.BorderSize = 0;
                dlg.Controls.Add(btnSave);

                var btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(266, 114),
                    Size = new Size(80, 28),
                    BackColor = Color.FromArgb(50, 50, 60),
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel
                };
                btnCancel.FlatAppearance.BorderSize = 0;
                dlg.Controls.Add(btnCancel);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _geminiService.ApiKey = txtKey.Text.Trim();
                    ScanDevices();
                }
            }
        }

        private void DrawRoundedBorder(Graphics g, Rectangle rect, int radius, Color borderColor)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = GetRoundedRectPath(new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), radius))
            using (var pen = new Pen(borderColor, 1))
            {
                g.DrawPath(pen, path);
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
