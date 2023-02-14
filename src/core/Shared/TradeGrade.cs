using System;

namespace core.Shared
{
    public struct TradeGrade
    {
        private readonly string _grade;

        public TradeGrade(string grade)
        {
            if (string.IsNullOrWhiteSpace(grade))
            {
                throw new ArgumentException(nameof(grade), "Grade cannot be blank");
            }
            
            // right now we allow only A, B, C
            // this feels like should be configurable by trader?
            if (!grade.Equals("A", StringComparison.OrdinalIgnoreCase) &&
                !grade.Equals("B", StringComparison.OrdinalIgnoreCase) &&
                !grade.Equals("C", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(nameof(grade), "Grade must be A, B, or C");
            }

            _grade = grade.ToUpper();
        }

        public static implicit operator string(TradeGrade g) => g._grade;
        public static implicit operator TradeGrade(string g) => new TradeGrade(g);
        public string Value => _grade;
    }
}