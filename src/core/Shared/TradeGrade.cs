using System;

namespace core.Shared
{
    public readonly struct TradeGrade : IComparable
    {
        public TradeGrade(string grade)
        {
            if (string.IsNullOrWhiteSpace(grade))
            {
                throw new ArgumentException("Grade cannot be blank", nameof(grade));
            }
            
            // right now we allow only A, B, C
            // this feels like should be configurable by trader?
            if (!grade.Equals("A", StringComparison.OrdinalIgnoreCase) &&
                !grade.Equals("B", StringComparison.OrdinalIgnoreCase) &&
                !grade.Equals("C", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Grade must be A, B, or C", nameof(grade));
            }

            Value = grade.ToUpper();
        }

        public string Value { get; }

        public int CompareTo(object obj)
        {
            if (obj is TradeGrade other)
            {
                return string.Compare(Value, other.Value, StringComparison.Ordinal);
            }

            throw new ArgumentException("Object is not a TradeGrade");
        }
    }
}