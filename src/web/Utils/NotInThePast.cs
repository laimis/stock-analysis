using System;
using System.ComponentModel.DataAnnotations;

namespace web.Utils
{
    public class NotInThePast : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            var dt = (DateTimeOffset)value;

            return dt > DateTime.UtcNow;
        }

        public override string FormatErrorMessage(string name)
        {
            return name + " cannot be in the past.";
        }
    }
}