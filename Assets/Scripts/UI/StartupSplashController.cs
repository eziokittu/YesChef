using System;
using System.Collections;
using UnityEngine;

namespace YesChef
{
    /// <summary>Shows the supplied cover artwork briefly while startup music begins.</summary>
    public sealed class StartupSplashController : MonoBehaviour
    {
        public GameObject splashRoot;
        [Min(.1f)] public float displaySeconds = 2f;

        private void Start()
        {
            if (splashRoot == null) return;
            if (IsAutomatedProbe())
            {
                splashRoot.SetActive(false);
                return;
            }

            splashRoot.SetActive(true);
            AudioDirector.Instance?.EnsureMusicPlaying();
            StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(displaySeconds);
            splashRoot.SetActive(false);
        }

        private static bool IsAutomatedProbe()
        {
            foreach (var argument in Environment.GetCommandLineArgs())
                if (argument.StartsWith("-yeschef-", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
