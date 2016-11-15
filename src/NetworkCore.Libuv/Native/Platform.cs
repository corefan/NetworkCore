namespace NetworkCore.Libuv.Native
{
    using System.Runtime.InteropServices;

    static class Platform
    {
        static Platform()
        {
            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            IsMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            IsUnix = IsLinux || IsMac;
        }

        public static bool IsWindows { get; }

        public static bool IsUnix { get; }

        public static bool IsMac { get; }

        public static bool IsLinux { get; }
    }
}
