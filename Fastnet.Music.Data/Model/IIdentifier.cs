namespace Fastnet.Music.Data
{
    public interface IIdentifier
    {
        long Id { get; }
        string ToIdent() => DefaultToIdent(this);
        protected static string DefaultToIdent(IIdentifier identifier)
        {
            return identifier switch
            {
                MusicFile _ => $"[MF-{identifier.Id}]",
                IdTag _ => $"[IdT-{identifier.Id}]",
                Artist _ => $"[A-{identifier.Id}]",
                Work _ => $"[W-{identifier.Id}]",
                //ArtistWork _ => $"[AW-{identifier.Id}]",
                Composition _ => $"[C-{identifier.Id}]",
                //CompositionPerformance _ => $"[CP-{identifier.Id}]",
                Performance _ => $"[P-{identifier.Id}]",
                Performer _ => $"[Pf-{identifier.Id}]",
                Track _ => $"[T-{identifier.Id}]",
                Device _ => $"[D-{identifier.Id}]",
                Playlist _ => $"[PL-{identifier.Id}]",
                PlaylistItem _ => $"[PLi-{identifier.Id}]",
                TaskItem _ => $"[Ti-{identifier.Id}]",
                _ => $"[U-{identifier.Id}]"
            };
        }
    }
    public abstract class EntityBase : IIdentifier
    {
        public abstract long Id { get; set; }
        public string ToIdent()
        {
            return IIdentifier.DefaultToIdent(this);
            //return ((IIdentifier)this).ToIdent();
        }
    }
    public interface IManyToManyIdentifier
    {
        string ToIdent();
        protected static string ToIdent(IIdentifier first, IIdentifier second)
        {
            return $"{first.ToIdent()}{second.ToIdent()}".Replace("][", "+");
        } 
    }
}
