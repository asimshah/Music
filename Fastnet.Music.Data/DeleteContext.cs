﻿using System.Collections.Generic;

namespace Fastnet.Music.Data
{
    public class DeleteContext
    {
        public List<long> ModifiedArtistList { get; set; } = new List<long>();
        public List<long> DeletedArtistList { get; set; } = new List<long>();
        private readonly object source;
        protected DeleteContext()
        {

        }
        public DeleteContext(TaskItem item)
        {
            source = item;
        }
        public override string ToString()
        {
            switch (source)
            {
                //case OpusFolder f:
                //    return f.ToContextDescription();
                case TaskItem ti:
                    return ti.ToContextDescription();
            }
            return "context unknown!";
        }
        public void SetModifiedArtistId(params long[] idList)
        {
            this.ModifiedArtistList.AddRange(idList);
        }
        public void SetDeletedArtistId(params long[] idList)
        {
            this.DeletedArtistList.AddRange(idList);
            foreach (var id in idList)
            {
                this.ModifiedArtistList.Remove(id);
            }
        }
    }
}