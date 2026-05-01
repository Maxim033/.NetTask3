using System.Collections.ObjectModel;
using System.Threading.Tasks; //  Добавлено
using System.Windows;
using System.Windows.Input;
using Simulation.Core.Interfaces;
using Simulation.Core.Simulation;

namespace Simulation.UI.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<CrossingViewModel> Crossings { get; } = new();
        public ICommand AddCrossingCommand { get; }

        public MainViewModel()
        {
            AddCrossingCommand = new RelayCommand(_ => AddNewCrossing());
        }

        private void AddNewCrossing()
        {
            var config = new SimulationConfig
            {
                CanvasWidth = 800,
                CanvasHeight = 600,
                AccidentProbability = 0.015,
                LightCycleMs = 4000,
                SimulationTickMs = 50,
                PedestrianSpeed = 2.0,
                CarSpeed = 4.0
            };

            var emergencyService = new MockEmergencyService();
            var crossingVm = new CrossingViewModel(config, emergencyService);
            
            Crossings.Add(crossingVm);
            _ = crossingVm.StartAsync();
        }
    }

    // 🔑 Исправленный класс службы (соответствует интерфейсу IEmergencyService)
    public class MockEmergencyService : IEmergencyService
    {
        public async Task DispatchAsync(double targetX, double targetY)
        {
            await Task.Delay(500);
            System.Diagnostics.Debug.WriteLine($"🚑 Аварийная служба: ({targetX:F1}, {targetY:F1})");
        }
    }
}