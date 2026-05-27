#region

using System;

#endregion

namespace Microsoft.VisualBasic.Devices
{
    internal static class Platform
    {
        public static readonly bool x64 = IntPtr.Size == 8;
        public static readonly bool LittleEndian = BitConverter.IsLittleEndian;
        public static readonly bool IsWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
    }
}