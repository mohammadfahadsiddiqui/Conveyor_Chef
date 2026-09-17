using UnityEngine;

namespace Watermelon
{
    public class SettingsHome : SettingsButtonBase
    {
        public override bool IsActive()
        {
#if MODULE_IAP
            return true;
#else
            return false;
#endif
        }

        public override void OnClick()
        {
            AudioController.PlaySound(AudioController.Sounds.buttonSound);

            // Simple: just go back to level selection
            UnityEngine.SceneManagement.SceneManager.LoadScene("menu");

            // Play button sound
            
        }
    }
}

// -----------------
// Settings Panel v 0.3
// -----------------