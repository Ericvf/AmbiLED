using Ambiled.Core;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Windows;

namespace Ambiled
{
    public partial class App : Application
    {
        private static MainVM viewModel = MainVM.LoadFile();

        public static void InitializeComposition(object instance)
        {
            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(MainWindow).Assembly));

            var container = new CompositionContainer(catalog);
            container.ComposeExportedValue<IViewModel>(viewModel);
            container.ComposeParts(instance);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            viewModel.Save();
        }
    }
}
