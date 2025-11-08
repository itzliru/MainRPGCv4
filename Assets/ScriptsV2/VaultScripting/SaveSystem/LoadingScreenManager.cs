using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using VaultSystems.Data;
namespace VaultSystems.Data
{
    // The loading screen diva—stealing the show while we wait!
    public class LoadingScreenManager : MonoBehaviour
    {
        [Header("UI References")]            // The glamorous cast of the UI stage!
        public CanvasGroup canvasGroup;     // Fade master, control the drama!
        public Image loadingBarFill;        // Progress bar, filling up the hype!
        public Image backgroundImage;       // Pretty backdrop, set the mood
        public Text loadingText;            // Words of wisdom at the bottom

        [Header("Fade Settings")]           // Tweak the fade magic here!
        public float fadeSpeed = 5f;        // How fast we strut into view
        public float fadeDuration = 0.5f;   // Quick fade or slow tease?

        public static LoadingScreenManager Instance; // The one and only loading queen!

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;                // Claiming the throne!
                canvasGroup.alpha = 0f;         // Start invisible, sneaky!
                canvasGroup.interactable = canvasGroup.blocksRaycasts = false; // No touchy!
                DontDestroyOnLoad(gameObject);  // Eternal loading glory!
            }
            else
            {
                Destroy(gameObject);            // Out with the copycat!
            }
        }

        // Show the loading screen with a sassy message—time to shine!
        public void Show(string message = "Loading...")
        {
            if (canvasGroup)
            {
                canvasGroup.interactable = canvasGroup.blocksRaycasts = true; // Take the stage!
                StartCoroutine(FadeCanvasGroup(1f)); // Fade in like a star
            }

            if (loadingText)
                loadingText.text = message;     // Spill the tea!

            if (loadingBarFill)
                loadingBarFill.fillAmount = 0f; // Start from zero, build the hype!
        }

        // Hide the loading screen—exit stage left!
        public void Hide()
        {
            if (canvasGroup)
            {
                canvasGroup.interactable = canvasGroup.blocksRaycasts = false; // Lights out!
                StartCoroutine(FadeCanvasGroup(0f)); // Fade away gracefully
            }
        }

        // Set the progress bar—watch it grow, baby!
        public void SetProgress(float progress)
        {
            if (loadingBarFill)
                loadingBarFill.fillAmount = Mathf.Clamp01(progress); // Keep it legit

            if (backgroundImage)
                backgroundImage.color = Color.Lerp(Color.black, Color.white, progress); // Smooth transition
        }

        // Update the loading message—new gossip to share!
        public void SetMessage(string message)
        {
            if (loadingText)
                loadingText.text = message;     // Update the chatter
        }

        // Fade the canvas like a pro—smooth moves only!
        private IEnumerator FadeCanvasGroup(float targetAlpha)
        {
            float timer = 0f;               // Start the clock
            float start = canvasGroup.alpha; // Where we begin

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;    // Tick tock
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, timer / fadeDuration); // Glide in style
                yield return null;          // Take a breather
            }

            canvasGroup.alpha = targetAlpha; // Lock it in!
        }

        // Smoothly progress the bar—elegant and chill!
        public IEnumerator SmoothProgress(float targetProgress, float speed = 2f)
        {
            float current = loadingBarFill?.fillAmount ?? 0f; // Start where we are

            while (!Mathf.Approximately(current, targetProgress))
            {
                current = Mathf.Lerp(current, targetProgress, Time.deltaTime * speed); // Ease into it
                SetProgress(current);                       // Update the show
                yield return null;                          // Catch a beat
            }

            SetProgress(targetProgress);                    // Final polish!
        }
    }
}