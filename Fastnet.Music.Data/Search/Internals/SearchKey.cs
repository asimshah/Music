namespace Fastnet.Music.Data
{
    //public static class __xxx
    //{
    //    private static Regex inan = new Regex(@"[^a-zA-Z0-9\p{L}]", RegexOptions.IgnoreCase);
    //    private static Regex splitToWords = new Regex(@"(\b[^\s]+\b)", RegexOptions.IgnoreCase);
    //}

    internal class SearchKey
    {
        public long Key { get; set; }
        public string Name { get; set; }
        public override string ToString()
        {
            return $"[SK-{Key}:{Name}]";
        }
    }
}
