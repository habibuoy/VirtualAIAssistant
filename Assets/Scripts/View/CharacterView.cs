using UnityEngine;

namespace VirtualAiAssistant.View
{
    public class CharacterView : MonoBehaviour
    {
        [SerializeField]
        private Animator characterAnimator;

        public void FadeToListening(float duration = 0.1f)
        {
            characterAnimator.CrossFade(AvatarAnimation.Listening, duration);
            characterAnimator.SetBool(AvatarAnimation.ParamIsListening, true);
        }

        public void FadeToTalking(float duration = 0.1f)
        {
            characterAnimator.CrossFade(AvatarAnimation.Talking, duration);
            characterAnimator.SetBool(AvatarAnimation.ParamIsListening, false);
            characterAnimator.SetBool(AvatarAnimation.ParamIsTalking, true);
        }

        public void FadeToIdle(float duration = 0.1f)
        {
            characterAnimator.CrossFade(AvatarAnimation.Idle, duration);
            characterAnimator.SetBool(AvatarAnimation.ParamIsTalking, false);
        }
    }

    public static class AvatarAnimation
    {
        public static int Idle = Animator.StringToHash("Idle");
        public static int Listening = Animator.StringToHash("Listening");
        public static int Talking = Animator.StringToHash("Talking");

        public static int ParamIsListening = Animator.StringToHash("IsListening");
        public static int ParamIsTalking = Animator.StringToHash("IsTalking");
    }
}
