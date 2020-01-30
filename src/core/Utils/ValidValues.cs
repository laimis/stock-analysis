using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace core.Utils
{
    public class ValidValues : ValidationAttribute
    {
        private List<string> _values;

        public ValidValues(params string[] values)
        {
            _values = new List<string>(values);
        }

        public override bool IsValid(object value)
        {
            var s = (string)value;

            return _values.Contains(s);
        }

        public override string FormatErrorMessage(string name)
        {
            return "Invalid value specified for " + name + ". Allowed values: " + string.Join(",", _values);
        }
    }
}