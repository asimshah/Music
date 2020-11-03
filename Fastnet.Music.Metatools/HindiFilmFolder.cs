namespace Fastnet.Music.Metatools
{

    public class HindiFilmFolder : WorkFolder
    {
        public string FilmName => this.workName;
        public HindiFilmFolder(MusicRoot mr, string name) : base(mr, null, name)
        {
        }
        public override string ToString()
        {
            return $"{musicRoot.MusicStyle} HindiFilmFolder: {FilmName}";
        }
    }
}