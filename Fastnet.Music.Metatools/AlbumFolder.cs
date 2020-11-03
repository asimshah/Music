using System.Diagnostics;

namespace Fastnet.Music.Metatools
{
    public class AlbumFolder : WorkFolder
    {
        public static AlbumFolder ForCollection(MusicRoot mr, string collectionAlbumName)
        {
            return new AlbumFolder(mr, collectionAlbumName);
        }
        public static AlbumFolder ForArtist(MusicRoot mr, string artistName, string albumName)
        {
            return new AlbumFolder(mr, artistName, albumName);
        }
        public static AlbumFolder ForArtistSingles(MusicRoot mr, string artistName)
        {
            return new AlbumFolder(mr, artistName, AlbumType.Singles);
        }
        public string AlbumName => this.workName;
        public string ArtistName => this.artistName;
        private AlbumFolder(MusicRoot mr, string name) : base(mr, null, name, AlbumType.Collection)
        {
        }
        private AlbumFolder(MusicRoot mr, string artistName, string workName) : base(mr, artistName, workName)
        {
        }
        private AlbumFolder(MusicRoot mr, string artistName, AlbumType type) : base(mr, artistName,  "Singles", AlbumType.Singles)
        {
            Debug.Assert(type == AlbumType.Singles);
        }
        public override string ToString()
        {
            return Type switch
            {
                AlbumType.Normal => $"{musicRoot.MusicStyle} AlbumFolder: {ArtistName}, {AlbumName}",
                AlbumType.Singles => $"{musicRoot.MusicStyle} AlbumFolder: {ArtistName}, Singles",
                AlbumType.Collection => $"{musicRoot.MusicStyle} AlbumFolder: Collection {AlbumName}",
                _ => $"{musicRoot.MusicStyle} AlbumFolder !error!"
            };

        }
    }
}