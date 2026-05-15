namespace Simulation.Core.Models
{
    public class Car : SimulationEntity
    {
        public int Lane { get; set; }
        public bool IsEmergency { get; set; }
        public bool IsStopped { get; set; }
        public int Direction { get; set; } = 1;
        
        
        public bool IsReckless { get; set; } 
        
        
        public bool HasCrashed { get; set; }
        public bool IsTowed { get; set; }
        public bool IsTowing { get; set; }
        public SimulationEntity? TowTarget { get; set; }
        public double TowOffsetY { get; set; } = 50;
    }
}