using System.Collections;

namespace AfterZero
{
    public static class ScreenFader
    {
        public static IEnumerator FadeOut(float seconds) => HUD.Fade(1f, seconds);
        public static IEnumerator FadeIn(float seconds) => HUD.Fade(0f, seconds);
    }
}
