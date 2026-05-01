using System.Windows;
using Simulation.UI.ViewModels;

namespace Simulation.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}