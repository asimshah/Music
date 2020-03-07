using Fastnet.Core;
using Fastnet.Music.Core;
using System.Collections.Generic;

namespace Fastnet.Music.Metatools
{
    public class TagValueComparer : IEqualityComparer<TagValue>
    {
        private static AccentAndCaseInsensitiveComparer accentAndCaseInsensitiveComparer = new AccentAndCaseInsensitiveComparer();
        public bool Equals(TagValue x, TagValue y)
        {
            return accentAndCaseInsensitiveComparer.Compare(x.Value, y.Value) == 0;
        }
        public int GetHashCode(TagValue obj)
        {
            return obj.Value.GetHashCode();
        }
    }

}
