using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Simulation.UI.Converters
{
    public class LightToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Зеленый" => Brushes.Green,
                "Красный" => Brushes.Red,
                "️ АВАРИЯ" => Brushes.Orange,
                _ => Brushes.Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}