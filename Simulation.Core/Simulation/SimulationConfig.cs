namespace Simulation.Core.Simulation
{
    public class SimulationConfig
    {
        public double CanvasWidth { get; init; }
        public double CanvasHeight { get; init; }
        public double AccidentProbability { get; init; } = 0.02;
        public int LightCycleMs { get; init; } = 3000;
        public int SimulationTickMs { get; init; } = 50;
        public double PedestrianSpeed { get; init; } = 2.0;
        public double CarSpeed { get; init; } = 4.0;
        public double CrossingX { get; init; } = 400; // X координата перехода
    }
}