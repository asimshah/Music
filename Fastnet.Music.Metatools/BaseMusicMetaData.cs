using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

//namespace Fastnet.Music.Metatools
//{
//    public abstract class BaseMusicMetaData
//    {
//        //protected internal readonly MusicOptions musicOptions;
//        //protected internal MusicStyles musicStyle { get; private set; }
//        //protected readonly ILogger log;
//        //public BaseMusicMetaData(MusicOptions musicOptions, MusicStyles musicStyle) : this(musicOptions)
//        //{
//        //    this.musicStyle = musicStyle;
//        //}
//        //public BaseMusicMetaData(MusicOptions musicOptions)
//        //{
//        //    this.musicOptions = musicOptions;
//        //    log = ApplicationLoggerFactory.CreateLogger(this.GetType());
//        //}
//        //protected void SetStyle(MusicStyles style)
//        //{
//        //    this.musicStyle = style;
//        //}
//        //protected IEnumerable<PathData> GetPathDataList()
//        //{
//        //    var list = new List<PathData>();
//        //    var style = musicOptions.Styles.Single(s => s.Style == musicStyle);
//        //    if (style.Enabled == false)
//        //    {
//        //        log.Warning($"Suspicious call - style {style.Style} is not enabled!");
//        //    }
//        //    foreach (var rootFolder in new MusicSources(musicOptions).OrderBy(s => s.IsGenerated).ThenBy(s => s.DiskRoot))
//        //    {
//        //        foreach (var setting in style.Settings)
//        //        {
//        //            var pd = new PathData { DiskRoot = rootFolder.DiskRoot, StylePath = setting.Path };
//        //            list.Add(pd);
//        //        }
//        //    }
//        //    return list;
//        //}
//        //protected (PathData pd, MusicStyles ms, bool enabled) GetPathData(string fullPath)
//        //{
//        //    var pd = new PathData();
//        //    MusicStyles? style = null;
//        //    var styleIsEnabled = false;
//        //    foreach (var source in new MusicSources(musicOptions))
//        //    {
//        //        var remainder = Path.GetRelativePath(source.DiskRoot, fullPath);
//        //        if (remainder.Length > 0 && !remainder.StartsWith(".."))
//        //        {
//        //            pd.DiskRoot = source.DiskRoot;
//        //            foreach (var si in new MusicStyleCollection(musicOptions, true))
//        //            {
//        //                foreach (var setting in si.Settings)
//        //                {
//        //                    if (remainder.StartsWithIgnoreAccentsAndCase(setting.Path))
//        //                    {
//        //                        style = si.Style;
//        //                        styleIsEnabled = si.Enabled;
//        //                        pd.StylePath = setting.Path;
//        //                        remainder = Path.GetRelativePath(setting.Path, remainder);
//        //                        var folderParts = remainder.Split(Path.DirectorySeparatorChar);
//        //                        pd.ArtistPath = folderParts[0];
//        //                        if (folderParts.Length > 1)
//        //                        {
//        //                            pd.OpusPath = folderParts[1];
//        //                        }
//        //                        break;
//        //                    }
//        //                }
//        //            }
//        //            break;
//        //        }
//        //    }
//        //    if (!style.HasValue)
//        //    {
//        //        throw new Exception($"Path {fullPath} is not valid - needs to be a folder within the apollo folder scheme");
//        //    }
//        //    return (pd, style.Value, styleIsEnabled);
//        //}
//    }
//}
