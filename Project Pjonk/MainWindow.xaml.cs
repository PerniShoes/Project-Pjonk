using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace Project_Pjonk
{
    public partial class MainWindow : Window
    {
        private DateTime lastUpdate = DateTime.Now;
        private double speed = 100; 

        public MainWindow()
        {
            int width = 128;
            int height = 64;

            InitializeComponent();
            PetImage.Source = new BitmapImage(new Uri("DoggoTest.png", UriKind.Relative));
            Width = width;
            Height = height;

            Left = SystemParameters.PrimaryScreenWidth - width - 40;
            Top = SystemParameters.PrimaryScreenHeight - height - 50;

            CompositionTarget.Rendering += Update;
        }

        private void Update(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            double deltaSeconds = (now - lastUpdate).TotalSeconds;
            lastUpdate = now;

            //Left += speed * deltaSeconds;

            if (Left > SystemParameters.PrimaryScreenWidth)
                Left = -Width;
        }
    }
}