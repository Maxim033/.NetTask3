using System;
using System.IO;
using System.Windows;

namespace Simulation.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Логируем старт
                File.AppendAllText("debug.log", $"[{DateTime.Now:HH:mm:ss}] 🚀 Запуск приложения...\n");

                // Перехватываем ошибки UI-потока
                DispatcherUnhandledException += (s, args) =>
                {
                    File.AppendAllText("debug.log", $"❌ UI Error: {args.Exception}\n");
                    args.Handled = true; // Не даём приложению упасть
                };

                base.OnStartup(e);
                File.AppendAllText("debug.log", "✅ OnStartup завершён. Окно должно открыться.\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"💥 CRASH: {ex}\n");
                MessageBox.Show($"Критическая ошибка запуска:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
    }
}