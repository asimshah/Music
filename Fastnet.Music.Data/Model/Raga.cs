using System.ComponentModel.DataAnnotations;

namespace Fastnet.Music.Data
{
    public class Raga : EntityBase
    {
        public override long Id { get; set; }
        [Required, MaxLength(512)]
        public string Name { get; set; }
        [MaxLength(512)]
        public string AlphamericName { get; set; }
        [MaxLength(ILengthConstants.MaxCompressedNameLength)]
        public string CompressedName { get; set; }
    }
}
