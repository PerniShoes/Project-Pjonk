using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;
using System.Windows.Media;

namespace Project_Pjonk
{
    internal class AnimationManager
    {

        private readonly Dictionary<PetState, Animation> animations = new();
        private Animation? currentAnimation;

        public void AddAnimation(PetState state, Animation animation)
        {
            animations[state] = animation;
        }

        public void Play(PetState state)
        {
            if (!animations.TryGetValue(state, out var anim))
                return;

            if (currentAnimation != anim)
            {
                currentAnimation = anim;
                anim.Reset(); 
            }

        }

        public void Update(double dt)
        {
            currentAnimation?.Update(dt);
        }

        public void SetAnimationSpeed(PetState state, double secondsPerFrame)
        {
            if (animations.TryGetValue(state,out var anim))
            {
                anim.SetFrameDuration(secondsPerFrame);
            }
        }

        public ImageSource? GetCurrentFrame()
        {
            return currentAnimation?.GetCurrentFrame();
        }


    }
}
