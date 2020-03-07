using Microsoft.EntityFrameworkCore;
using System;

namespace Fastnet.Music.Data
{
    [Owned]
    public class Image
    {
        public string Sourcefile { get; set; }
        public long Filelength { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public string MimeType { get; set; }
        public byte[] Data { get; set; }
    }

}
