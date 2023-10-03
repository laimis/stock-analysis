using System;
using System.Collections.Generic;

namespace core.Shared
{
    public static class ChartAnnotationLineType
    {
        public const string Vertical = "vertical";
        public const string Horizontal = "horizontal";
    }
    
    public static class DataPointChartType
    {
        public const string Line = "line";
        public const string Column = "column";
    }

    
    public record ChartAnnotationLine(decimal Value, string ChartAnnotationLineType);

    public class ChartDataPointContainer<T> : ChartDataPointContainerBase
    {
        public ChartDataPointContainer(
            string label,
            string chartType) : base(label, chartType)
        {
        }

        public ChartDataPointContainer(
            string label,
            string chartType,
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


    public class ChartDataPointContainerBase
    {
        protected ChartDataPointContainerBase(string label, string chartType)
        {
            Label = label;
            ChartType = chartType;
        }

        public string Label { get; }
        public string ChartType { get; }
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