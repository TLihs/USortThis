using System;
using System.Globalization;
using System.Windows.Data;

namespace USortThis.Core.Services
{
    public class TimeSpanToSecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan ts)
                return ts.TotalSeconds.ToString(culture);
            return "0";
        }

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
