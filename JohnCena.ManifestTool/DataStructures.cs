using System;
using System.Collections.Generic;
using System.IO;
using JohnCena.ManifestTool.DataAttributes;

namespace JohnCena.ManifestTool
{
    // VARIABLE + 20
    public struct ManifestHeader
    {
        [StructureOrder(0)]
        public string Project { get; set; }

        [StructureOrder(1)]
        public ushort BranchID { get; set; }

        [StructureOrder(2)]
        public string BranchProject { get; set; }

        [StructureOrder(3)]
        public ushort BranchBuildID { get; set; }

        [StructureOrder(4)]
        public ulong BranchBuildDate { get; set; }

        [StructureOrder(5)]
        public DateTime BranchBuildDateTime
        {
            get
            {
                var unix = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var local = unix.AddSeconds(this.BranchBuildDate);
                return local;
            }
        }

        [StructureOrder(6)]
        public ushort BranchBuildNumber { get; set; }

        [StructureOrder(7)]
        public uint BranchIncrement { get; set; }

        [StructureOrder(8)]
        public uint BranchRevision { get; set; }

        public override string ToString()
        {
            return string.Concat(this.Project, ": ", this.BranchProject, ".", this.BranchBuildID, ".", this.BranchBuildDateTime.ToString("yyyy-MM-dd"), ".", this.BranchBuildNumber, " inc. ", this.BranchIncrement, " rev. ", this.BranchRevision);
        }
    }

    // VARIABLE + 36
    public struct FileEntry
    {
        [StructureOrder(0)]
        public string File { get; set; }

        [StructureOrder(1)]
        public ulong Timestamp { get; set; }

        [StructureOrder(2)]
        public DateTime FileDateTime
        {
            get
            {
                var unix = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var local = unix.AddSeconds(this.Timestamp);
                return local;
            }
        }

        [StructureOrder(3)]
        public ulong Length { get; set; }

        [StructureOrder(4)]
        public int Checksum { get; set; }

        [StructureOrder(5)]
        public long Field3 { get; set; }

        [StructureOrder(6)]
        public long Field4 { get; set; }

        public override string ToString()
        {
            return this.File;
        }
    }

    public struct Change
    {
        public FileEntry Entry { get; set; }
        public ChangeType ChangeType { get; set; }
    }

    public struct Manifest
    {
        public ManifestHeader Header { get; set; }
        public IEnumerable<FileEntry> Entries { get; set; }
    }

    public struct ProgramParams
    {
        public FileInfo File { get; set; }
        public bool XML { get; set; }
        public bool NoUpdate { get; set; }
        public bool NoChangelog { get; set; }
    }

    public enum ChangeType
    {
        New,
        Removed,
        Untouched,
        Timestamp
    }
}
