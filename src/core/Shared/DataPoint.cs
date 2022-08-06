using System;
using System.Collections.Generic;

namespace core.Shared
{
    public class DataPointContainer<T>
    {
        public DataPointContainer(string label)
        {
            Label = label;
        }
        
        public List<DataPoint<T>> Data { get; private set; } = new List<DataPoint<T>>();
        public string Label { get; }

        public void Add(DateTimeOffset dateTime, T value)
        {
            Data.Add(new DataPoint<T>(dateTime, value));
        }
    }
    
    public class DataPoint<T>
    {
        public DataPoint(string label, T value)
        {
            Label = label;
            Value = value;
        }

        public DataPoint(DateTimeOffset timestamp, T value) : this(timestamp.ToString("yyyy-MM-dd"), value)
        {
        }

        public string Label { get; set; }
        public T Value { get; set; }
    }
}