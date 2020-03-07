using Fastnet.Music.Data;
//using System.IO;

namespace Fastnet.Music.Metatools
{
    public class DeleteContext
    {
        public long? ModifiedArtistId { get; private set; }
        public long? DeletedArtistId { get; private set; }
        private readonly object source;
        public DeleteContext(OpusFolder folder)
        {
            source = folder;
        }
        public DeleteContext(TaskItem item)
        {
            source = item;
        }
        public override string ToString()
        {
            switch (source)
            {
                case OpusFolder f:
                    return f.ToContextDescription();
                case TaskItem ti:
                    return ti.ToContextDescription();
            }
            return "context unknown!";
        }
        public void SetModifiedArtistId(long id)
        {
            this.ModifiedArtistId = id;
        }
        public void SetDeletedArtistId(long id)
        {
            this.DeletedArtistId = id;
            this.ModifiedArtistId = null;
        }
    }

}
