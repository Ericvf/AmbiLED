using Ambiled.Core;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;

namespace Ambiled
{
    public partial class MainWindow : Window
    {
        [Import]
        public IMainController MainController { get; set; }

        [Import]
        public ICaptureService CaptureService { get; set; }

        [Import]
        public IControllerService ControllerService { get; set; }

        [Import]
        public IViewModel ViewModel { get; set; }

        public MainWindow()
        {
            App.InitializeComposition(this);

            InitializeComponent();

            RenderOptions.SetBitmapScalingMode(this.previewWindow, BitmapScalingMode.NearestNeighbor);

            DataContext = ViewModel;

            MainController.Load();

            Closing += (s,e) => CaptureService.Close();
        }

        #region Button handlers (TODO: Move to ViewModel using ICommand)
      
        private void btnToggleController(object sender, RoutedEventArgs e)
        {
            MainController.ToggleController();
        }

        private void btnUpdateSetup(object sender, RoutedEventArgs e)
        {
            CaptureService.Reset();
        }

        private void btnChangeMonitor_Click(object sender, RoutedEventArgs e)
        {
            CaptureService.Reset();
        }

        private void btnRefreshCom(object sender, RoutedEventArgs e)
        {
            ControllerService.RefreshDevices();
        }

        #endregion 
    }
}
