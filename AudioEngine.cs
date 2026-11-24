using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace EarbudsController
{
    public class AudioDevice
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string CleanName { get; set; }
        public string Enumerator { get; set; }
        public bool IsBluetooth { get; set; }
        public bool IsActive { get; set; }

        public override string ToString()
        {
            return CleanName + (IsBluetooth ? " (Bluetooth)" : "");
        }
    }

    public class AudioEngine
    {
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMMDeviceEnumerator
        {
            [PreserveSig]
            int EnumAudioEndpoints(int dataFlow, int stateMask, out IMMDeviceCollection ppDevices);
            [PreserveSig]
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
            [PreserveSig]
            int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);
            [PreserveSig]
            int RegisterEndpointNotificationCallback(IntPtr pClient);
            [PreserveSig]
            int UnregisterEndpointNotificationCallback(IntPtr pClient);
        }

        [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMMDeviceCollection
        {
            [PreserveSig]
            int GetCount(out uint pcDevices);
            [PreserveSig]
            int Item(uint nDevice, out IMMDevice ppDevice);
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMMDevice
        {
            [PreserveSig]
            int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
            [PreserveSig]
            int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);
            [PreserveSig]
            int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
            [PreserveSig]
            int GetState(out int pdwState);
        }

        [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IPropertyStore
        {
            [PreserveSig]
            int GetCount(out uint cProps);
            [PreserveSig]
            int GetAt(uint iProp, out PropertyKey pkey);
            [PreserveSig]
            int GetValue(ref PropertyKey key, out PropVariant pv);
            [PreserveSig]
            int SetValue(ref PropertyKey key, ref PropVariant propvar);
            [PreserveSig]
            int Commit();
        }

        [Guid("7991EEC0-363E-4299-ACA0-16CD8401C449"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMMNotificationClient
        {
            void OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, int dwNewState);
            void OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId);
            void OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId);
            void OnDefaultDeviceChanged(int flow, int role, [MarshalAs(UnmanagedType.LPWStr)] string pwstrDefaultDeviceId);
            void OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, PropertyKey key);
        }

        [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAudioEndpointVolume
        {
            int RegisterControlChangeNotify(IntPtr pNotify);
            int UnregisterControlChangeNotify(IntPtr pNotify);
            int GetChannelCount(out uint pnChannelCount);
            int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
            [PreserveSig]
            int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
            int GetMasterVolumeLevel(out float pfLevelDB);
            [PreserveSig]
            int GetMasterVolumeLevelScalar(out float pfLevel);
            int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
            int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
            int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
            int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
            [PreserveSig]
            int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
            [PreserveSig]
            int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct PropertyKey
        {
            public Guid fmtid;
            public uint pid;

            public PropertyKey(Guid guid, uint id)
            {
                fmtid = guid;
                pid = id;
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 24)]
        internal struct PropVariant
        {
            [FieldOffset(0)] public ushort vt;
            [FieldOffset(8)] public IntPtr pwszVal;
        }

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        internal class MMDeviceEnumeratorComObject { }

        private static readonly PropertyKey PKEY_Device_FriendlyName = new PropertyKey(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 14);
        private static readonly PropertyKey PKEY_Device_EnumeratorName = new PropertyKey(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 24);

        private IMMDeviceEnumerator _deviceEnumerator;

        public AudioEngine()
        {
            _deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
        }

        public AudioDevice GetDefaultOutputDevice()
        {
            try
            {
                IMMDevice dev;
                int hr = _deviceEnumerator.GetDefaultAudioEndpoint(0 /* eRender */, 1 /* eMultimedia */, out dev);
                if (hr != 0 || dev == null) return null;

                return ReadDeviceInfo(dev);
            }
            catch
            {
                return null;
            }
        }

        public List<AudioDevice> GetAllOutputDevices()
        {
            var list = new List<AudioDevice>();
            try
            {
                IMMDeviceCollection coll;
                // 1 = DEVICE_STATE_ACTIVE, 8 = DEVICE_STATE_UNPLUGGED (Bluetooth standby)
                int hr = _deviceEnumerator.EnumAudioEndpoints(0, 1 | 8, out coll);
                if (hr == 0 && coll != null)
                {
                    uint count;
                    coll.GetCount(out count);
                    for (uint i = 0; i < count; i++)
                    {
                        IMMDevice dev;
                        coll.Item(i, out dev);
                        if (dev != null)
                        {
                            var info = ReadDeviceInfo(dev);
                            if (info != null) list.Add(info);
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        private IMMDevice GetDeviceOrDefault(string deviceId)
        {
            try
            {
                IMMDevice dev = null;
                if (!string.IsNullOrEmpty(deviceId))
                {
                    int hr = _deviceEnumerator.GetDevice(deviceId, out dev);
                    if (hr == 0 && dev != null) return dev;
                }
                _deviceEnumerator.GetDefaultAudioEndpoint(0, 1, out dev);
                return dev;
            }
            catch
            {
                return null;
            }
        }

        public float GetMasterVolume(string deviceId = null)
        {
            try
            {
                IMMDevice dev = GetDeviceOrDefault(deviceId);
                if (dev == null) return 0f;

                Guid iid = typeof(IAudioEndpointVolume).GUID;
                object obj;
                int hr = dev.Activate(ref iid, 1, IntPtr.Zero, out obj);
                if (hr != 0 || obj == null) return 0f;

                var epv = (IAudioEndpointVolume)obj;
                float vol;
                epv.GetMasterVolumeLevelScalar(out vol);
                return vol;
            }
            catch
            {
                return 0f;
            }
        }

        public void SetMasterVolume(float level, string deviceId = null)
        {
            try
            {
                if (level < 0f) level = 0f;
                if (level > 1f) level = 1f;

                IMMDevice dev = GetDeviceOrDefault(deviceId);
                if (dev == null) return;

                Guid iid = typeof(IAudioEndpointVolume).GUID;
                object obj;
                int hr = dev.Activate(ref iid, 1, IntPtr.Zero, out obj);
                if (hr != 0 || obj == null) return;

                var epv = (IAudioEndpointVolume)obj;
                Guid ctx = Guid.Empty;
                epv.SetMasterVolumeLevelScalar(level, ref ctx);
            }
            catch { }
        }

        public bool GetMute(string deviceId = null)
        {
            try
            {
                IMMDevice dev = GetDeviceOrDefault(deviceId);
                if (dev == null) return false;

                Guid iid = typeof(IAudioEndpointVolume).GUID;
                object obj;
                int hr = dev.Activate(ref iid, 1, IntPtr.Zero, out obj);
                if (hr != 0 || obj == null) return false;

                var epv = (IAudioEndpointVolume)obj;
                bool mute;
                epv.GetMute(out mute);
                return mute;
            }
            catch
            {
                return false;
            }
        }

        public void SetMute(bool mute, string deviceId = null)
        {
            try
            {
                IMMDevice dev = GetDeviceOrDefault(deviceId);
                if (dev == null) return;

                Guid iid = typeof(IAudioEndpointVolume).GUID;
                object obj;
                int hr = dev.Activate(ref iid, 1, IntPtr.Zero, out obj);
                if (hr != 0 || obj == null) return;

                var epv = (IAudioEndpointVolume)obj;
                Guid ctx = Guid.Empty;
                epv.SetMute(mute, ref ctx);
            }
            catch { }
        }

        public void ResetChannelBalance(string deviceId = null)
        {
            try
            {
                IMMDevice dev = GetDeviceOrDefault(deviceId);
                if (dev == null) return;

                Guid iid = typeof(IAudioEndpointVolume).GUID;
                object obj;
                int hr = dev.Activate(ref iid, 1, IntPtr.Zero, out obj);
                if (hr != 0 || obj == null) return;

                var epv = (IAudioEndpointVolume)obj;
                uint count;
                epv.GetChannelCount(out count);
                float master;
                epv.GetMasterVolumeLevelScalar(out master);
                Guid ctx = Guid.Empty;
                for (uint i = 0; i < count; i++)
                {
                    epv.SetChannelVolumeLevelScalar(i, master, ref ctx);
                }
            }
            catch { }
        }

        private AudioDevice ReadDeviceInfo(IMMDevice dev)
        {
            if (dev == null) return null;
            try
            {
                string id;
                dev.GetId(out id);

                int state;
                dev.GetState(out state);

                IPropertyStore store;
                dev.OpenPropertyStore(0, out store);

                string friendlyName = GetStringProperty(store, PKEY_Device_FriendlyName);
                string enumerator = GetStringProperty(store, PKEY_Device_EnumeratorName);

                bool isBluetooth = string.Equals(enumerator, "BTHENUM", StringComparison.OrdinalIgnoreCase)
                                   || (id != null && id.IndexOf("BTHENUM", StringComparison.OrdinalIgnoreCase) >= 0);

                string cleanName = ExtractCleanDeviceName(friendlyName);

                return new AudioDevice
                {
                    Id = id,
                    FullName = friendlyName,
                    CleanName = cleanName,
                    Enumerator = enumerator,
                    IsBluetooth = isBluetooth,
                    IsActive = (state == 1)
                };
            }
            catch
            {
                return null;
            }
        }

        private string ExtractCleanDeviceName(string friendlyName)
        {
            if (string.IsNullOrEmpty(friendlyName)) return "Audio Device";

            // If name is like "Headphones (OnePlus Nord Buds 3r)" or "Speakers (Realtek(R) Audio)"
            Match m = Regex.Match(friendlyName, @"\((.+)\)");
            if (m.Success && !string.IsNullOrEmpty(m.Groups[1].Value))
            {
                string inside = m.Groups[1].Value.Trim();
                if (!string.Equals(inside, "Realtek(R) Audio", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(inside, "Realtek Audio", StringComparison.OrdinalIgnoreCase))
                {
                    return inside;
                }
            }

            return friendlyName.Trim();
        }

        private static string GetStringProperty(IPropertyStore store, PropertyKey key)
        {
            try
            {
                PropVariant pv;
                store.GetValue(ref key, out pv);
                if (pv.vt == 31 && pv.pwszVal != IntPtr.Zero)
                {
                    return Marshal.PtrToStringUni(pv.pwszVal);
                }
            }
            catch { }
            return string.Empty;
        }
    }
}
