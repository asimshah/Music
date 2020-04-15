using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Microsoft.Extensions.Logging;

namespace Fastnet.Music.Metatools
{
    public class ArtistFolder //: MusicMetaData
    {
        public string ArtistName => artistName;
        private readonly string artistName;
        protected readonly MusicOptions musicOptions;
        protected readonly MusicStyles musicStyle;
        protected readonly ILogger log;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="style"></param>
        /// <param name="artistName">This name will be matched to disk folders ignoring accents and case</param>
        public ArtistFolder(MusicOptions options, MusicStyles style, string artistName) //: base(options, style)
        {

            this.artistName = artistName;
            musicOptions = options;
            musicStyle = style;
            log = ApplicationLoggerFactory.CreateLogger<ArtistFolder>();
        }

        public OpusFolderCollection GetOpusFolders(string requiredPrefix = null)
        {
            return new OpusFolderCollection(new MusicFolderInformation
            {
                //IsCollection = false,
                MusicOptions = musicOptions,
                MusicStyle = musicStyle,
                Paths = MusicMetaDataMethods.GetPathDataList(musicOptions, musicStyle, artistName),
                IncludeSingles = musicStyle == MusicStyles.Popular, // causes the collection to include singles
                RequiredPrefix = requiredPrefix
            });
        }
        public override string ToString()
        {
            return $"artistfolder for {ArtistName}";
        }


    }


}
