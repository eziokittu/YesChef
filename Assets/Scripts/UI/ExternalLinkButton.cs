using UnityEngine;

namespace YesChef
{
    public sealed class ExternalLinkButton : MonoBehaviour
    {
        public void OpenGlitchbongContact()
        {
            Application.OpenURL("https://glitchbong.com/contact");
        }

        public void OpenRepository()
        {
            Application.OpenURL("https://github.com/eziokittu/YesChef");
        }
    }
}
