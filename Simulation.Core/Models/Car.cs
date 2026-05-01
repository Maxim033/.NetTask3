namespace Simulation.Core.Models
{
    public class Car : SimulationEntity
    {
        public int Lane { get; set; }
        public bool IsEmergency { get; set; }
        public bool IsStopped { get; set; }
        public int Direction { get; set; } = 1;
        
        // Управление ДТП и эвакуацией
        public bool HasCrashed { get; set; }
        public bool IsTowed { get; set; }
        public bool IsTowing { get; set; }          
        public SimulationEntity? TowTarget { get; set; }
        public double TowOffsetY { get; set; } = 50;
    }
}