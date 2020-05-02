using Fastnet.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fastnet.Music.Data
{
    internal class IndianClassicalArtistQueryResult : IndianClassicalQueryResult
    {
        [DebuggerDisplay("{ShowInDebugger()}")]
        public IEnumerable<SearchKey> Artists { get; set; }
        private string ShowInDebugger()
        {
            return Artists.Select(a => a.ToString()).ToCSV();
        }
    }
}
