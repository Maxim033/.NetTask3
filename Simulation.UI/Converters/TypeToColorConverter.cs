using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Simulation.UI.Converters
{
    public class TypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Car_Emergency" => Brushes.Gray,  // Серая точка аварийной службы
                "Car_Towed"     => Brushes.LightBlue, // Сбитое авто (чуть светлее)
                "Car"           => Brushes.Blue,
                "Pedestrian"    => Brushes.Green,
                _               => Brushes.Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}