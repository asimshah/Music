using System;
using System.Collections.Generic;
using System.IO;

namespace Fastnet.Music.MediaTools
{
    // see https://github.com/Zeugma440/atldotnet
    public class FlacTools : IDisposable
    {
        private static readonly byte[] magicFlacMarker = { 0x66, 0x4C, 0x61, 0x43 }; // fLa
        private Stream dataStream;
        private List<MetadataBlock> metadata;

        // Some housekeeping for the file save
        private long frameStart;
        private string filePath = String.Empty;
        private StreamInfo streamInfo;
        private ApplicationInfo applicationInfo;
        private VorbisComment vorbisComment;
        private CueSheet cueSheet;
        private SeekTable seekTable;
        private Padding padding;
        public StreamInfo StreamInfo { get { return this.streamInfo; } }
        public List<MetadataBlock> Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = new List<MetadataBlock>();
                }
                return this.metadata;
            }
        }
        public FlacTools(string path)
        {
            this.filePath = path;
            this.dataStream = File.OpenRead(path);
            this.Initialize();
        }
        public VorbisComment GetVorbisCommenTs()
        {
            return this.vorbisComment;
        }
        public TimeSpan GetDuration()
        {
            return this.StreamInfo.GetDuration();
        }
        public int GetBitsPerSample()
        {
            return this.StreamInfo.BitsPerSample;
        }
        protected void Initialize()
        {
            VerifyFlacIdentity();
            ReadMetadata();
        }
        private void VerifyFlacIdentity()
        {
            byte[] data = new byte[4];

            this.dataStream.Read(data, 0, 4);
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != magicFlacMarker[i])
                {
                    throw new FormatException("Flac identity not found");
                    //throw new FlacLibSharp.Exceptions.FlacLibSharpInvalidFormatException("In Verify Flac Identity");
                }
            }

            //try
            //{
            //    this.dataStream.Read(data, 0, 4);
            //    for (int i = 0; i < data.Length; i++)
            //    {
            //        if (data[i] != magicFlacMarker[i])
            //        {
            //            throw new FormatException("Flac identity not found");
            //            //throw new FlacLibSharp.Exceptions.FlacLibSharpInvalidFormatException("In Verify Flac Identity");
            //        }
            //    }
            //}
            //catch (Exception xe)
            //{
            //    throw new FormatException("In Verify Flac Identity");
            //    //throw new FlacLibSharp.Exceptions.FlacLibSharpInvalidFormatException("In Verify Flac Identity");
            //}
        }
        protected void ReadMetadata()
        {
            bool foundStreamInfo = false;
            MetadataBlock lastMetaDataBlock = null;
            do
            {
                lastMetaDataBlock = MetadataBlock.Create(this.dataStream);
                this.Metadata.Add(lastMetaDataBlock);
                switch (lastMetaDataBlock.Header.Type)
                {
                    case MetadataBlockHeader.MetadataBlockType.StreamInfo:
                        foundStreamInfo = true;
                        this.streamInfo = (StreamInfo)lastMetaDataBlock;
                        break;
                    case MetadataBlockHeader.MetadataBlockType.Application:
                        this.applicationInfo = (ApplicationInfo)lastMetaDataBlock;
                        break;
                    case MetadataBlockHeader.MetadataBlockType.CueSheet:
                        this.cueSheet = (CueSheet)lastMetaDataBlock;
                        break;
                    case MetadataBlockHeader.MetadataBlockType.Seektable:
                        this.seekTable = (SeekTable)lastMetaDataBlock;
                        break;
                    case MetadataBlockHeader.MetadataBlockType.VorbisComment:
                        this.vorbisComment = (VorbisComment)lastMetaDataBlock;
                        break;
                    case MetadataBlockHeader.MetadataBlockType.Padding:
                        this.padding = (Padding)lastMetaDataBlock;
                        break;
                }
            } while (!lastMetaDataBlock.Header.IsLastMetaDataBlock);

            if (!foundStreamInfo)
                throw new Exception("Stream Info missing");
            //throw new Exceptions.FlacLibSharpStreamInfoMissing();

            // Remember where the frame data starts
            frameStart = this.dataStream.Position;
        }

        public void Dispose()
        {
            if(this.dataStream != null)
            {
                this.dataStream.Dispose();
            }
        }
    }
}
