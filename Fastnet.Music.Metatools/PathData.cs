using Fastnet.Music.Core;
using System;
using System.Diagnostics;
using System.IO;

namespace Fastnet.Music.Metatools
{
    public class PathData
    {
        /// <summary>
        /// This is the topmost level, e.g. "D:\Music\flac"
        /// </summary>
        public string DiskRoot { get; set; }
        /// <summary>
        /// This is the portion determined by the music style, e.g. for Western Classical "Western\Classical"
        /// </summary>
        public string StylePath { get; set; }
        /// <summary>
        /// This is the portion of a path for an artist, e.g. "David Bowie"
        /// It can also be the string "Collections" (for multi artist collections)
        /// </summary>
        public string ArtistPath { get; set; }
        /// <summary>
        /// This is the portion of a path for a work/album, e.g "The Nocturnes"
        /// It does NOT include a part folder (in the case of a multi-part work)
        /// </summary>
        public string OpusPath { get; set; }
        public MusicStyles MusicStyle { get; set; }
        public bool IsGenerated { get; set; }
        public bool IsCollections { get; set; }
        public bool IsPortraits { get; set; }
        //public PathData Clone()
        //{
        //    return new PathData { DiskRoot = this.DiskRoot, StylePath = this.StylePath, ArtistPath = this.ArtistPath, OpusPath = this.OpusPath };
        //}
        public string GetFullArtistPath()
        {
            Debug.Assert(DiskRoot != null && StylePath != null && ArtistPath != null);
            return Path.Combine(this.DiskRoot, this.StylePath, this.ArtistPath);
        }
        public string GetFullOpusPath()
        {
            if(!(DiskRoot != null && StylePath != null && ArtistPath != null && OpusPath != null))
            {
                return null;
            }
            return Path.Combine(DiskRoot, StylePath, ArtistPath, OpusPath);
        }
        public string GetPortraitsPath()
        {
            Debug.Assert(DiskRoot != null && StylePath != null && IsPortraits == true);
            return Path.Combine(this.DiskRoot, this.StylePath, "$Portraits");
        }
        public string GetFolderpath()
        {
            if (OpusPath != null)
            {
                return Path.Combine(DiskRoot, StylePath, ArtistPath, OpusPath);
            }
            else if(ArtistPath != null)
            {
                return Path.Combine(this.DiskRoot, this.StylePath, this.ArtistPath);
            }
            else
            {
                return Path.Combine(this.DiskRoot, this.StylePath);
            }
        }
        public override string ToString()
        {
            return $"{this.DiskRoot}::{this.StylePath}::{(this.ArtistPath ?? "null")}::{(this.OpusPath ?? "null")}";
        }
        public override int GetHashCode()
        {
            return this.GetFolderpath().GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return this.Equals(obj as PathData);
        }
        public bool Equals(PathData pd)
        {
            // If parameter is null, return false.
            if (Object.ReferenceEquals(pd, null))
            {
                return false;
            }

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, pd))
            {
                return true;
            }
            // If parameter is null, return false.
            if (Object.ReferenceEquals(pd, null))
            {
                return false;
            }

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, pd))
            {
                return true;
            }
            return GetFolderpath() == pd.GetFolderpath()
                && IsGenerated == pd.IsGenerated && IsCollections == pd.IsCollections && pd.IsPortraits == pd.IsPortraits;
        }
        public static bool operator ==(PathData lhs, PathData rhs)
        {
            // Check for null on left side.
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                {
                    // null == null = true.
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }
        public static bool operator !=(PathData lhs, PathData rhs)
        {
            return !(lhs == rhs);
        }
    }
}
