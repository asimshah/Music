using Fastnet.Core;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    //public class OpusFolderCollection : IEnumerable<OpusFolder>
    //{
    //    private readonly MusicFolderInformation mfi;
    //    public OpusFolderCollection(MusicFolderInformation mfi)
    //    {
    //        this.mfi = mfi;
    //    }
    //    public IEnumerator<OpusFolder> GetEnumerator()
    //    {
    //        int i = 0;
    //        foreach (var pd in mfi.Paths)
    //        {
    //            var stylePath = Path.Combine(pd.DiskRoot, pd.StylePath);
    //            var artistPath = Directory.EnumerateDirectories(stylePath).SingleOrDefault(d => Path.GetFileName(d).IsEqualIgnoreAccentsAndCase(pd.ArtistPath));
    //            if (artistPath != null)
    //            {
    //                foreach (var item in Directory.EnumerateDirectories(artistPath))
    //                {
    //                    if (mfi.RequiredPrefix == null || item.StartsWithIgnoreAccentsAndCase(mfi.RequiredPrefix))
    //                    {
    //                        mfi.Paths[i].OpusPath = Path.GetRelativePath(artistPath, item).Split(Path.DirectorySeparatorChar).First();
    //                        var ofolder = new OpusFolder(mfi, i/*, item*/);
    //                        yield return ofolder;
    //                    }
    //                }
    //                if (mfi.IncludeSingles)
    //                {
    //                    mfi.Paths[i].OpusPath = null;
    //                    var ofolder = new OpusFolder(mfi, i, /*artistPath,*/ true);
    //                    yield return ofolder;
    //                }
    //            }
    //            i++;
    //        }
    //    }

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return GetEnumerator();
    //    }
    //}


}
