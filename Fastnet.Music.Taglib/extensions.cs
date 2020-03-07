using System;
using System.Collections.Generic;

namespace Fastnet.Music.TagLib
{
    public static class extensions
    {
        public static string GetComposition(this File file)
        {
            var id3v2Tags = (TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2);
            return id3v2Tags?.GetUserTextAsString("composition", false);
        }
        public static string GetWork(this File file)
        {
            var id3v2Tags = (TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2);

            return id3v2Tags?.GetUserTextAsString("work", false);
        }
    }
    public abstract partial class Tag
    {
        //public virtual string Work
        //{
        //    get { return null; }
        //    set { }
        //}
        //public virtual string Composition
        //{
        //    get { return null; }
        //    set { }
        //}
    }
}
namespace Fastnet.Music.TagLib.Id3v2
{
    public partial class Tag : TagLib.Tag, IEnumerable<Frame>, ICloneable
    {
        public void SetApolloString(string key, string value)
        {
            SetUserTextAsString(key, value, false);
        }
        public void SetApolloStrings(string key, string[] values)
        {
            var text = string.Join("|", values);
            SetUserTextAsString(key, text, false);
        }
        public string GetApolloString(string key)
        {
            return GetUserTextAsString(key, false);

        }
        public string[] GetApolloStrings(string key)
        {
            var text = GetUserTextAsString(key, false);
            if (text != null)
            {
                return text.Split('|');
            }
            return null;

        }
        //public override string Work
        //{
        //    get => GetUserTextAsString("WORK", false);
        //    set => SetUserTextAsString("WORK", value, false);
        //}
        //public override string Composition
        //{
        //    get => GetUserTextAsString("COMPOSITION", false);
        //    set => SetUserTextAsString("COMPOSITION", value, false);
        //}
    }
}
