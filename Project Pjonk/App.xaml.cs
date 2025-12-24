using Hardcodet.Wpf.TaskbarNotification;
using System.Configuration;
using System.Data;
using System.Net.Http;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace Project_Pjonk
{
    public partial class App : System.Windows.Application
    {
        private TaskbarIcon _trayIcon = new();

        PetWindow pet1 = new("Media/Sprites/Static.png", SystemParameters.FullPrimaryScreenWidth / 2, PetPresets.Kreatyna);
        PetWindow pet2 = new("Media/Sprites/BrownStatic.png", 300.0, PetPresets.Sterydzia);
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            bool createdNew;
            using (var mutex = new System.Threading.Mutex(true, "ProjectPjonkPet", out createdNew))
            {
                if (!createdNew) return;
            }
                InitializeTaskbarIcon();

            InitializePetWindows();

        }

        private void InitializePetWindows()
        {
            pet1.Show();
            pet2.Show();

        }

        private void InitializeTaskbarIcon()
        {
            _trayIcon.Icon = new System.Drawing.Icon("SpiderIcon.ico");
            _trayIcon.ToolTipText = "Pjonk";

            var menu = new System.Windows.Controls.ContextMenu();

            double[] scales = { 0.5, 1, 2, 3, 4, 5, 10 };
            foreach (var scale in scales)
            {
                var item = new System.Windows.Controls.MenuItem { Header = $"Scale x{scale}" };
                item.Click += (_, __) => SetPetScale(scale);
                menu.Items.Add(item);
            }

            menu.Items.Add(new System.Windows.Controls.Separator());

            var teleportPets = new System.Windows.Controls.MenuItem { Header = "Teleport Pets" };
            teleportPets.Click += (_, __) => TeleportAllPets();
            menu.Items.Add(teleportPets);

            var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
            exitItem.Click += (_, __) => Shutdown();
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenu = menu;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon.Dispose();
            base.OnExit(e);
            
        }
        private void TeleportAllPets()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                pet1?.TeleportPetToScreen();
                pet2?.TeleportPetToScreen();
            });
        }
        private void SetPetScale(double scale)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                pet1?.SetScale(scale);
                pet2?.SetScale(scale);
            });
        }



    }

}
