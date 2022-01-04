using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SpeedMann.PvPRework
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        [XmlArray(ElementName = "Elements")]
        [XmlArrayItem(ElementName = "Element")]
        public List<KeyValueElement<TKey, TValue>> serializableDictionary
        {
            get
            {
                if (RealDictionary != null)
                    return RealDictionary.Select(x => new KeyValueElement<TKey, TValue> { Key = x.Key, Value = x.Value }).ToList();
                return new List<KeyValueElement<TKey, TValue>>();
            }
            set { RealDictionary = value.ToDictionary(x => x.Key, x => x.Value); }
        }
        [XmlIgnore]
        public Dictionary<TKey, TValue> RealDictionary = new Dictionary<TKey, TValue>();


        public int Count()
        {
            if (RealDictionary != null)
                return RealDictionary.Count();
            return 0;
        }
        public bool ContainsKey(TKey key)
        {
            if (RealDictionary != null)
                return RealDictionary.ContainsKey(key);
            return false;
        }
        public bool TryGetValue(TKey key, out TValue value)
        {

            return RealDictionary.TryGetValue(key, out value);
        }
    }

    [Serializable]
    public class KeyValueElement<TKey, TValue>
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }
    }
}