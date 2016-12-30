using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JohnCena.ManifestTool.BuildUtilities;

namespace JohnCena.ManifestTool
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            var v = VersionTools.AssemblyVersion;
            var b = VersionTools.AssemblyBuildDate;

            Console.WriteLine("Cryptic Manifest Tool");
            Console.WriteLine("Version {0} (build revision {1:yyyyMMddHHmmss})", v, b);
            Console.WriteLine("Usage: mct [-nochangelog] [-noupdate] [-xml] <file path>");
            Console.WriteLine("---");

            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.WriteLine("[{0:00,000}] Parsing commandline", sw.ElapsedMilliseconds);
            var cmdl = DataStructureTools.ParseCommandLine(args);
            Console.WriteLine("[{0:00,000}] File: {1}", sw.ElapsedMilliseconds, cmdl.File.FullName);
            Console.WriteLine("[{0:00,000}] Additional options: ", sw.ElapsedMilliseconds);
            Console.WriteLine("[{0:00,000}]     -NoChangelog: {1}", sw.ElapsedMilliseconds, cmdl.NoChangelog);
            Console.WriteLine("[{0:00,000}]     -NoUpdate: {1}", sw.ElapsedMilliseconds, cmdl.NoUpdate);
            Console.WriteLine("[{0:00,000}]     -XML: {1}", sw.ElapsedMilliseconds, cmdl.XML);
            Console.WriteLine();

            Console.WriteLine("[{0:00,000}] Loading manifest...", sw.ElapsedMilliseconds);
            var mf = DataStructureTools.LoadManifest(cmdl.File);
            Console.WriteLine("[{0:00,000}] Loaded", sw.ElapsedMilliseconds);

            if (cmdl.XML)
            {
                Console.WriteLine("[{0:00,000}] Writing XMLized manifest...", sw.ElapsedMilliseconds);
                var xfi = new FileInfo(string.Concat(cmdl.File.FullName, ".xml"));
                DataStructureTools.WriteXML(mf, xfi);
                Console.WriteLine("[{0:00,000}] Done", sw.ElapsedMilliseconds);
            }

            Console.WriteLine("[{0:00,000}] Loading binary manifest...", sw.ElapsedMilliseconds);
            var bfi = new FileInfo(string.Concat(cmdl.File.FullName, ".bcm"));
            var bmf = DataStructureTools.LoadBinaryManifest(bfi);
            Console.WriteLine("[{0:00,000}] Done", sw.ElapsedMilliseconds);

            if (bmf.Entries != null)
            {
                Console.WriteLine("[{0:00,000}] Comparing manifests...", sw.ElapsedMilliseconds);
                var cel = bmf.Entries
                    .Concat(mf.Entries)
                    .GroupBy(xmf => xmf.File)
                    .Select(xgmf => xgmf.First());
                var ame = cel
                    .ToDictionary(xmf => xmf.File);
                var bed = bmf.Entries
                    .ToDictionary(xmf => xmf.File, xmf => xmf.Timestamp);
                var ced = mf.Entries
                    .ToDictionary(xmf => xmf.File, xmf => xmf.Timestamp);
                var ecd = cel
                    .ToDictionary(xmf => xmf.File, xmf => ChangeType.Removed);

                foreach (var xmf in cel)
                {
                    if (!ced.ContainsKey(xmf.File))
                        continue;

                    if (!bed.ContainsKey(xmf.File))
                    {
                        ecd[xmf.File] = ChangeType.New;
                        continue;
                    }

                    if (ced[xmf.File] != bed[xmf.File])
                    {
                        ecd[xmf.File] = ChangeType.Timestamp;
                        continue;
                    }

                    ecd[xmf.File] = ChangeType.Untouched;
                }
                Console.WriteLine("[{0:00,000}] Done", sw.ElapsedMilliseconds);

                if (!cmdl.NoChangelog)
                {
                    Console.WriteLine("[{0:00,000}] Writing changelog...", sw.ElapsedMilliseconds);
                    var cl = ecd
                        .Where(xkvp => xkvp.Value != ChangeType.Untouched)
                        .Select(xkvp => new Change { Entry = ame[xkvp.Key], ChangeType = xkvp.Value });

                    var cfi = new FileInfo(string.Concat(cmdl.File.FullName, ".changelog.", DateTime.Now.ToString("yyyyMMddHHmmss"), ".txt"));
                    DataStructureTools.WriteChangelog(cl, cfi);
                    Console.WriteLine("[{0:00,000}] Done", sw.ElapsedMilliseconds);
                }
            }

            if (!cmdl.NoUpdate)
            {
                Console.WriteLine("[{0:00,000}] Updating binary manifest...", sw.ElapsedMilliseconds);
                DataStructureTools.WriteBinaryManifest(mf, bfi);
                Console.WriteLine("[{0:00,000}] Done", sw.ElapsedMilliseconds);
            }
            Console.WriteLine("[{0:00,000}] All operations completed, press any key to continue", sw.ElapsedMilliseconds);
            sw.Stop();

            Console.ReadKey();
        }
    }
}
