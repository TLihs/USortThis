using System;
using System.Globalization;
using System.Windows.Data;

namespace USortThis.Core.ViewModels
{
    /// <summary>
    /// A converter that checks for equality between multiple values.
    /// </summary>
    public class EqualityMultiConverter : IMultiValueConverter
    {
        /// <summary>
        /// Compares two or more values for equality. Checks if values are the same instance, then checks properties by Id, and finally falls back to default equality comparison.
        /// </summary>
        /// <param name="values">The array of values to compare.</param>
        /// <param name="targetType">The desired type of the binding target.</param>
        /// <param name="parameter">An optional parameter to the converter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>True if the values are equal, otherwise false.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;
            var a = values[0];
            var b = values[1];
            if (a == null && b == null)
                return true;
            if (a == null || b == null)
                return false;
            // If objects are same reference or equal by Id (FileItemViewModel has Id)
            if (ReferenceEquals(a, b))
                return true;

            // Try to compare by Id property
            try
            {
                var aid = a.GetType().GetProperty("Id")?.GetValue(a);
                var bid = b.GetType().GetProperty("Id")?.GetValue(b);
                if (aid != null && bid != null)
                    return aid.Equals(bid);
            }
            catch { }

            return a.Equals(b);
        }

        /// <summary>
        /// This method is not supported for equality conversion and will always throw a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <param name="targetTypes">The target types for the conversion.</param>
        /// <param name="parameter">An optional parameter to the converter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <exception cref="NotSupportedException">Always thrown because converting back is not supported.</exception>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
