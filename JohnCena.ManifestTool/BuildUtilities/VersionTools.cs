using System;
using System.IO;
using System.Reflection;

namespace JohnCena.ManifestTool.BuildUtilities
{
    public static class VersionTools
    {
        public static Version AssemblyVersion { get; private set; }
        public static DateTime AssemblyBuildDate { get; private set; }

        static VersionTools()
        {
            var a = Assembly.GetExecutingAssembly();
            var n = a.GetName();
            AssemblyVersion = n.Version;

            AssemblyBuildDate = GetLinkerTime(a, TimeZoneInfo.Utc);
        }

        internal static DateTime GetLinkerTime(Assembly assembly, TimeZoneInfo target = null)
        {
            var filePath = assembly.Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                stream.Read(buffer, 0, 2048);

            var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            var tz = target ?? TimeZoneInfo.Local;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

            return localTime;
        }
    }
}
