namespace DataGridDemo
{
    using Microsoft.UI.Xaml.Data;
    using System;
    using System.Globalization;
   
    public class MassValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (value is Mass)
            {
                var v = (Mass)value;
                return v.ToString();
            }

            throw new InvalidOperationException($"Cannot convert {value?.GetType()} to {targetType}");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            var s = value as string;
            return Mass.Parse(s, null);
        }
    }
}