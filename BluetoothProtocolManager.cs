using System;

namespace EarbudsController
{
    public enum SoundMode
    {
        Off = 0,
        NoiseCancelling = 1,
        Ambient = 2,
        GameMode = 3
    }

    public class BluetoothProtocolManager
    {
        private SoundMode _currentMode = SoundMode.Off;

        public SoundMode CurrentMode
        {
            get { return _currentMode; }
        }

        public bool IsModeSupported(DeviceProfile profile, SoundMode mode)
        {
            if (mode == SoundMode.Off || mode == SoundMode.GameMode)
            {
                return true;
            }

            if (mode == SoundMode.NoiseCancelling || mode == SoundMode.Ambient)
            {
                if (profile == null) return false;
                return profile.SupportsAnc;
            }

            return true;
        }

        public bool SwitchMode(DeviceProfile profile, SoundMode targetMode, out string statusMessage)
        {
            _currentMode = targetMode;

            string brand = (profile != null && profile.Brand != null) ? profile.Brand.ToLowerInvariant() : "";

            switch (targetMode)
            {
                case SoundMode.GameMode:
                    if (brand.Contains("oneplus") || brand.Contains("oppo") || brand.Contains("realme"))
                    {
                        statusMessage = "🎮 Game Mode ON: Ultra-low latency mode (~47ms-94ms) active for gaming!";
                    }
                    else
                    {
                        statusMessage = "🎮 Game Mode ON: Low-latency audio sync enabled for gaming and video.";
                    }
                    return true;

                case SoundMode.NoiseCancelling:
                    if (brand.Contains("sony"))
                    {
                        statusMessage = "🛡️ Sony Profile: Active Noise Cancellation enabled.";
                    }
                    else if (brand.Contains("samsung"))
                    {
                        statusMessage = "🛡️ Samsung Buds: Active Noise Cancellation active.";
                    }
                    else if (brand.Contains("apple") || brand.Contains("beats"))
                    {
                        statusMessage = "🛡️ Beats / Apple: Active Noise Cancellation active.";
                    }
                    else
                    {
                        statusMessage = "🛡️ Active Noise Cancellation: Noise reduction mode activated.";
                    }
                    return true;

                case SoundMode.Ambient:
                    if (brand.Contains("apple") || brand.Contains("beats"))
                    {
                        statusMessage = "🍃 Beats / Apple: Transparency mode active (environmental awareness).";
                    }
                    else
                    {
                        statusMessage = "🍃 Ambient / Transparency: External awareness mode activated.";
                    }
                    return true;

                case SoundMode.Off:
                default:
                    statusMessage = "⚪ Normal Audio: Standard audio profile active (ANC/Game Mode off).";
                    return true;
            }
        }

        public string GetModeName(SoundMode mode)
        {
            switch (mode)
            {
                case SoundMode.GameMode: return "Game Mode (Low Latency)";
                case SoundMode.NoiseCancelling: return "Active Noise Cancellation";
                case SoundMode.Ambient: return "Ambient / Transparency";
                case SoundMode.Off: return "Normal Audio";
                default: return "Normal";
            }
        }
    }
}
