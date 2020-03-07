using Fastnet.Music.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    public interface IMusicSet
    {
        //IEnumerable<MusicTags> CustomTagFileTagList { get; }
        string Name { get; }
        Task<CatalogueResult> CatalogueAsync();
        //void SetMusicDb(MusicDb musicDb);
    }
}