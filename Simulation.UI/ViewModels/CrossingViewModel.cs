using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Simulation.Core.Interfaces;
using Simulation.Core.Models;
using Simulation.Core.Simulation;

namespace Simulation.UI.ViewModels
{
    public class CrossingViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly CrossingSimulator _simulator;
        private readonly Dispatcher _uiDispatcher;
        private string _lightState = "Красный";
        
        public ObservableCollection<EntityViewModel> Entities { get; } = new();
        public string LightState { get => _lightState; private set => SetProperty(ref _lightState, value); }
        public event PropertyChangedEventHandler? PropertyChanged;

        public CrossingViewModel(SimulationConfig config, IEmergencyService emergencyService)
        {
            _uiDispatcher = Application.Current.Dispatcher;
            _simulator = new CrossingSimulator(config, emergencyService);
            
            _simulator.LightChanged += OnLightChanged;
            _simulator.EntityMoved += OnEntityMoved;
            _simulator.AccidentOccurred += OnAccidentOccurred;

            CreateInitialEntities(config);
            _ = StartAsync();
        }

        private void CreateInitialEntities(SimulationConfig config)
        {
            double centerX = config.CanvasWidth / 2;
            
            // 🚶 Пешеходы стартуют с тротуаров (±100px от центра дороги)
            _simulator.AddEntity(new Pedestrian 
            { 
                X = centerX - 100, 
                Y = config.CanvasHeight / 2 - 20, 
                Speed = config.PedestrianSpeed, 
                IsActive = true, 
                Direction = 1 
            });
            
            _simulator.AddEntity(new Pedestrian 
            { 
                X = centerX - 120, 
                Y = config.CanvasHeight / 2 + 20, 
                Speed = config.PedestrianSpeed, 
                IsActive = true, 
                Direction = 1 
            });
            
            _simulator.AddEntity(new Pedestrian 
            { 
                X = centerX + 100, 
                Y = config.CanvasHeight / 2, 
                Speed = config.PedestrianSpeed, 
                IsActive = true, 
                Direction = -1 
            });

            // 🚗 Машины (2 полосы)
            for (int i = 0; i < 5; i++)
            {
                double xOffset = (i % 2 == 0) ? -20 : 20;
                _simulator.AddEntity(new Car
                {
                    X = centerX + xOffset,
                    Y = -100 - (i * 140),
                    Speed = config.CarSpeed,
                    IsActive = true,
                    Lane = i % 2,
                    Direction = 1,
                    IsEmergency = false,
                    IsStopped = false
                });
            }
        }

        public async Task StartAsync() => await _simulator.StartAsync();
        public void Stop() => _simulator.Stop();

        private void OnLightChanged(object? sender, TrafficLightChangedEventArgs e) =>
            LightState = e.NewState == TrafficLightState.GreenForCars ? "Зеленый" : "Красный";

        private void OnEntityMoved(object? sender, EntityMovedEventArgs e)
        {
            _uiDispatcher.InvokeAsync(() =>
            {
                var vm = Entities.FirstOrDefault(x => x.Id.Equals(e.Entity.Id));
                if (vm == null)
                {
                    vm = new EntityViewModel 
                    { 
                        Id = e.Entity.Id, 
                        X = e.Entity.X, 
                        Y = e.Entity.Y, 
                        IsVisible = e.Entity.IsActive 
                    };
                    
                    // 🔥 Маппинг типов для XAML (включая раненого пешехода)
                    vm.Type = e.Entity switch
                    {
                        Car c when c.IsEmergency => "Car_Emergency",
                        Car c when c.IsTowed     => "Car_Towed",
                        Pedestrian p when p.IsInjured => "Pedestrian_Injured",
                        Car                      => "Car",
                        Pedestrian               => "Pedestrian",
                        _                        => "Unknown"
                    };
                    
                    Entities.Add(vm);
                }
                else
                {
                    vm.X = e.Entity.X;
                    vm.Y = e.Entity.Y;
                    vm.IsVisible = e.Entity.IsActive;
                }
            });
        }

        private void OnAccidentOccurred(object? sender, AccidentEventArgs e) =>
            _uiDispatcher.InvokeAsync(() => LightState = "⚠️ АВАРИЯ");

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        
        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (!Equals(field, value)) { field = value; OnPropertyChanged(name); }
        }

        public void Dispose() => _simulator.Dispose();
    }
}