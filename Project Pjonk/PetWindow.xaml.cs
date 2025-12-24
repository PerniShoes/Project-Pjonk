using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Project_Pjonk
{

    enum PetState
    {
        Idle,
        Walking,
        Sleeping,
        Falling,
        Jumping,
        Sliding,
        Static
    }
    public enum RandomBias
    {
        None,
        Low,
        Middle,
        High
    }

    public enum PetPresets
    {
        Kreatyna,
        Sterydzia,

    }

    public partial class PetWindow : Window
    {
        private AnimationManager animationManager = new();
        // Physics
        private const double Gravity = 3000;
        private const double TerminalVelocity = 7000;
        private const double GroundSnapTolerance = 5;
        private const double HorizontalFriction = 0.999;
        private double JumpVelocityMinY = -200; 
        private double JumpVelocityMaxY = -4000; 
        private double turboJumpVelocityY = -20000;
        private double JumpVelocityMinX = 300; 
        private double JumpVelocityMaxX = 2000;
        private const double MaxDt = 1.0 / 30.0; // 30 FPS
        private const double MinDt = 0.0001;     


        private Vector velocity = new(0.0,0.0);

        private readonly Queue<(Point pos, DateTime time)> mouseHistory = new();
        private const int MaxHistory = 5;

        //

        private readonly Image petImage = new();
        private bool isDragged = false;
        private Point clickOffset;

        // State-behaviour
        private bool isGrounded;
        private PetState currentState = PetState.Idle;
        private PetState previousState = PetState.Idle;
        private DateTime stateStartTime;
        private Random random = new();
        private double targetX = 10;

        private DateTime lastUpdate = DateTime.Now;

        private double minSpeed = 250;
        private double maxSpeed = 700;
        private double currentSpeed = 0;
        private Point position;
        private double BaseSize = 32;

        double jumpProbability = 0.35;
        double sleepProbability = 0.05;
        bool preferHighSpeeds = true;
        int maxSleepTime = 45;
        int maxIdleTime = 8;


        private void PreSetWhite()
        {
            animationManager.AddAnimation(PetState.Idle, new Animation("Media/Sprites/IdleSheet.png", 9, 0.2));
            animationManager.AddAnimation(PetState.Walking, new Animation("Media/Sprites/WalkSheet.png", 8, 0.2));
            animationManager.AddAnimation(PetState.Static, new Animation("Media/Sprites/Static.png", 1, 0.2));
            animationManager.AddAnimation(PetState.Sleeping, new Animation("Media/Sprites/SleepSheet.png", 21, 0.15));
            animationManager.AddAnimation(PetState.Jumping, new Animation("Media/Sprites/Jump.png", 1, 0.2));

            Width = 32;
            Height = 32;
            // Uses default values 
        }

        private void PreSetBrown()
        {
            animationManager.AddAnimation(PetState.Idle, new Animation("Media/Sprites/BrownIdleSheet.png", 9, 0.2));
            animationManager.AddAnimation(PetState.Walking, new Animation("Media/Sprites/BrownWalkSheet.png", 8, 0.2));
            animationManager.AddAnimation(PetState.Static, new Animation("Media/Sprites/BrownStatic.png", 1, 0.2));
            animationManager.AddAnimation(PetState.Sleeping, new Animation("Media/Sprites/BrownSleepSheet.png", 21, 0.15));
            animationManager.AddAnimation(PetState.Jumping, new Animation("Media/Sprites/BrownJump.png", 1, 0.2));

            Width = 64;
            Height = 64;
            BaseSize = 64;

            // Overrides default values 
            maxSleepTime = 80;
            maxIdleTime = 14;

            preferHighSpeeds = false;
            jumpProbability = 0.15;
            sleepProbability = 0.15;
            minSpeed = 100;
            maxSpeed = 400;

            JumpVelocityMinY = -200;
            JumpVelocityMaxY = -2000;
            turboJumpVelocityY = -4000;

            JumpVelocityMinX = 100;
            JumpVelocityMaxX = 800;

        }
        public PetWindow(string imagePath, double startX, PetPresets preset)
        {

            switch (preset)
            {
                case PetPresets.Kreatyna:
                    PreSetWhite();
                    break;

                case PetPresets.Sterydzia:
                    PreSetBrown();
                    break;

                default:
                    PreSetWhite();
                    break;

            }

    
            AllowsTransparency = true;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;

            petImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Relative));
            petImage.IsHitTestVisible = true;
            Content = petImage;
            petImage.RenderTransformOrigin = new Point(0.5, 0.5);
            petImage.RenderTransform = new ScaleTransform(1, 1);

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

            EnterSleepState();
        }
        public void SetScale(double scale)
        {
            Width = BaseSize * scale;
            Height = BaseSize * scale;
        }
        private void Update(object? sender, EventArgs e)
        {
            double dt = (DateTime.Now - lastUpdate).TotalSeconds;
            lastUpdate = DateTime.Now;

            if (dt > MaxDt) dt = MaxDt;
            if (dt < MinDt) dt = MinDt;

            PetState newState = currentState;
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
            else
            {
                velocity = new(0.0, 0.0);
            }
            if(newState != currentState)
            {
                previousState = newState;
            }

            // Animation
            switch (currentState)
            {
                case PetState.Sliding:
                    animationManager.Play(PetState.Static);
                    break;
                case PetState.Falling:
                    animationManager.Play(PetState.Static);
                    break;
                default:
                    animationManager.Play(currentState);
                    break;

            }
            animationManager.Update(dt);
            var frame = animationManager.GetCurrentFrame();
            if (frame != null)
            {
                petImage.Source = frame;
            }
            //

            UpdateWindowPosition();
        }
        private double RandomRange(double min, double max, RandomBias bias = RandomBias.None)
        {
            double t = random.NextDouble();

            switch (bias)
            {
                case RandomBias.Low:
                    t = (t*t);              
                    break;

                case RandomBias.High:
                    t = 1.0 - (1.0 - t) * (1.0 - t); 
                    break;
                case RandomBias.Middle:
                    t = 0.5 + (t - 0.5) * 0.5;
                    break;

                case RandomBias.None:
                default:
                    break;
            }

            return min + t * (max - min);
        }

        private void UpdateWindowPosition()
        {
            Left = position.X;
            Top = position.Y;
        }
        public void TeleportPetToScreen()
        {
            position.X = SystemParameters.PrimaryScreenWidth / 2.0 - Width / 2.0;
            position.Y = SystemParameters.PrimaryScreenHeight / 2.0 - Height / 2.0;
            
            velocity = new Vector(0, 0);
            isGrounded = false;
            isDragged = false;
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
            currentState = PetState.Static;
            petImage.CaptureMouse();
        }
        private void PetMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!isDragged) return;

            Point mouse_DIU = GetMousePosOnScreen();

            position.X = mouse_DIU.X - clickOffset.X;
            position.Y = mouse_DIU.Y - clickOffset.Y;
            velocity = new(0.0, 0.0);
            currentState = PetState.Static;

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
            velocity.X = direction * currentSpeed;
        }
        private void EnterSleepState()
        {
            currentState = PetState.Sleeping;
            stateStartTime = DateTime.Now;
        }
        private void EnterJumpState()
        {
            if (!isGrounded) return;

            isGrounded = false;
            currentState = PetState.Jumping;

            velocity.Y = RandomRange(JumpVelocityMinY, JumpVelocityMaxY, RandomBias.Low);
            double jumpX = RandomRange(JumpVelocityMinX, JumpVelocityMaxX, RandomBias.Middle);

            if (velocity.Y > JumpVelocityMinY)
            {
                velocity.Y = JumpVelocityMinY;
            }
            if (random.NextDouble() < 0.08)
            {
                velocity.Y = turboJumpVelocityY;
            }
            double direction;
            if (Math.Abs(velocity.X) > 1)
            {
                direction = Math.Sign(velocity.X);
            }
            else
            {
                direction = random.Next(0, 2) == 0 ? -1 : 1;
            }

            velocity.X = jumpX * direction;
            UpdateSpriteDirection();
            stateStartTime = DateTime.Now;
        }
        private void EnterIdleState()
        {
            currentState = PetState.Idle;
            stateStartTime = DateTime.Now;
        }
        private void EnterWalkingState()
        {
            currentState = PetState.Walking;
            var screen = GetCurrentScreen();
            var dpi = GetDpi();

            double minX = screen.WorkingArea.Left / dpi.X;
            double maxX = screen.WorkingArea.Right / dpi.X - Width;

            if (preferHighSpeeds)
            {
                targetX = RandomRange(minX, maxX, RandomBias.High);
            }
            else
            {
                targetX = RandomRange(minX, maxX, RandomBias.Low);
            }
            SetCurrentSpeed();
            stateStartTime = DateTime.Now;
            double maxAnimSpeed = 0.03;
            double minAnimSpeed = 0.10; 
            double normalized = Math.Clamp(currentSpeed / maxSpeed, 0, 1);
            double frameDuration = minAnimSpeed - normalized * (minAnimSpeed - maxAnimSpeed);

            animationManager.SetAnimationSpeed(PetState.Walking, frameDuration);
            UpdateSpriteDirection();
        }
        private void SetCurrentSpeed()
        {
            bool biasedFaster = true;

            if (biasedFaster)
            {
                double t = random.NextDouble();
                t = 1.0 - Math.Pow(1.0 - t, 2.0);
                currentSpeed = minSpeed + t * (maxSpeed - minSpeed);
            }
            else
            // No bias
            {
                currentSpeed = random.NextDouble() * maxSpeed;
            }

            if(currentSpeed < minSpeed)
            {
                currentSpeed = minSpeed;
            }
        }
        private void PetBehaviour()
        {
            DateTime now = DateTime.Now;

            switch (currentState)
            {
                case PetState.Idle:
                    velocity.X = 0;
                    if((now-stateStartTime).TotalSeconds > random.Next(2, maxIdleTime))
                    {
                        if (random.NextDouble() < jumpProbability)
                        {
                            EnterJumpState();
                        }
                        else
                        {
                            EnterWalkingState();
                        }
                    }
                    break;

                case PetState.Walking:
                    MoveTowardsTarget();

                    if (HasReachedTarget())
                    {
                        velocity.X = 0;
                        if(random.NextDouble() < sleepProbability)
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
                    if((now - stateStartTime).TotalSeconds > random.Next(20, maxSleepTime))
                    {
                        EnterWalkingState();
                    }
                        
                    break;
                case PetState.Sliding:
                    {
                        if (Math.Abs(velocity.X) <= 3.0 || (now - stateStartTime).TotalSeconds > random.Next(5, 8))
                        {
                            EnterIdleState();
                        }
                    }
                    break;
                default:

                    break;
            }


        }
        private void UpdateSpriteDirection()
        {
            if (currentState == PetState.Jumping)
            {
                if(velocity.X > 1)
                {
                    ((ScaleTransform)petImage.RenderTransform).ScaleX = 1;
                }
                else
                {
                    ((ScaleTransform)petImage.RenderTransform).ScaleX = -1;
                }
            }
            else
            {
                if (Math.Sign(targetX - Left) < 0)
                {
                    ((ScaleTransform)petImage.RenderTransform).ScaleX = -1;
                }
                else
                {
                    ((ScaleTransform)petImage.RenderTransform).ScaleX = 1;
                }
            }
        }

        // Physics
        private void ApplyGravity(double dt)
        {
            DateTime now = DateTime.Now;
            if (!isGrounded)
            {
                if (currentState == PetState.Jumping)
                {
                    velocity.Y += Gravity * dt;
                }
                else if(currentState != PetState.Jumping)
                {
                    velocity.Y += Gravity * dt;
                }

                if (velocity.Y > 0 &&
                    (currentState == PetState.Jumping || currentState == PetState.Static))
                {
                    currentState = PetState.Falling;
                }

                if (velocity.Y > TerminalVelocity)
                {
                    velocity.Y = TerminalVelocity;
                }
            }
        }
        private void ApplyMovement(double dt)
        {
            if (isDragged) return;
            position.X += velocity.X * dt;
            position.Y += velocity.Y * dt;
            
            if (!isGrounded)
            {
                velocity.X *= Math.Pow(HorizontalFriction, dt * 800);

            }
            else if(currentState != PetState.Walking)
            {
                velocity.X *= Math.Pow(HorizontalFriction, dt * 2400);
            }

        }

        private void GroundCollision()
        {
            var screen = GetCurrentScreen();
            double groundY = screen.WorkingArea.Bottom / GetDpi().Y - Height;

            if (currentState == PetState.Jumping) return;
            if(position.Y >= groundY - GroundSnapTolerance)
            {
                position.Y = groundY;
                if(currentState == PetState.Falling)
                {
                    velocity.Y = 0;
                    if (previousState == PetState.Jumping)
                    {
                        currentState = PetState.Idle;
                    }
                    else
                    {
                        currentState = PetState.Sliding;
                    }
                    isGrounded = true;
                    stateStartTime = DateTime.Now;
                }
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
            GetCursorPos(out POINT p); 

            PresentationSource source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget == null)
                return new Point(p.X, p.Y);

            return source.CompositionTarget.TransformFromDevice
                .Transform(new Point(p.X, p.Y));
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


        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
        }


    }

}

