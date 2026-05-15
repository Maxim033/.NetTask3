using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Simulation.Core.Interfaces;
using Simulation.Core.Models;

namespace Simulation.Core.Simulation
{
    public enum TrafficLightState { GreenForCars, RedForCars }

    public class TrafficLightChangedEventArgs : EventArgs
    {
        public TrafficLightState NewState { get; }
        public TrafficLightChangedEventArgs(TrafficLightState state) => NewState = state;
    }

    public class AccidentEventArgs : EventArgs
    {
        public double X { get; }
        public double Y { get; }
        public AccidentEventArgs(double x, double y) { X = x; Y = y; }
    }

    public class EntityMovedEventArgs : EventArgs
    {
        public SimulationEntity Entity { get; }
        public EntityMovedEventArgs(SimulationEntity entity) => Entity = entity;
    }

    public class CrossingSimulator : IDisposable
    {
        private readonly SimulationConfig _config;
        private readonly IEmergencyService _emergencyService;
        private readonly ConcurrentBag<SimulationEntity> _entities = new();
        private readonly Random _random = new();
        private CancellationTokenSource? _cts;
        private TrafficLightState _lightState = TrafficLightState.RedForCars;
        private DateTime _lightChangeTime = DateTime.UtcNow;

        private double CrosswalkCenterY => _config.CanvasHeight / 2;
        private double CrosswalkHalfH => 55;
        private double CrosswalkTopY => CrosswalkCenterY - CrosswalkHalfH;
        private double StopLineY => CrosswalkTopY - 80;
        
        private const double SafeCarDistance = 70;
        private const double SmoothFactor = 0.12;

        public event EventHandler<TrafficLightChangedEventArgs>? LightChanged;
        public event EventHandler<AccidentEventArgs>? AccidentOccurred;
        public event EventHandler<EntityMovedEventArgs>? EntityMoved;

        public CrossingSimulator(SimulationConfig config, IEmergencyService emergencyService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _emergencyService = emergencyService ?? throw new ArgumentNullException(nameof(emergencyService));
        }

        public void AddEntity(SimulationEntity entity) => _entities.Add(entity);

        public async Task StartAsync(CancellationToken stoppingToken = default)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            await Task.Run(() => SimulationLoop(_cts.Token), _cts.Token);
        }

        public void Stop() => _cts?.Cancel();

        private async Task SimulationLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                UpdateTrafficLight();
                MoveEntities();
                CheckAccidents();

                foreach (var entity in _entities.Where(e => e.IsActive))
                    EntityMoved?.Invoke(this, new EntityMovedEventArgs(entity));

                await Task.Delay(_config.SimulationTickMs, ct);
            }
        }

        private void UpdateTrafficLight()
        {
            if ((DateTime.UtcNow - _lightChangeTime).TotalMilliseconds < _config.LightCycleMs) return;

            _lightState = _lightState == TrafficLightState.GreenForCars ? TrafficLightState.RedForCars : TrafficLightState.GreenForCars;
            _lightChangeTime = DateTime.UtcNow;
            LightChanged?.Invoke(this, new TrafficLightChangedEventArgs(_lightState));

            bool carsCanGo = _lightState == TrafficLightState.GreenForCars;

            foreach (var c in _entities.OfType<Car>().Where(c => !c.IsEmergency && !c.HasCrashed))
            {
                c.IsStopped = c.IsReckless ? false : (carsCanGo ? false : (c.Y < StopLineY));
            }

            foreach (var p in _entities.OfType<Pedestrian>())
                p.IsCrossing = !carsCanGo;
        }

        private void MoveEntities()
        {
            var cars = _entities.OfType<Car>().Where(c => c.IsActive && !c.HasCrashed).OrderBy(c => c.Y).ToList();
            
            for (int i = 0; i < cars.Count; i++)
            {
                var car = cars[i];
                if (car.IsEmergency || car.HasCrashed) continue;

                double desiredSpeed = _config.CarSpeed;

                if (!car.IsReckless && _lightState == TrafficLightState.RedForCars && car.Y < StopLineY)
                {
                    double distToStop = StopLineY - car.Y;
                    if (distToStop < 120)
                        desiredSpeed = _config.CarSpeed * (distToStop / 120);
                    if (distToStop <= 5) desiredSpeed = 0;
                }

                if (i < cars.Count - 1 && !car.IsReckless)
                {
                    double gap = cars[i + 1].Y - car.Y;
                    if (gap < SafeCarDistance + 30)
                        desiredSpeed = Math.Min(desiredSpeed, _config.CarSpeed * (gap / (SafeCarDistance + 30)));
                }

                
                car.Speed += (desiredSpeed - car.Speed) * SmoothFactor;
                if (car.Speed > 0.05) car.Y += car.Speed;

                // Респавн
                if (car.Y > _config.CanvasHeight + 30)
                {
                    car.Y = -100 - _random.Next(50, 150);
                    car.Speed = _config.CarSpeed;
                    car.HasCrashed = false;
                    car.IsTowed = false;
                    car.TowTarget = null;
                    // 15% шанс стать лихачом при появлении
                    car.IsReckless = _random.NextDouble() < 0.15;
                }
            }

            double leftSidewalk = _config.CanvasWidth / 2 - 100;
            double rightSidewalk = _config.CanvasWidth / 2 + 100;

            foreach (var p in _entities.OfType<Pedestrian>().Where(p => p.IsActive))
            {
                if (p.IsCrossing)
                {
                    p.X += p.Speed * p.Direction;
                    bool reached = (p.Direction == 1 && p.X >= rightSidewalk) || (p.Direction == -1 && p.X <= leftSidewalk);
                    if (reached)
                    {
                        p.IsCrossing = false;
                        p.Direction *= -1;
                    }
                }
            }

            foreach (var truck in _entities.OfType<Car>().Where(c => c.IsEmergency && c.IsActive))
            {
                truck.Y += truck.Speed;
                if (truck.Y > _config.CanvasHeight + 50)
                {
                    truck.IsActive = false;
                    foreach (var towed in _entities.OfType<Car>().Where(t => t.TowTarget == truck)) towed.IsActive = false;
                }
            }

            foreach (var towed in _entities.OfType<Car>().Where(c => c.IsTowed && c.IsActive))
            {
                if (towed.TowTarget is Car target && target.IsActive)
                    towed.Y += (target.Y - towed.TowOffsetY - towed.Y) * 0.15;
            }
        }

        
        private void CheckAccidents()
        {
            var cars = _entities.OfType<Car>()
                                 .Where(c => c.IsActive && !c.HasCrashed && !c.IsEmergency && !c.IsTowed)
                                 .OrderBy(c => c.Y)
                                 .ToList();

            for (int i = 0; i < cars.Count - 1; i++)
            {
                var rearCar = cars[i];
                var frontCar = cars[i + 1];

                
                double distY = frontCar.Y - rearCar.Y;
                double distX = Math.Abs(rearCar.X - frontCar.X);

                if (distX < 30 && distY < 40 && distY > -10)
                {
                    TriggerCarAccident(rearCar, frontCar);
                    return;
                }
            }
        }

        private void TriggerCarAccident(Car rearCar, Car frontCar)
        {
            rearCar.HasCrashed = true;
            rearCar.IsStopped = true;
            frontCar.IsStopped = true; 

            AccidentOccurred?.Invoke(this, new AccidentEventArgs(rearCar.X, rearCar.Y));
            Debug.WriteLine($"ДТП! Столкновение автомобилей. Координаты: ({rearCar.X:F0}, {rearCar.Y:F0})");

            
            var towTruck = new Car
            {
                X = rearCar.X,
                Y = rearCar.Y - 90,
                Speed = 1.8,
                IsActive = true,
                IsEmergency = true,
                IsTowing = true
            };
            _entities.Add(towTruck);

            
            rearCar.IsTowed = true;
            rearCar.TowTarget = towTruck;
        }

        public void Dispose() => _cts?.Dispose();
    }
}