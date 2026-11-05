using System;
using System.Globalization;
using System.Windows.Data;

namespace USortThis.Core.ViewModels
{
    public class EqualityMultiConverter : IMultiValueConverter
    {
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

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
