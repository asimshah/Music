using System;

namespace Fastnet.Music.Data
{
    public interface INameParsing
    {
        //string MbidName { get; set; }
        string IdTagName { get; set; }
        string UserProvidedName { get; set; }
        Guid UID { get; set; }
        LibraryParsingStage ParsingStage { get; set; }
    }
}
