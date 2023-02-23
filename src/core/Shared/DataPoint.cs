using System;
using System.Collections.Generic;

namespace core.Shared
{
    public class DataPointContainer<T> : DataPointContainerBase
    {
        public DataPointContainer(string label, DataPointChartType chartType) : base(label, chartType)
        {
        }
        
        public List<DataPoint<T>> Data { get; private set; } = new List<DataPoint<T>>();
        

        public void Add(DateTimeOffset label, T value)
        {
            Data.Add(new DataPoint<T>(label, value));
        }

        public void Add(string label, T value)
        {
            Data.Add(new DataPoint<T>(label, value));
        }
    }

    public enum DataPointChartType { line, bar }


    public class DataPointContainerBase
    {
        public DataPointContainerBase(string label, DataPointChartType chartType)
        {
            Label = label;
            ChartType = chartType;
        }

        public string Label { get; }
        public DataPointChartType ChartType { get; }
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