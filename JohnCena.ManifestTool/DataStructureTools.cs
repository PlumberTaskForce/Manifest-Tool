using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using JohnCena.ManifestTool.DataAttributes;

namespace JohnCena.ManifestTool
{
    public static class DataStructureTools
    {
        private const uint MAGIC = 0xBADB002E;

        public static XNamespace XMLNamespaceJC { get; private set; }
        public static XNamespace XMLNamespaceData { get; private set; }

        static DataStructureTools()
        {
            XMLNamespaceJC = "johncena.data.cryptic.manifest";
            XMLNamespaceData = "johncena.data.serialization.xml";
        }

        public static byte[] Serialize<T>(T data) where T : struct
        {
            var t = typeof(T);
            var a = typeof(StructureOrderAttribute);
            var utf = new UTF8Encoding(false);
            var pl = t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(xp => xp.GetSetMethod() != null && xp.GetGetMethod() != null);
            var plx = pl
                .Select(xp => new { prop = xp, orderattr = (StructureOrderAttribute)Attribute.GetCustomAttribute(xp, a) })
                .Where(xpa => xpa.orderattr != null)
                .OrderBy(xpa => xpa.orderattr.Order);
            var sd = new Dictionary<string, string>();


            int sl = 0;
            foreach (var xp in plx)
            {
                var xpt = xp.prop.PropertyType;
                if (xpt == typeof(short) || xpt == typeof(ushort))
                    sl += 2;
                else if (xpt == typeof(int) || xpt == typeof(uint) || xpt == typeof(float))
                    sl += 4;
                else if (xpt == typeof(long) || xpt == typeof(ulong) || xpt == typeof(double))
                    sl += 8;
                else if (xpt == typeof(string))
                {
                    var s = (string)xp.prop.GetValue(data);
                    sd[xp.prop.Name] = s;
                    sl += utf.GetByteCount(s) + 2;
                }
            }

            if (sl <= 0)
                return null;

            var btc = typeof(BitConverter);
            //var btcb = btc.GetMethod("GetBytes", BindingFlags.Public | BindingFlags.Static);
            var btcb = new Dictionary<Type, MethodInfo>()
            {
                { typeof(short), btc.GetMethod("GetBytes", new Type[] { typeof(short) }) },
                { typeof(ushort), btc.GetMethod("GetBytes", new Type[] { typeof(ushort) }) },
                { typeof(int), btc.GetMethod("GetBytes", new Type[] { typeof(int) }) },
                { typeof(uint), btc.GetMethod("GetBytes", new Type[] { typeof(uint) }) },
                { typeof(long), btc.GetMethod("GetBytes", new Type[] { typeof(long) }) },
                { typeof(ulong), btc.GetMethod("GetBytes", new Type[] { typeof(ulong) }) },
                { typeof(float), btc.GetMethod("GetBytes", new Type[] { typeof(float) }) },
                { typeof(double), btc.GetMethod("GetBytes", new Type[] { typeof(double) }) }
            };

            var sb = new byte[sl];
            var sp = 0;
            foreach (var xp in plx)
            {
                if (xp.prop.PropertyType != typeof(string))
                {
                    var xpv = xp.prop.GetValue(data);
                    var xps = (byte[])btcb[xp.prop.PropertyType].Invoke(null, new object[] { xpv });
                    Array.Copy(xps, 0, sb, sp, xps.Length);
                    sp += xps.Length;
                }
                else
                {
                    var xpv = sd[xp.prop.Name];
                    var xps = utf.GetBytes(xpv);
                    var xpl = (ushort)xps.Length;
                    var xpc = BitConverter.GetBytes(xpl);
                    Array.Copy(xpc, 0, sb, sp, xpc.Length);
                    sp += xpc.Length;
                    Array.Copy(xps, 0, sb, sp, xps.Length);
                    sp += xps.Length;
                }
            }

            return sb;
        }

        public static T Deserialize<T>(byte[] data, ref int index) where T : struct
        {
            var t = typeof(T);
            var a = typeof(StructureOrderAttribute);
            var utf = new UTF8Encoding(false);
            var pl = t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(xp => xp.GetSetMethod() != null && xp.GetGetMethod() != null);
            var plx = pl
                .Select(xp => new { prop = xp, orderattr = (StructureOrderAttribute)Attribute.GetCustomAttribute(xp, a) })
                .Where(xpa => xpa.orderattr != null)
                .OrderBy(xpa => xpa.orderattr.Order);

            var btc = typeof(BitConverter);
            var pd = new Dictionary<Type, MethodInfo>()
            {
                { typeof(short), btc.GetMethod("ToInt16", BindingFlags.Public | BindingFlags.Static)  },
                { typeof(ushort), btc.GetMethod("ToUInt16", BindingFlags.Public | BindingFlags.Static) },
                { typeof(int), btc.GetMethod("ToInt32", BindingFlags.Public | BindingFlags.Static) },
                { typeof(uint), btc.GetMethod("ToUInt32", BindingFlags.Public | BindingFlags.Static) },
                { typeof(long), btc.GetMethod("ToInt64", BindingFlags.Public | BindingFlags.Static) },
                { typeof(ulong), btc.GetMethod("ToUInt64", BindingFlags.Public | BindingFlags.Static) },
                { typeof(float), btc.GetMethod("ToSingle", BindingFlags.Public | BindingFlags.Static) },
                { typeof(double), btc.GetMethod("ToDouble", BindingFlags.Public | BindingFlags.Static) }
            };

            int sb = index;
            var dv = new Dictionary<PropertyInfo, object>();
            foreach (var xp in plx)
            {
                if (pd.ContainsKey(xp.prop.PropertyType))
                {
                    // numeric
                    var m = pd[xp.prop.PropertyType];
                    var v = m.Invoke(null, new object[] { data, sb });
                    dv[xp.prop] = v;
                    var xpt = xp.prop.PropertyType;
                    if (xpt == typeof(short) || xpt == typeof(ushort))
                        sb += 2;
                    else if (xpt == typeof(int) || xpt == typeof(uint) || xpt == typeof(float))
                        sb += 4;
                    else if (xpt == typeof(long) || xpt == typeof(ulong) || xpt == typeof(double))
                        sb += 8;
                }
                else
                {
                    // string
                    var sl = BitConverter.ToUInt16(data, sb);
                    sb += 2;
                    var s = utf.GetString(data, sb, sl);
                    sb += sl;
                    dv[xp.prop] = s;
                }
            }
            index = sb;

            var dat = Activator.CreateInstance<T>();
            var obj = (object)dat;
            foreach (var kvp in dv)
            {
                kvp.Key.SetValue(obj, kvp.Value);
            }
            dat = (T)obj;

            return dat;
        }

        public static XElement XMLize<T>(T data)
        {
            var t = typeof(T);
            var a = typeof(StructureOrderAttribute);
            var pl = t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(xp => xp.GetGetMethod() != null);
            var plx = pl
                .Select(xp => new { prop = xp, orderattr = (StructureOrderAttribute)Attribute.GetCustomAttribute(xp, a) })
                .Where(xpa => xpa.orderattr != null)
                .OrderBy(xpa => xpa.orderattr.Order);

            var xn0 = XMLNamespaceJC;
            var xn1 = XMLNamespaceData;
            var xe = new XElement(xn0 + t.Name.ToLower());

            //xe.Add(new XAttribute(XNamespace.Xmlns + "johncena", xn0.NamespaceName));
            //xe.Add(new XAttribute(XNamespace.Xmlns + "data", xn1.NamespaceName));

            xe.Add(new XAttribute(xn1 + "data-type", t.FullName));

            foreach (var xp in plx)
            {
                var v = xp.prop.GetValue(data);

                var xxe = new XElement(xn0 + xp.prop.Name.ToLower(), v);
                xxe.Add(new XAttribute(xn1 + "target-property", xp.prop.Name));

                xe.Add(xxe);
            }

            return xe;
        }

        public static void WriteXML(Manifest manifest, FileInfo target)
        {
            var xn0 = XMLNamespaceJC;
            var xn1 = XMLNamespaceData;

            var xmh = XMLize(manifest.Header);

            var xmfs = new List<XElement>();
            foreach (var xmf in manifest.Entries)
                xmfs.Add(XMLize(xmf));
            var xdoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement(xn0 + "cryptic-manifest",
                    new XAttribute(XNamespace.Xmlns + "johncena", xn0.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "data", xn1.NamespaceName),
                    new XAttribute(xn1 + "target-type", manifest.GetType().FullName)
                )
            );
            xdoc.Root.Add(xmh);
            var xefs = new XElement(xn0 + "file-entries");
            xdoc.Root.Add(xefs);
            foreach (var xmf in xmfs)
                xefs.Add(xmf);

            using (var fs = target.Create())
                xdoc.Save(fs);
        }

        public static Manifest LoadManifest(FileInfo file)
        {
            if (!file.Name.EndsWith(".manifest"))
                return default(Manifest);

            using (var fs = file.OpenRead())
            using (var sr = new StreamReader(fs))
            {
                var line = sr.ReadLine();
                if (!line.StartsWith("#"))
                    return default(Manifest);

                var reg0 = new Regex(@"^# Project: ([A-Za-z0-9\-_]+?)  Branch: (\d+) \(([A-Z]+)\.(\d+)\.([0-9a-z]+)\.(\d+) \[incremental (\d+)\]\)  Revision: (\d+)");
                var m = reg0.Match(line);
                if (!m.Success)
                    return default(Manifest);

                var bbdr = m.Groups[5].Value;
                var bbdr_lc = bbdr.Last();
                if (Char.IsLetter(bbdr_lc))
                    bbdr = bbdr.Substring(0, bbdr.Length - 1);

                var unix = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var bd = DateTime.ParseExact(bbdr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                var bt = bd - unix;
                var bs = (ulong)bt.TotalSeconds;

                var mh = new ManifestHeader
                {
                    Project = m.Groups[1].Value,
                    BranchID = ushort.Parse(m.Groups[2].Value),
                    BranchProject = m.Groups[3].Value,
                    BranchBuildID = ushort.Parse(m.Groups[4].Value),
                    BranchBuildDate = bs,
                    BranchBuildNumber = ushort.Parse(m.Groups[6].Value),
                    BranchIncrement = uint.Parse(m.Groups[7].Value),
                    BranchRevision = uint.Parse(m.Groups[8].Value)
                };
                var mfs = new List<FileEntry>();

                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) break;

                    var lins = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lins.Length < 6)
                        break;

                    var mf = new FileEntry
                    {
                        File = lins.Length > 6 ? string.Join(" ", lins, 0, lins.Length - 5) : lins[0],
                        Timestamp = ulong.Parse(lins[lins.Length - 5]),
                        Length = ulong.Parse(lins[lins.Length - 4]),
                        Checksum = int.Parse(lins[lins.Length - 3]),
                        Field3 = int.Parse(lins[lins.Length - 2]),
                        Field4 = long.Parse(lins[lins.Length - 1])
                    };
                    mfs.Add(mf);
                }

                return new Manifest
                {
                    Header = mh,
                    Entries = mfs
                };
            }
        }

        public static Manifest LoadBinaryManifest(FileInfo file)
        {
            int index = 0;
            if (!file.Exists)
                return default(Manifest);

            try
            {
                using (var fs = file.OpenRead())
                using (var br = new BinaryReader(fs))
                {
                    var magic = br.ReadUInt32();
                    if (magic != MAGIC)
                        return default(Manifest);

                    var nfiles = br.ReadUInt32();
                    byte[] data = new byte[br.BaseStream.Length - 8];
                    br.Read(data, 0, data.Length);
                    index = 0;

                    var mh = Deserialize<ManifestHeader>(data, ref index);

                    var mfs = new List<FileEntry>();
                    for (int i = 0; i < nfiles; i++)
                    {
                        mfs.Add(Deserialize<FileEntry>(data, ref index));
                    }

                    return new Manifest
                    {
                        Header = mh,
                        Entries = mfs
                    };
                }
            }
            catch (Exception)
            {
                return default(Manifest);
            }
        }

        public static void WriteBinaryManifest(Manifest manifest, FileInfo file)
        {
            using (var fs = file.Create())
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(MAGIC);
                bw.Write((uint)manifest.Entries.Count());

                bw.Write(Serialize(manifest.Header));

                foreach (var mf in manifest.Entries)
                    bw.Write(Serialize(mf));
            }
        }

        public static void WriteChangelog(IEnumerable<Change> changes, FileInfo file)
        {
            if (changes.Count() <= 0)
                return;

            using (var fs = file.Create())
            using (var sw = new StreamWriter(fs, new UTF8Encoding(false)))
            {
                var gcl = changes.GroupBy(xc => xc.ChangeType);
                foreach (var gc in gcl)
                {
                    sw.Write(" ==================== ");
                    sw.WriteLine(gc.Key.ToString());

                    foreach (var xc in gc)
                    {
                        sw.WriteLine(xc.Entry.File);
                    }

                    sw.WriteLine();
                }
            }
        }
        
        public static ProgramParams ParseCommandLine(string[] args)
        {
            var t = typeof(ProgramParams);
            var pd = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(xp => xp.GetGetMethod() != null && xp.GetSetMethod() != null)
                .ToDictionary(xp => xp.Name.ToLower());
            var pc = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(xp => xp.GetGetMethod() != null && xp.GetSetMethod() != null)
                .ToDictionary(xp => xp.Name.ToLower(), xp => false);
            var f = string.Empty;

            foreach (var arg in args)
            {
                var an = arg.Substring(1).ToLower();
                if (!arg.StartsWith("-"))
                    f = arg;
                else if (pd.ContainsKey(an))
                    pc[an] = true;
            }

            var file = new FileInfo(f);

            var pp = new ProgramParams();
            pp.File = file;
            var obj = (object)pp;
            foreach (var kvp in pd)
                if (kvp.Value.PropertyType == typeof(bool))
                    kvp.Value.SetValue(obj, pc[kvp.Key]);
            pp = (ProgramParams)obj;

            return pp;
        }
    }
}
