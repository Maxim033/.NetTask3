using System;

namespace Simulation.Core.Models
{
    public abstract class SimulationEntity
    {
        public Guid Id { get; } = Guid.NewGuid();
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
        public bool IsActive { get; set; } = true;
    }
}