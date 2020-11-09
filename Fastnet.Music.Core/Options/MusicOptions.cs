using Fastnet.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fastnet.Music.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class MusicSource
    {
        /// <summary>
        /// 
        /// </summary>
        public string DiskRoot { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// Content in this root is generated (by the system)
        /// (e.g by resampling)
        /// </summary>
        public bool IsGenerated { get; set; } = false;
        /// <summary>
        /// Content of this root is resampled (to the specified resampling directory)
        /// </summary>
        public bool ResamplingEnabled { get; set; } = false;
        /// <summary>
        /// Content is resampled to this directory if ResamplingEnabled = true
        /// note: the resampling agent must be running in the local computer for this to work
        /// </summary>
        public string ResampleTo { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class StyleSetting
    {
        /// <summary>
        /// 
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Mood { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class StyleInformation
    {
        /// <summary>
        /// 
        /// </summary>
        public MusicStyles Style { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Name { get { return Style.ToDescription(); } }
        /// <summary>
        /// 
        /// </summary>
        public StyleSetting[] Settings { get; set; } = new StyleSetting[0];
        /// <summary>
        /// 
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Filter { get; set; } = false;
        /// <summary>
        /// used if Filter == true
        /// names are film names for Hindi Films, artist names for all other styles
        /// </summary>
        public string[] IncludeNames { get; set; } = new string[0];
        /// <summary>
        /// 
        /// </summary>
        [Obsolete]
        public string[] IncludeFolders { get; set; } = new string[0];
        /// <summary>
        /// 
        /// </summary>
        [Obsolete]
        public string[] IncludeArtists { get; set; } = new string[0];
    }
    /// <summary>
    /// 
    /// </summary>
    public class MusicOptions
    {
        public bool DisableResampling { get; set; }
        /// <summary>
        /// Interval to wait for change notifications to stop (after they start!), default is 10 secs
        /// </summary>
        [Obsolete]
        public TimeSpan FolderChangeAfterChangesInterval { get; set; } = TimeSpan.FromSeconds(10);
        /// <summary>
        /// Interval at which to check that source folders are accessible, default is 15 secs
        /// </summary>
        public TimeSpan FolderChangeDiskAccessCheckInterval { get; set; } = TimeSpan.FromSeconds(15);
        /// <summary>
        /// Interval at which to check if it time to process folder changes, default is 3 sec
        /// </summary>
        public TimeSpan FolderChangePollingInterval { get; set; } = TimeSpan.FromSeconds(3);
        public bool TimeCatalogueSteps { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Obsolete]
        public string[] MusicFileExtensions { get; set; } = new string[] { ".mp3", ".flac", ".m4a" };
        /// <summary>
        /// 
        /// </summary>
        public int SearchPrefixLength { get; set; } = 3;
        public bool AutoConfigureSources { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public MusicSource[] Sources { get; set; } = new MusicSource[0];
        /// <summary>
        /// 
        /// </summary>
        public StyleInformation[] Styles { get; set; } = new StyleInformation[0];
        public string[] CoverFilePatterns { get; set; } = new string[]
                {
                    "*cover.jpg",
                    "*cover.jpeg",
                    "*cover.png",
                    "*front.jpg",
                    "*front.jpeg",
                    "*front.png",
                    "*folder.jpg",
                    "*folder.jpeg",
                    "*folder.png",
                };
        /// <summary>
        /// An array of string arrays eaxch containing a variable number of aliases
        /// if any alias is matched, replace it with the first member of the array
        /// </summary>
        public List<List<string>> Aliases { get; set; } = new List<List<string>>();
        public int MaxTaskThreads { get; set; }
        public int MaxTaskRetries { get; set; }
        public bool AllowOutOfDateGeneratedFiles { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("Sources:");
            foreach (var source in Sources)
            {
                sb.AppendLine($"    {source.DiskRoot} [{(source.Enabled ? "enabled" : "disabled")}]");
            }
            sb.AppendLine("Styles:");
            foreach (var style in Styles)
            {

                sb.AppendLine($"    {style.Name} [{(style.Enabled ? "enabled" : "disabled")}]");
                if (style.Enabled)
                {
                    foreach (var s in style.Settings)
                    {
                        sb.AppendLine($"        style path: {s.Path}, mood: {s.Mood}");
                    }
                }
            }
            return sb.ToString();
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class MusicSources : IEnumerable<MusicSource>
    {
        private readonly MusicOptions musicOptions;
        private readonly bool includeGenerated;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="musicOptions"></param>
        /// <param name="includeGenerated"></param>
        public MusicSources(MusicOptions musicOptions, bool includeGenerated = false)
        {
            this.musicOptions = musicOptions;
            this.includeGenerated = includeGenerated;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<MusicSource> GetEnumerator()
        {
            foreach (var source in musicOptions.Sources.Where(x => x.Enabled && x.IsGenerated == false || includeGenerated == true).OrderBy(x => x.DiskRoot))
            {
                yield return source;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class MusicStyleCollection : IEnumerable<StyleInformation>
    {
        private readonly MusicOptions musicOptions;
        private readonly bool includeDisabled;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="musicOptions"></param>
        /// <param name="includeDisabled"></param>
        public MusicStyleCollection(MusicOptions musicOptions, bool includeDisabled = false)
        {
            this.musicOptions = musicOptions;
            this.includeDisabled = includeDisabled;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<StyleInformation> GetEnumerator()
        {
            foreach (var si in musicOptions.Styles.Where(x => x.Enabled || (includeDisabled == true)).OrderBy(x => x.Style))
            {
                yield return si;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
