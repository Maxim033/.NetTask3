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
        private double CrosswalkBottomY => CrosswalkCenterY + CrosswalkHalfH;
        private double StopLineY => CrosswalkTopY - 45; // Линия СТОП перед переходом
        private const double SafeCarDistance = 70;      // Мин. дистанция между авто

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
                if (carsCanGo)
                {
                    c.IsStopped = false; // Зелёный для авто
                }
                else
                {
                    // Красный для авто: останавливаем ТОЛЬКО если ещё не пересекли линию СТОП
                    if (c.Y < StopLineY) c.IsStopped = true;
                    // Если уже на переходе или проехали его → пусть доезжают (не блокируем посреди дороги)
                }
            }

            
            foreach (var p in _entities.OfType<Pedestrian>())
            {
                if (carsCanGo)
                {
                    p.IsCrossing = false; // Красный для пешеходов
                }
                else
                {
                    p.IsCrossing = true;  // Зелёный для пешеходов
                }
            }
        }

        private void MoveEntities()
        {
            
            var cars = _entities.OfType<Car>()
                                 .Where(c => c.IsActive && !c.HasCrashed)
                                 .OrderBy(c => c.Y)
                                 .ToList();

            for (int i = 0; i < cars.Count; i++)
            {
                var car = cars[i];
                if (car.IsEmergency || car.IsStopped) continue;

                bool canMove = true;

                
                if (i < cars.Count - 1)
                {
                    var nextCar = cars[i + 1];
                    if (nextCar.Y - car.Y < SafeCarDistance)
                        canMove = false;
                }

                
                if (_lightState == TrafficLightState.RedForCars && car.Y < StopLineY)
                    canMove = false;

                if (canMove)
                {
                    car.Y += car.Speed;
                }

                // Респавн
                if (car.Y > _config.CanvasHeight + 30)
                {
                    car.Y = -100 - _random.Next(50, 150);
                    car.HasCrashed = false;
                    car.IsTowed = false;
                    car.TowTarget = null;
                }
            }

            
            foreach (var p in _entities.OfType<Pedestrian>().Where(p => p.IsActive))
            {
                if (p.IsCrossing)
                {
                    p.X += p.Speed * p.Direction;
                    bool reached = (p.Direction == 1 && p.X >= p.TargetX) ||
                                   (p.Direction == -1 && p.X <= p.TargetX);
                    
                    if (reached)
                    {
                        p.IsCrossing = false;
                        p.Direction *= -1;
                        p.TargetX = p.Direction == 1 ? 50 : _config.CanvasWidth - 50;
                    }
                }
            }

            
            var towTrucks = _entities.OfType<Car>().Where(c => c.IsEmergency && c.IsActive).ToList();
            foreach (var truck in towTrucks)
            {
                truck.Y += truck.Speed;
                if (truck.Y > _config.CanvasHeight + 50)
                {
                    truck.IsActive = false;
                    // Деактивируем всех прицепленных
                    foreach (var towed in _entities.OfType<Car>().Where(t => t.TowTarget == truck))
                        towed.IsActive = false;
                }
            }

            var towedCars = _entities.OfType<Car>().Where(c => c.IsTowed && c.IsActive).ToList();
            foreach (var towed in towedCars)
            {
                if (towed.TowTarget is Car target && target.IsActive)
                {
                    double targetY = target.Y - towed.TowOffsetY;
                    towed.Y += (targetY - towed.Y) * 0.15; // Плавное следование
                }
            }
        }

        private void CheckAccidents()
        {
            // Шанс проверки (чтобы не спамить)
            if (_random.NextDouble() > 0.08) return;

            var peds = _entities.OfType<Pedestrian>().Where(p => p.IsActive && p.IsCrossing).ToList();
            var cars = _entities.OfType<Car>().Where(c => c.IsActive && !c.IsEmergency && !c.HasCrashed).ToList();

            foreach (var p in peds)
            {
                foreach (var c in cars)
                {
                    // Машина должна быть в зоне перехода
                    bool carInZone = c.Y > CrosswalkTopY && c.Y < CrosswalkBottomY;
                    // Пешеход должен быть на дороге
                    bool pedOnRoad = p.X > (_config.CanvasWidth / 2 - 50) && p.X < (_config.CanvasWidth / 2 + 50);
                    // Физическое пересечение
                    bool collision = Math.Abs(c.X - p.X) < 25 && Math.Abs(c.Y - p.Y) < 25;

                    if (carInZone && pedOnRoad && collision)
                    {
                        TriggerAccident(c, p);
                        return;
                    }
                }
            }
        }

        private void TriggerAccident(Car car, Pedestrian pedestrian)
        {
            car.HasCrashed = true;
            car.IsStopped = true;
            pedestrian.IsActive = false;

            AccidentOccurred?.Invoke(this, new AccidentEventArgs(car.X, car.Y));
            Debug.WriteLine($"ДТП! Авто не затормозило. ({car.X:F0}, {car.Y:F0})");

            
            var towTruck = new Car
            {
                X = car.X,
                Y = car.Y - 80,
                Speed = 1.8,
                IsActive = true,
                IsEmergency = true,
                IsTowing = true
            };
            _entities.Add(towTruck);

            
            car.IsTowed = true;
            car.TowTarget = towTruck;
        }

        public void Dispose() => _cts?.Dispose();
    }
}