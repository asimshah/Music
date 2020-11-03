using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    public abstract class AudioFile //: BaseMusicMetaData
    {
        //protected readonly MusicOptions musicOptions;
        //protected MusicStyles musicStyle { get; private set; }
        protected readonly ILogger log;
        private bool _isSingle;
        public FileInfo File { get; set; }
        public bool IsSingle => _isSingle;//{ get; set; }
        public OpusPart Part { get; set; } = null;
        public AudioFile(FileInfo fi) //: base(musicOptions, musicStyle)
        {
            this.File = fi;
            //this.musicOptions = musicOptions;
            log = ApplicationLoggerFactory.CreateLogger(this.GetType());
            //this.musicStyle = musicStyle;
        }
        /// <summary>
        /// Important: getting audio properties is always a 'slow' process
        /// </summary>
        /// <returns></returns>
        public abstract AudioProperties GetAudioProperties();
        internal void SetAsSingle()
        {
            _isSingle = true;
        }
    }
    public static partial class Extensions
    {

    }
}
