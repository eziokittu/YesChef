using TMPro;
using UnityEngine;

namespace YesChef
{
    /// <summary>Keeps short and long customer lines inside an appropriately sized cloud.</summary>
    public sealed class DialogueBubbleLayout : MonoBehaviour
    {
        public TMP_Text dialogueText;
        public Vector2 shortSize = new(540f, 225f);
        public Vector2 mediumSize = new(640f, 267f);
        public Vector2 longSize = new(720f, 300f);
        public Vector2 extraLongSize = new(800f, 333f);
        public float minimumFontSize = 18f;
        public float maximumFontSize = 33f;

        private RectTransform bubbleRect;

        private void Awake()
        {
            bubbleRect = transform as RectTransform;
            Configure(dialogueText != null ? dialogueText.text : string.Empty);
        }

        public void Configure(string message)
        {
            if (bubbleRect == null) bubbleRect = transform as RectTransform;
            if (dialogueText == null || bubbleRect == null) return;
            dialogueText.text = message;

            var visibleCharacters = CountVisibleCharacters(message);
            var lineBreaks = 0;
            foreach (var character in message)
                if (character == '\n') lineBreaks++;

            var tier = visibleCharacters <= 24 && lineBreaks == 0 ? 0
                : visibleCharacters <= 50 && lineBreaks <= 1 ? 1
                : visibleCharacters <= 80 && lineBreaks <= 2 ? 2 : 3;
            var sizes = new[] { shortSize, mediumSize, longSize, extraLongSize };
            var tierMaximum = Mathf.Max(minimumFontSize,
                maximumFontSize - (tier == 0 ? 0f : tier == 1 ? 2f : tier == 2 ? 4f : 6f));

            // First grow through the available cloud tiers. If the longest cloud is
            // still too tight, reduce the type one point at a time until it fits.
            dialogueText.enableAutoSizing = false;
            dialogueText.overflowMode = TextOverflowModes.Overflow;
            dialogueText.textWrappingMode = TextWrappingModes.Normal;
            for (var sizeTier = tier; sizeTier < sizes.Length; sizeTier++)
            {
                bubbleRect.sizeDelta = sizes[sizeTier];
                if (TryFit(message, tierMaximum)) return;
            }

            bubbleRect.sizeDelta = extraLongSize;
            for (var fontSize = Mathf.Floor(tierMaximum); fontSize >= minimumFontSize; fontSize--)
                if (TryFit(message, fontSize)) return;

            dialogueText.fontSize = minimumFontSize;
            var minimumPreferred = dialogueText.GetPreferredValues(
                message, Mathf.Max(1f, extraLongSize.x - 112f), Mathf.Infinity);
            var requiredHeight = Mathf.Max(extraLongSize.y, minimumPreferred.y + 100f);
            bubbleRect.sizeDelta = new Vector2(
                Mathf.Max(extraLongSize.x, requiredHeight * 2.4f), requiredHeight);
        }

        private bool TryFit(string message, float fontSize)
        {
            dialogueText.fontSize = fontSize;
            // The generated cloud reserves 42/70 px horizontally and 48/52 px
            // vertically for the irregular illustrated edge.
            var availableWidth = Mathf.Max(1f, bubbleRect.sizeDelta.x - 112f);
            var availableHeight = Mathf.Max(1f, bubbleRect.sizeDelta.y - 100f);
            var preferred = dialogueText.GetPreferredValues(message, availableWidth, Mathf.Infinity);
            return preferred.x <= availableWidth + 1f && preferred.y <= availableHeight + 1f;
        }

        private static int CountVisibleCharacters(string value)
        {
            var count = 0;
            var insideTag = false;
            foreach (var character in value)
            {
                if (character == '<') insideTag = true;
                else if (character == '>') insideTag = false;
                else if (!insideTag && character != '\n' && character != '\r') count++;
            }
            return count;
        }
    }
}
