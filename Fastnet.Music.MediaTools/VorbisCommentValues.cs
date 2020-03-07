using System.Collections.Generic;

namespace Fastnet.Music.MediaTools
{
    /// <summary>
    /// The value or values of a single Vorbis Comment Field.
    /// </summary>
    public class VorbisCommentValues : List<string>
    {
        /// <summary>
        /// Creates an empty vorbis comment.
        /// </summary>
        public VorbisCommentValues() { }

        /// <summary>
        /// Creates a vorbis comment with one value.
        /// </summary>
        public VorbisCommentValues(string value)
        {
            this.Add(value);
        }

        /// <summary>
        /// Creates a vorbis comment with the given values.
        /// </summary>
        public VorbisCommentValues(IEnumerable<string> values)
        {
            this.AddRange(values);
        }

        /// <summary>
        /// The first value of the list of values.
        /// </summary>
        /// <remarks></remarks>
        public string Value
        {
            get
            {
                if (this.Count == 0) { return string.Empty; }
                return this[0];
            }
            set
            {
                if (this.Count == 0) { this.Add(value); }
                else { this[0] = value; }
            }
        }
    }
}
