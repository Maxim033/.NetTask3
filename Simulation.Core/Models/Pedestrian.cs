namespace Simulation.Core.Models
{
    public class Pedestrian : SimulationEntity
    {
        public bool IsCrossing { get; set; }
        public double CrossingProgress { get; set; }
        public int Direction { get; set; } = 1;
        public double TargetX { get; set; }
        
        
        public bool IsInjured { get; set; }
        public double InjuryTime { get; set; } // время с момента ДТП
    }
}