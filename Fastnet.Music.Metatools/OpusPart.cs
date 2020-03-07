namespace Fastnet.Music.Metatools
{
    public class OpusPart
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public override string ToString()
        {
            return $"{Number} ({Name})";
        }
    }
}
