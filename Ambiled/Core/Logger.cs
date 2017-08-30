using System.ComponentModel.Composition;
using System.Windows;

namespace Ambiled.Core
{
    public interface ILogger
    {
        void Add(string message);
    }

    [Export(typeof(ILogger))]
    public class Logger : ILogger
    {
        [Import]
        public IViewModel ViewModel { get; set; }

        public void Add(string message)
        {
            Application.Current.Dispatcher.Invoke(() => {
                ViewModel.Messages.Add(message);
                ViewModel.Message = message;
            });
        }
    }
}
