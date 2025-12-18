using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;

namespace Project_Pjonk
{
    public partial class PetWindow : Window
    {
        private bool isDragged = false;
        private Point clickOffset;

        private readonly Image petImage = new();
        private DateTime lastUpdate = DateTime.Now;
        private double speed = 100;

        public PetWindow(string imagePath, double startX)
        {   
            // Temp hardcoded
            Width = 128;
            Height = 64;

            AllowsTransparency = true;
            WindowStyle = WindowStyle.None;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;

            petImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Relative));
            petImage.IsHitTestVisible = true;
            Content = petImage;

            Left = startX;
            Top = SystemParameters.WorkArea.Bottom - Height-250;

            CompositionTarget.Rendering += Update;

            SystemParameters.StaticPropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SystemParameters.WorkArea))
                {
                    Top = SystemParameters.WorkArea.Bottom - Height;
                }
            };

            petImage.MouseLeftButtonDown += PetMouseDown;
            petImage.MouseLeftButtonUp += PetMouseUp;
            petImage.MouseMove += PetMouseMove;

        }

        private void Update(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            //double deltaSeconds = (now - lastUpdate).TotalSeconds;
            lastUpdate = now;
            //Left += speed * deltaSeconds;

            if (Left < 0 || Left + Width > SystemParameters.PrimaryScreenWidth)
                speed = -speed;

        }


        private void PetMouseDown(object sender, MouseButtonEventArgs e)
        {
            isDragged = true;

            Point mouse_DIU = GetMousePosOnScreen();

            clickOffset.X = mouse_DIU.X - Left;
            clickOffset.Y = mouse_DIU.Y - Top;

            petImage.CaptureMouse();
           
        }

        private void PetMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!isDragged) return;

            Point mouse_DIU = GetMousePosOnScreen();

            Left = mouse_DIU.X - clickOffset.X;
            Top = mouse_DIU.Y - clickOffset.Y;

        }

        private void PetMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDragged) return;
            isDragged = false;
            petImage.ReleaseMouseCapture();

            Screen currentScreen = GetCurrentScreen();
            // Just snaps to bottom
            Top = currentScreen.WorkingArea.Bottom / GetDpi().Y - Height;
        }


        private Point GetMousePosOnScreen()
        {
            Point mousePos = Mouse.GetPosition(null);
            Point screenPos = petImage.PointToScreen(mousePos);

            PresentationSource source = PresentationSource.FromVisual(this);
            Matrix transform = source.CompositionTarget.TransformFromDevice;

            return new(screenPos.X * transform.M11, screenPos.Y * transform.M22);
        }

        private Screen GetCurrentScreen()
        {
            PresentationSource source = PresentationSource.FromVisual(this);
            Matrix transform = source.CompositionTarget.TransformFromDevice;
            double dpiX = source.CompositionTarget.TransformToDevice.M11;
            double dpiY = source.CompositionTarget.TransformToDevice.M22;

            var windowRect = new System.Drawing.Rectangle(
                (int)(Left * dpiX),
                (int)(Top * dpiY),
                (int)(Width * dpiX),
                (int)(Height * dpiY)
            );

           return Screen.FromRectangle(windowRect);
        }

        private Point GetDpi()
        {
            var source = PresentationSource.FromVisual(this);
            if (source == null) return new Point(1.0, 1.0);

            return new Point(
                source.CompositionTarget.TransformToDevice.M11,
                source.CompositionTarget.TransformToDevice.M22
            );
        }




    }

}

