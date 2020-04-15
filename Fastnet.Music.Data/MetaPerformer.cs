using Fastnet.Core;
using System;

namespace Fastnet.Music.Data
{
    public class MetaPerformer : IEquatable<MetaPerformer>
    {
        public PerformerType Type { get; private set; }
        public string Name { get; private set; }
        public MetaPerformer(PerformerType t, string n)
        {
            Type = t;
            Name = n;
        }
        public void Reset(PerformerType type)
        {
            this.Type = type;
        }
        public override string ToString()
        {
            return ($"{Name}[{Type}]");
        }
        public override bool Equals(object obj)
        {
            MetaPerformer mp = obj as MetaPerformer;
            if (mp == null)
            {
                return false;
            }
            else
            {
                return Equals(mp);
            }
        }
        public bool Equals(MetaPerformer other)
        {
            if (other == null)
            {
                return false;
            }
            return this.Type == other.Type && this.Name.IsEqualIgnoreAccentsAndCase(other.Name);
        }

        public override int GetHashCode()
        {
            return (Type, Name.RemoveDiacritics().ToLower()).GetHashCode();
        }
        public static bool operator ==(MetaPerformer left, MetaPerformer right)
        {
            if (object.ReferenceEquals(left, right)) return true;
            if (left is null) return false;
            if (right is null) return false;
            return left.Equals(right);
        }
        public static bool operator !=(MetaPerformer left, MetaPerformer right)
        {
            if (object.ReferenceEquals(left, right)) return false;
            if (left is null) return true;
            if (right is null) return true;
            return !left.Equals(right);
        }
    }
}
