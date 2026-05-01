using System.Threading.Tasks;

namespace Simulation.Core.Interfaces
{
    public interface IEmergencyService
    {
        Task DispatchAsync(double targetX, double targetY);
    }
}