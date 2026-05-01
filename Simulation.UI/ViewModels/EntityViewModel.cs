using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Simulation.UI.ViewModels
{
    public class EntityViewModel : INotifyPropertyChanged
    {
        private double _x;
        private double _y;
        private string _type = string.Empty; // Исправлено предупреждение CS8618
        private bool _isVisible = true;
        public Guid Id { get; set; }

        public double X { get => _x; set => SetProperty(ref _x, value); }
        public double Y { get => _y; set => SetProperty(ref _y, value); }
        public string Type { get => _type; set => SetProperty(ref _type, value); }
        public bool IsVisible { get => _isVisible; set => SetProperty(ref _isVisible, value); }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (!Equals(field, value)) { field = value; OnPropertyChanged(name); }
        }
    }
}