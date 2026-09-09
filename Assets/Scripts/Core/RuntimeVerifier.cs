using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace YesChef
{
    public sealed class RuntimeVerifier : MonoBehaviour
    {
        private void Start()
        {
            var arguments = Environment.GetCommandLineArgs();
            if (Array.IndexOf(arguments, "-yeschef-gameplay-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(true));
            }
            else if (Array.IndexOf(arguments, "-yeschef-capture") >= 0)
            {
                StartCoroutine(CaptureAndQuit(false));
            }
        }

        private static IEnumerator CaptureAndQuit(bool beginGame)
        {
            yield return null;
            if (beginGame) GameManager.Instance.BeginGame();
            yield return new WaitForSecondsRealtime(0.35f);
            yield return new WaitForEndOfFrame();
            var fileName = beginGame ? "YesChef_Gameplay_Verification.png" : "YesChef_Verification.png";
            var outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", fileName));
            ScreenCapture.CaptureScreenshot(outputPath, 1);
            yield return new WaitForSecondsRealtime(2f);
            Application.Quit(0);
        }
    }
}
