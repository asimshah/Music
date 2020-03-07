using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Fastnet.Music.Core;

namespace Fastnet.Music.Data
{
    public enum TaskType
    {
        DiskPath,
        ArtistName,
        ArtistFolder,
        MusicStyle,
        Portraits,
        DeletedPath,
        ResampleWork
    }
    public class TaskItem
    {
        public long Id { get; set; }
        public TaskStatus Status { get; set; }
        public TaskType Type { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ScheduledAt { get; set; }
        public DateTimeOffset FinishedAt { get; set; }
        public MusicStyles MusicStyle { get; set; }
        public string TaskString { get; set; } // interpreted according to the Type property
        public bool ForSingles { get; set; }
        public bool Force { get; set; }
        public int RetryCount { get; set; }
        [Timestamp]
        public byte[] Timestamp { get; set; }
        public override string ToString()
        {
            return $"[TI-{Id}]";
        }
        public string ToDescription()
        {
            return $"[TI-{Id}] type {Type} for {TaskString}";
        }
        public string ToContextDescription()
        {
            //{(this.ArtistPath ?? "null")}::{(this.OpusPath ?? "null")}
            return this.ToString();
        }
    }
}
