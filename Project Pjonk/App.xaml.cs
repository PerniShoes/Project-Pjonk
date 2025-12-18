using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;

namespace Project_Pjonk
{
    public partial class App : Application
    {
        private TaskbarIcon _trayIcon = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            InitializeTaskbarIcon();

            InitializePetWindows();

        }

        private void InitializePetWindows()
        {
            PetWindow pet1 = new("Media/DoggoTest.png", SystemParameters.FullPrimaryScreenWidth - 150.0);
            //PetWindow pet2 = new("Media/PuppyTest.png", 100.0);
            pet1.Show();
            //pet2.Show();
        }

        private void InitializeTaskbarIcon()
        {
            _trayIcon.Icon = new System.Drawing.Icon("SpiderIcon.ico");
            _trayIcon.ToolTipText = "Pjonk";

            var menu = new System.Windows.Controls.ContextMenu();
            var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
            exitItem.Click += (s, ev) => Shutdown();
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenu = menu;
        }
        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon.Dispose();
            base.OnExit(e);
        }



    }
   
}
