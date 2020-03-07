using Fastnet.Music.Data;

namespace Fastnet.Apollo.Web
{
    public class TaskQueueItem
    {
        public long TaskItemId { get; set; }
        public TaskType Type { get; set; }
        
        //[Obsolete("do I need this??")]
        //public Guid ProcessingId { get; set; }
        public override string ToString()
        {
            return $"TQI [{TaskItemId}, {Type}] added";
        }
    }

}
