using JP_Dictionary.Models;

namespace JP_Dictionary.Services
{
    public class AnimationService
    {
        public event Func<Motions, Task>? OnAnimate;

        public async Task RequestAnimation(Motions motion)
        {
            if (OnAnimate != null)
            {
                await OnAnimate.Invoke(motion);
            }
        }
    }
}
