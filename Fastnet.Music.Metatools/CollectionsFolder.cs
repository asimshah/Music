using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Microsoft.Extensions.Logging;

namespace Fastnet.Music.Metatools
{
    public class CollectionsFolder //: MusicMetaData
    {
        protected readonly MusicOptions musicOptions;
        protected readonly MusicStyles musicStyle;
        protected readonly ILogger log;
        public CollectionsFolder(MusicOptions options, MusicStyles style) //: base(options, style)
        {
            musicOptions = options;
            musicStyle = style;
            log = ApplicationLoggerFactory.CreateLogger<CollectionsFolder>();
        }

        public OpusFolderCollection GetOpusFolders(string requiredPrefix = null)
        {
            return new OpusFolderCollection(new MusicFolderInformation
            {
                //sCollection = true,
                MusicOptions = musicOptions,
                MusicStyle = musicStyle,
                Paths = MusicMetaDataMethods.GetPathDataList(musicOptions, musicStyle, "collections"),
                IncludeSingles = false,
                RequiredPrefix = requiredPrefix
            });
        }
    }


}
