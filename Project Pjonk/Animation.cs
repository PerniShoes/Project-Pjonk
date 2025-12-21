using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace Project_Pjonk
{
    internal class Animation
    {
        public BitmapImage SpriteSheet { get; private set; } = new BitmapImage();
        public int FrameCount { get; private set; }
        public int CurrentFrame { get; private set; } = 0;
        public double FrameDuration { get; private set; }
        private double elapsedTime = 0;

        public int FrameWidth => SpriteSheet.PixelWidth / FrameCount;
        public int FrameHeight => SpriteSheet.PixelHeight;
        public Animation(string path, int frameCount, double frameDuration)
        {
            SpriteSheet.BeginInit();
            SpriteSheet.UriSource = new Uri(path, UriKind.Relative);
            SpriteSheet.EndInit();

            FrameCount = frameCount;
            FrameDuration = frameDuration;
            CurrentFrame = 0;
            elapsedTime = 0;
        }

        public void Update(double dt)
        {
            elapsedTime += dt;
            if (elapsedTime >= FrameDuration)
            {
                CurrentFrame = (CurrentFrame + 1) % FrameCount;
                elapsedTime = 0;
            }
        }

        public CroppedBitmap GetCurrentFrame()
        {
            return new(
                SpriteSheet,
                new System.Windows.Int32Rect(CurrentFrame * FrameWidth, 0, FrameWidth, FrameHeight)
                );
        }

        public void SetFrameDuration(double newDuration)
        {
            FrameDuration = newDuration;
        }
        public void Reset()
        {
            elapsedTime = 0;
            CurrentFrame = 0;
        }
    }
}
