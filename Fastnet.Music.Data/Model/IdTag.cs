using System.ComponentModel.DataAnnotations;

namespace Fastnet.Music.Data
{
    public class IdTag : IIdentifier
    {
        public long Id { get; set; }
        [MaxLength(64)]
        public string Name { get; set; }
        public string Value { get; set; }
        public long PictureChecksum { get; set; }
        public byte[] PictureData { get; set; }
        [MaxLength(64)]
        public string PictureMimeType { get; set; }
        public long MusicFileId { get; set; }
        public virtual MusicFile MusicFile { get; set; }
        public override string ToString()
        {
            return $"{Name}={Value}";
        }
    }
}
