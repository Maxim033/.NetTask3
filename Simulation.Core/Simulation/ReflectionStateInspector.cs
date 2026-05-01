using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Simulation.Core.Models;

namespace Simulation.Core.Simulation
{
    
    public static class ReflectionStateInspector
    {
        public static string GenerateStateReport(IEnumerable<SimulationEntity> entities)
        {
            var report = new StringBuilder();
            foreach (var entity in entities)
            {
                // Получаем все публичные свойства через рефлексию
                var properties = entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                report.AppendLine($"--- {entity.GetType().Name} (ID: {entity.Id}) ---");
                
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(entity);
                    report.AppendLine($"  {prop.Name}: {value ?? "null"}");
                }
            }
            return report.ToString();
        }
    }
}