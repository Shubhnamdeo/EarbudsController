using System;
using System.Runtime.InteropServices;
using System.Text;

namespace EarbudsController
{
    public class BatteryService
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct DEVPROPKEY
        {
            public Guid fmtid;
            public uint pid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(IntPtr ClassGuid, string Enumerator, IntPtr hwndParent, uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetupDiGetDevicePropertyW(
            IntPtr deviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            ref DEVPROPKEY propertyKey,
            out uint propertyType,
            byte[] propertyBuffer,
            uint propertyBufferSize,
            out uint requiredSize,
            uint flags
        );

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            uint Property,
            out uint PropertyRegDataType,
            byte[] PropertyBuffer,
            uint PropertyBufferSize,
            out uint RequiredSize
        );

        private static readonly DEVPROPKEY PKEY_Device_BatteryLevel = new DEVPROPKEY
        {
            fmtid = new Guid("104EA319-6EE2-4701-BD47-8DDBF425BBE5"),
            pid = 2
        };

        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint DIGCF_ALLCLASSES = 0x00000004;
        private const uint SPDRP_FRIENDLYNAME = 12;

        /// <summary>
        /// Reads the battery level (0-100%) for a given device name from Windows SetupAPI.
        /// Returns -1 if the device does not report battery level.
        /// </summary>
        public int GetBatteryPercentage(string deviceCleanName)
        {
            IntPtr devSet = SetupDiGetClassDevs(IntPtr.Zero, "BTHENUM", IntPtr.Zero, DIGCF_PRESENT | DIGCF_ALLCLASSES);
            if (devSet == IntPtr.Zero || devSet == new IntPtr(-1))
            {
                return -1;
            }

            int bestMatchBattery = -1;
            try
            {
                SP_DEVINFO_DATA devData = new SP_DEVINFO_DATA();
                devData.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));

                uint index = 0;
                while (SetupDiEnumDeviceInfo(devSet, index++, ref devData))
                {
                    uint pType;
                    uint reqSize;
                    byte[] batBuf = new byte[16];
                    DEVPROPKEY batKey = PKEY_Device_BatteryLevel;

                    if (SetupDiGetDevicePropertyW(devSet, ref devData, ref batKey, out pType, batBuf, (uint)batBuf.Length, out reqSize, 0))
                    {
                        if (reqSize > 0)
                        {
                            int batteryVal = (int)batBuf[0];
                            if (batteryVal < 0 || batteryVal > 100) continue;

                            // Read Friendly Name
                            byte[] nameBuf = new byte[512];
                            uint nameType, nameReq;
                            string devFriendlyName = "";

                            if (SetupDiGetDeviceRegistryProperty(devSet, ref devData, SPDRP_FRIENDLYNAME, out nameType, nameBuf, (uint)nameBuf.Length, out nameReq))
                            {
                                devFriendlyName = Encoding.Unicode.GetString(nameBuf, 0, (int)nameReq).TrimEnd('\0');
                            }

                            // If we match the clean name (e.g. "OnePlus Nord Buds 3r" or "Beats Studio Pro")
                            if (!string.IsNullOrEmpty(deviceCleanName) && !string.IsNullOrEmpty(devFriendlyName))
                            {
                                if (devFriendlyName.IndexOf(deviceCleanName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    deviceCleanName.IndexOf(devFriendlyName, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    return batteryVal;
                                }
                            }

                            // Keep as fallback if only one Bluetooth battery device is connected
                            if (bestMatchBattery == -1)
                            {
                                bestMatchBattery = batteryVal;
                            }
                        }
                    }
                }
            }
            catch
            {
                return -1;
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(devSet);
            }

            return bestMatchBattery;
        }
    }
}
