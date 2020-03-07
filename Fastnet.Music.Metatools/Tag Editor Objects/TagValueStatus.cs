using Fastnet.Core;
using Fastnet.Music.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    public class TagValueStatus
    {
        private static readonly TagValueComparer tagValueComparer = new TagValueComparer();
        private static readonly AccentAndCaseInsensitiveComparer accentsAndCaseInsensitiveComparer = new AccentAndCaseInsensitiveComparer();
        public TagNames Tag { get; set; }
        public IEnumerable<TagValue> Values { get; set; }
        /// <summary>
        /// for json conversion only
        /// </summary>
        public TagValueStatus()
        {

        }
        public TagValueStatus(TagNames tag, int number)
        {
            Tag = tag;
            SetValue(number);
            //SetSelected(false);
        }
        public TagValueStatus(TagNames tag, string value)
        {
            Tag = tag;
            SetValue(value);
            //SetSelected(false);
        }
        public TagValueStatus(TagNames tag, IEnumerable<TagValue> values, bool selectAll = false)
        {
            Tag = tag;
            Values = values
                .Distinct(tagValueComparer)
                .ToArray();
            SetSelected(selectAll);
        }
        public TagValueStatus(TagNames tag, IEnumerable<string> values, bool selectAll = false)
        {
            Tag = tag;
            //values = values.Distinct(accentsAndCaseInsensitiveComparer);
            Values = values
                .Distinct(accentsAndCaseInsensitiveComparer)
                .Select(x => new TagValue { Value = x }).Distinct(tagValueComparer)
                .ToArray();
            SetSelected(selectAll);
        }
        public TagValueStatus(TagNames tag, IEnumerable<int> values, bool selectAll = false)
        {
            Tag = tag;
            //values = values.ToArray().Distinct(accentsAndCaseInsensitiveComparer);
            Values = values
                .Select(x => new TagValue { Value = x.ToString() }).Distinct(tagValueComparer)
                .ToArray();
            SetSelected(selectAll);
        }
        private void SetSelected(bool selectAll)
        {
            if (selectAll)
            {
                foreach (var v in Values)
                {
                    v.Selected = true;
                }
            }
            else if (Values.Count() > 0)
            {
                if (Values.Where(x => x.Selected).Count() != 1)
                {
                    var toBeSelected = Values.FirstOrDefault(x => x.Selected) ?? Values.First();
                    foreach (var tv in Values)
                    {
                        tv.Selected = false;
                    }
                    toBeSelected.Selected = true;
                }
            }
        }
        public T GetValue<T>()
        {
            if (typeof(T) == typeof(int))
            {
                var r = Values.FirstOrDefault(x => x.Selected)?.Value.ToNumber() ?? 0;
                return (T)(object)r;
            }
            else if (typeof(T) == typeof(string))
            {
                var r = Values.FirstOrDefault(x => x.Selected)?.Value ?? string.Empty;
                return (T)(object)r;
            }
            return default;
        }
        public IEnumerable<string> GetValues<T>()
        {
            if (typeof(T) == typeof(int))
            {
                return Values.Where(x => x.Selected).Select(x => x.Value.ToString());
                //var r = Values.FirstOrDefault(x => x.Selected)?.Value.ToNumber() ?? 0;
                //return string.Join(", ", r);
            }
            else if (typeof(T) == typeof(string))
            {
                return Values.Where(x => x.Selected).Select(x => x.Value);
                //var r = Values.FirstOrDefault(x => x.Selected)?.Value ?? string.Empty;
                //return string.Join(", ", r);
            }
            return default;
        }
        private void SetValue(int number)
        {
            Values = new List<TagValue> { new TagValue { Selected = true, Value = number.ToString() } };
        }
        private void SetValue(string text)
        {
            Values = new List<TagValue> { new TagValue { Selected = true, Value = text } };
        }
        public override string ToString()
        {
            string getValues()
            {
                if (Values == null)
                {
                    return "(none)";
                }
                return string.Join(", ", Values.Select(v => v.Value).ToArray());
            }
            return $"{Tag.ToString()}={getValues()}";
        }
    }

}
