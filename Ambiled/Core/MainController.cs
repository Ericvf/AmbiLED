using System.ComponentModel.Composition;

namespace Ambiled.Core
{
    public interface IMainController
    {
        IViewModel ViewModel { get; set; }

        void Load();

        void ToggleController();
    }

    [Export(typeof(IMainController))]
    public class MainController : IMainController
    {
        [Import]
        public IViewModel ViewModel { get; set; }

        [Import]
        public IControllerService ControllerService { get; set; }

        [Import]
        public IWindowsHandleReporter WindowsHandleReporter { get; set; }

        [Import]
        public IHttpServer WebServer { get; set; }

        public void Load()
        {
            ControllerService.RefreshDevices();

            if (ViewModel.IsConnectOnStart)
                ControllerService.Start();

            WebServer.Start();
        }

        public void ToggleController()
        {
            ControllerService.Toggle();
        }
    }
}
