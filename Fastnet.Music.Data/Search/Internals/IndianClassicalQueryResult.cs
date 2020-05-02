using System.Collections.Generic;

namespace Fastnet.Music.Data
{
    internal interface IndianClassicalQueryResult : IQueryResult
    {        
        IEnumerable<SearchKey> Artists { get; set; }
    }
}
