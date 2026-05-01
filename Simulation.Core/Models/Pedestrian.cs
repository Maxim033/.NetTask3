namespace Simulation.Core.Models
{
    public class Pedestrian : SimulationEntity
    {
        public bool IsCrossing { get; set; }
        public double CrossingProgress { get; set; }
        
        // 1 = слева направо, -1 = справа налево
        public int Direction { get; set; } = 1;
        public double TargetX { get; set; }
    }
}