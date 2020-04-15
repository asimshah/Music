using Fastnet.Music.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    public interface IMusicSet
    {
        string Name { get; }
        Task<CatalogueResultBase> CatalogueAsync();
    }
}