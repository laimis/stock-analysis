using System;
using System.Collections.Generic;

namespace core.Shared
{
    public enum ChartAnnotationLineType { vertical, horizontal}
    
    public record ChartAnnotationLine(decimal value, string label, ChartAnnotationLineType chartAnnotationLineType);

    public class ChartDataPointContainer<T> : ChartDataPointContainerBase
    {
        public ChartDataPointContainer(
            string label,
            DataPointChartType chartType) : base(label, chartType)
        {
        }

        public ChartDataPointContainer(
            string label,
            DataPointChartType chartType,
            ChartAnnotationLine annotationLine) : base(label, chartType)
        {
            AnnotationLine = annotationLine;
        }

        public List<DataPoint<T>> Data { get; } = new();
        public ChartAnnotationLine AnnotationLine { get; }

        public void Add(DateTimeOffset label, T value)
        {
            Data.Add(new DataPoint<T>(label, value));
        }

        public void Add(string label, T value)
        {
            Data.Add(new DataPoint<T>(label, value));
        }
    }

    public enum DataPointChartType { line, column }


    public class ChartDataPointContainerBase
    {
        public ChartDataPointContainerBase(string label, DataPointChartType chartType)
        {
            Label = label;
            ChartType = chartType;
        }

        public string Label { get; }
        public DataPointChartType ChartType { get; }
    }

    public class DataPoint<T>
    {
        private DataPoint(string label, T value, bool isDate)
        {
            Label = label;
            Value = value;
            IsDate = isDate;
        }
        
        public DataPoint(string label, T value) : this(label, value, isDate: false)
        {
        }

        public DataPoint(DateTimeOffset timestamp, T value) : this(timestamp.ToString("yyyy-MM-dd"), value, isDate: true)
        {
        }

        public string Label { get; }
        public T Value { get; }
        public bool IsDate { get; }
    }
}