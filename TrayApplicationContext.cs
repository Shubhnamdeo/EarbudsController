using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EarbudsController
{
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly MainFlyoutForm _flyoutForm;
        private readonly AudioEngine _audioEngine;
        private readonly GeminiService _geminiService;
        private readonly BluetoothProtocolManager _protocolManager;

        public TrayApplicationContext()
        {
            _audioEngine = new AudioEngine();
            _geminiService = new GeminiService();
            _protocolManager = new BluetoothProtocolManager();

            _flyoutForm = new MainFlyoutForm(_audioEngine, _geminiService, _protocolManager);

            var contextMenu = new ContextMenuStrip();
            var itemOpen = new ToolStripMenuItem("Open Earbuds Controller", null, (s, e) => ToggleFlyout());
            itemOpen.Font = new Font(itemOpen.Font, FontStyle.Bold);
            contextMenu.Items.Add(itemOpen);

            contextMenu.Items.Add(new ToolStripSeparator());

            contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

            _trayIcon = new NotifyIcon
            {
                Icon = GenerateHeadphoneIcon(),
                ContextMenuStrip = contextMenu,
                Text = "Earbuds Sound & ANC Controller",
                Visible = true
            };

            _trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ToggleFlyout();
                }
            };

            // Show window immediately on startup so user sees it right away
            _flyoutForm.Show();
            _flyoutForm.Activate();
        }

        private void ToggleFlyout()
        {
            if (_flyoutForm.Visible)
            {
                _flyoutForm.Hide();
            }
            else
            {
                _flyoutForm.Show();
                _flyoutForm.WindowState = FormWindowState.Normal;
                _flyoutForm.BringToFront();
                _flyoutForm.Activate();
                _flyoutForm.RefreshState();
            }
        }

        private void ExitApplication()
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Exit();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        private Icon GenerateHeadphoneIcon()
        {
            try
            {
                using (var bmp = new Bitmap(32, 32))
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    using (var pen = new Pen(Color.FromArgb(99, 102, 241), 3f))
                    {
                        g.DrawArc(pen, 5, 4, 22, 20, 180, 180);
                    }

                    using (var brush = new SolidBrush(Color.FromArgb(129, 140, 248)))
                    {
                        g.FillEllipse(brush, 3, 14, 6, 12);
                        g.FillEllipse(brush, 23, 14, 6, 12);
                    }

                    IntPtr hIcon = bmp.GetHicon();
                    return Icon.FromHandle(hIcon);
                }
            }
            catch
            {
                return SystemIcons.Application;
            }
        }
    }
}
