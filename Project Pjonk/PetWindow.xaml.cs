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

    enum PetState
    {
        Idle,
        Moving,
        Sleeping,
        Falling,
        Sliding
    }

    public partial class PetWindow : Window
    {
        // Physics
        private const double Gravity = 3000;
        private const double TerminalVelocity = 7000;
        private const double GroundSnapTolerance = 5;
        private const double HorizontalFriction = 0.999;

        private Vector velocity = new(0.0,0.0);

        // Test for momentum
        private readonly Queue<(Point pos, DateTime time)> mouseHistory = new();
        private const int MaxHistory = 5;

        //

        private readonly Image petImage = new();
        private bool isDragged = false;
        private Point clickOffset;

        // State-behaviour
        private bool isGrounded;
        private PetState currentState = PetState.Idle;
        private DateTime stateStartTime;
        private Random random = new();
        private double targetX = 10;

        private DateTime lastUpdate = DateTime.Now;
        private double speed = 300;
        private Point position;

        public PetWindow(string imagePath, double startX)
        {   
            // Temp hardcoded
            Width = 128;
            Height = 128;
            // 
            AllowsTransparency = true;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;

            petImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Relative));
            petImage.IsHitTestVisible = true;
            Content = petImage;

            position = new(startX, SystemParameters.WorkArea.Bottom - Height - 250);
            UpdateWindowPosition();

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
            this.PreviewKeyDown += PetKeyDown;

            currentState = PetState.Idle;
        }

        private void Update(object? sender, EventArgs e)
        {
            double dt = (DateTime.Now - lastUpdate).TotalSeconds;
            lastUpdate = DateTime.Now; 

            if (!isDragged)
            {
                ApplyGravity(dt);
                ApplyMovement(dt);
                GroundCollision();
                ScreenSidesBounce();

                if (isGrounded)
                {
                    PetBehaviour();
                }
            }
            UpdateWindowPosition();
        }
        private void UpdateWindowPosition()
        {
            Left = position.X;
            Top = position.Y;
        }
        private void TeleportPetToScreen()
        {
            position.X = SystemParameters.PrimaryScreenWidth / 2.0 - Width / 2.0;
            position.Y = SystemParameters.PrimaryScreenHeight / 2.0 - Height / 2.0;

            velocity = new Vector(0, 0);
            isGrounded = false;
            currentState = PetState.Falling;
            UpdateWindowPosition();
        }

        private void PetKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.R)
            {
                TeleportPetToScreen();
            }
        }
        private void PetMouseDown(object sender, MouseButtonEventArgs e)
        {
            isDragged = true;
            isGrounded = false;

            Point mouse_DIU = GetMousePosOnScreen();
            clickOffset = new(mouse_DIU.X - Left, mouse_DIU.Y - Top);

            velocity = new(0.0, 0.0);
            petImage.CaptureMouse();
        }
        private void PetMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!isDragged) return;

            Point mouse_DIU = GetMousePosOnScreen();

            position.X = mouse_DIU.X - clickOffset.X;
            position.Y = mouse_DIU.Y - clickOffset.Y;

            mouseHistory.Enqueue((mouse_DIU, DateTime.Now));
            if (mouseHistory.Count > MaxHistory)
            {
                mouseHistory.Dequeue();
            }

        }
        private void PetMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDragged) return;
            isDragged = false;
            isGrounded = false;
            petImage.ReleaseMouseCapture();

            if (mouseHistory.Count >= 2)
            {
                var first = mouseHistory.Peek();
                var last = mouseHistory.ToArray()[^1]; 
                double dt = (last.time - first.time).TotalSeconds;
                if (dt > 0)
                {
                    velocity = new Vector(
                        (last.pos.X - first.pos.X) / dt,
                        (last.pos.Y - first.pos.Y) / dt
                    );
                }
                else
                {
                    velocity = new Vector(0, 0);
                }
            }
            else
            {
                velocity = new Vector(0, 0);
            }
            currentState = PetState.Falling;

            mouseHistory.Clear();
        }
        private bool HasReachedTarget()
        {
            int tolerance = 5;
            return Math.Abs(Left - targetX) < tolerance;
        }
        private void MoveTowardsTarget()
        {
            double direction = Math.Sign(targetX - Left);
            velocity.X = direction * speed;
        }
        private void EnterSleepState()
        {
            currentState = PetState.Sleeping;
            petImage.Source = new BitmapImage(new Uri("Media/SleepingDoggo.png", UriKind.Relative));
            stateStartTime = DateTime.Now;
        }

        private void EnterIdleState()
        {
           currentState = PetState.Idle;
            petImage.Source = new BitmapImage(new Uri("Media/DoggoTest.png", UriKind.Relative));
            stateStartTime = DateTime.Now;
        }
        private void EnterMovingState()
        {
            currentState = PetState.Moving;
            petImage.Source = new BitmapImage(new Uri("Media/DoggoTest.png", UriKind.Relative));

            var screen = GetCurrentScreen();
            double maxX = screen.WorkingArea.Right / GetDpi().X - Width;

            targetX = random.NextDouble() * maxX;
            stateStartTime = DateTime.Now;
        }
        private void PetBehaviour()
        {
            DateTime now = DateTime.Now;

            switch (currentState)
            {
                case PetState.Idle:
                    velocity.X = 0;
                    if((now-stateStartTime).TotalSeconds > random.Next(2, 5))
                    {
                        EnterMovingState();
                    }
                    break;

                case PetState.Moving:
                    MoveTowardsTarget();

                    if (HasReachedTarget())
                    {
                        velocity.X = 0;
                        
                        if(random.NextDouble() < 0.2)
                        {
                            EnterSleepState();
                        }
                        else
                        {
                            EnterIdleState();
                        }

                    }
                    break;

                case PetState.Sleeping:
                    velocity.X = 0;
                    if((now -stateStartTime).TotalSeconds > random.Next(30, 48))
                    {
                        EnterMovingState();
                    }
                        
                    break;
                case PetState.Sliding:
                    {
                        if (Math.Abs(velocity.X) <= 3.0 || (now - stateStartTime).TotalSeconds > random.Next(6, 8))
                        {
                            EnterIdleState();
                        }
                    }
                    break;

                case PetState.Falling:

                    break;
                default:

                    break;
            }


        }

        // Physics
        private void ApplyGravity(double dt)
        {
            if (!isGrounded)
            {
                velocity.Y += Gravity * dt;

                if(velocity.Y > TerminalVelocity)
                {
                    velocity.Y = TerminalVelocity;
                }
            }
        }
        private void ApplyMovement(double dt)
        {
            position.X += velocity.X * dt;
            position.Y += velocity.Y * dt;
            if (!isGrounded)
            {
                velocity.X *= Math.Pow(HorizontalFriction, dt * 800);

            }
            else if(currentState != PetState.Moving)
            {
                velocity.X *= Math.Pow(HorizontalFriction, dt * 2400);
            }

        }
        private void GroundCollision()
        {
            var screen = GetCurrentScreen();
            double groundY = screen.WorkingArea.Bottom / GetDpi().Y - Height;

            if(position.Y >= groundY - GroundSnapTolerance)
            {
                position.Y = groundY;
                velocity.Y = 0;
                if(currentState == PetState.Falling)
                {
                    currentState = PetState.Sliding;
                    stateStartTime = DateTime.Now;
                }
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }
        }
        private void ScreenSidesBounce()
        {
            var screen = GetCurrentScreen();
            double dpiX = GetDpi().X;

            double minX = screen.WorkingArea.Left / dpiX;
            double maxX = screen.WorkingArea.Right / dpiX - Width;

            if (position.X < minX)
            {
                position.X = minX;
                velocity.X = Math.Abs(velocity.X); 
            }
            else if (position.X > maxX)
            {
                position.X = maxX;
                velocity.X = -Math.Abs(velocity.X); 
            }
        }

        // Helpers
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
            double dpiX = 1.0;
            double dpiY = 1.0;
            PresentationSource source = PresentationSource.FromVisual(this);
            if (source != null)
            {
                Matrix transform = source.CompositionTarget.TransformFromDevice;
                dpiX = transform.M11;
                dpiY = transform.M22;
            }
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

