using System;
using System.Globalization;
using System.Windows.Data;

namespace USortThis.Core.Services
{
    /// <summary>
    /// Converter class for converting TimeSpan to seconds and vice versa.
    /// Implements <see cref="IValueConverter"/> interface.
    /// </summary>
    public class TimeSpanToSecondsConverter : IValueConverter
    {
        /// <summary>
        /// Converts a <see cref="TimeSpan"/> object to its equivalent string representation in seconds.
        /// </summary>
        /// <param name="value">The <see cref="TimeSpan"/> value to convert.</param>
        /// <param name="targetType">The type of the binding target property. This parameter is ignored in this implementation.</param>
        /// <param name="parameter">An optional parameter to specify the format. This parameter is ignored in this implementation.</param>
        /// <param name="culture">The culture-specific formatting information. </param>
        /// <returns>
        /// A string representing the total seconds of the <see cref="TimeSpan"/>; or "0" if the value is null or not a <see cref="TimeSpan"/> instance.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan ts)
                return ts.TotalSeconds.ToString(culture);
            return "0";
        }

        /// <summary>
        /// Converts a string representing seconds back to its equivalent <see cref="TimeSpan"/> object.
        /// </summary>
        /// <param name="value">The string value to convert, representing seconds.</param>
        /// <param name="targetType">The type to convert to. Must be <see cref="TimeSpan"/>. This parameter is ignored in this implementation.</param>
        /// <param name="parameter">An optional parameter. This parameter is ignored in this implementation.</param>
        /// <param name="culture">The culture-specific formatting information. </param>
        /// <returns>
        /// A <see cref="TimeSpan"/> representing the converted value; or <see cref="TimeSpan.Zero"/> if the conversion fails.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                if (double.TryParse(s, NumberStyles.Float, culture, out var d))
                    return TimeSpan.FromSeconds(d);
            }
            return TimeSpan.FromSeconds(0);
        }
    }
}
