using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using VaultSystems.Data;



namespace VaultSystems.Data
{
    // The global weather wizard—ruling the skies and scenes!
    public class GlobalWorldManager : MonoBehaviour
    {
        public static GlobalWorldManager Instance { get; private set; } // The one true maestro!

        [Header("Time")]                        // Time to set the rhythm!
        [Range(0f, 24f)] public float currentHour = 12f; // What time is it, party time?
        public float timeSpeed = 0.05f;         // How fast we roll through the hours

        [Header("Day/Night")]                   // Lighting the world stage!
        public Light directionalLight;          // The sun’s spotlight
        public Gradient ambientColor, sunColor; // Day-to-night vibes

        [Header("Weather")]                     // Control the elements, darling!
        public string currentWeather = "Clear"; // Starting with sunshine
        public bool isRaining, isStorming, isClear; // Weather drama flags

        [Header("Weather Effects")]             // Visual flair for the weather!
        public GameObject rainFX, stormFX, clearFX; // FX to dazzle
        [Header("Weather Audio")]               // Soundtrack for the skies!
        public AudioSource ambientAudio;        // The mood music
        public AudioClip rainClip, stormClip, clearClip; // Weather jams
        [Range(0f, 1f)] public float indoorMuffleVolume = 0.25f; // Indoor hush level

        [Header("Environment")]                 // The world’s setting!
        public SceneCellContainer currentCell;  // Current scene star
        public string currentCellName = "Overworld"; // Default hangout
        public bool isIndoors;                  // Inside or out?

        public event Action<SceneCellContainer> OnCellChanged; // Shout when the scene shifts!

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);            // Out with the fake!
            }
            else
            {
                Instance = this;                // Claim the crown!
                DontDestroyOnLoad(gameObject);  // Eternal reign!
            }
            SceneManager.sceneLoaded += OnSceneLoaded; // Hook into scene magic
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // Unhook like a pro
        }

        private void Start()
        {
            StartCoroutine(InitializeWorld());   // Kick off the setup party
            StartCoroutine(WeatherCycle());     // Let the weather dance begin!
        }

        private IEnumerator InitializeWorld()
        {
            yield return new WaitForEndOfFrame(); // Take a beat
            var cells = DataContainerManager.Instance?.GetAllContainers()
                .OfType<SceneCellContainer>().Where(c => c.isLoaded).ToList(); // Find the loaded crew
            SetCell(cells?.FirstOrDefault() ?? null, cells == null ? SceneManager.GetActiveScene().name : null, false); // Set the stage
        }

        private IEnumerator WeatherCycle()
        {
            float seed = Time.time;             // Random seed, let’s mix it up
            float checkInterval = 60f;          // Check every minute
            float changeProbability = 0.25f;    // 25% chance of drama

            string[] weatherOptions = { "Clear", "Rain", "Storm" }; // Weather menu
            string current = currentWeather;    // Current vibe

            while (true)
            {
                float timeFactor = (currentHour + seed) / 24f; // Time magic
                float randomValue = Mathf.Abs(Mathf.Sin(timeFactor * Mathf.PI * 2f) * seed); // Wild card
                if (UnityEngine.Random.value < changeProbability)
                {
                    var options = weatherOptions.Where(w => w != current).ToList(); // New options
                    current = options[Mathf.FloorToInt(randomValue * options.Count) % options.Count]; // Pick a winner
                    SetWeather(current);                // Change the scene
                }

                if (isStorming && stormFX != null)
                {
                    var ps = stormFX.GetComponent<ParticleSystem>(); // Storm effects
                    if (ps != null && UnityEngine.Random.value < changeProbability)
                    {
                        var main = ps.main;             // Tweak the storm
                        main.startSpeed = Mathf.Lerp(5f, 15f, randomValue % 1f); // Speed it up
                        main.startSize = Mathf.Lerp(0.5f, 2f, randomValue % 1f); // Make it pop
                    }
                }
                yield return new WaitForSeconds(checkInterval); // Chill for a bit
            }
        }
          private void SetFXRotation(GameObject fx) {if (fx != null)  fx.transform.localRotation = Quaternion.Euler(90f, -90f, -90f);
        }
        private void Update()
        {
            AdvanceTime();                      // Tick the clock
            UpdateLighting();                   // Light the stage
        }

        private void AdvanceTime()
        {
            currentHour += Time.deltaTime * timeSpeed; // Move through time
            if (currentHour >= 24f) currentHour = 0f; // Reset the day
        }

        private void UpdateLighting()
        {
            if (directionalLight)
            {
                float t = currentHour / 24f;    // Time ratio
                directionalLight.color = sunColor.Evaluate(t); // Set sun color
                RenderSettings.ambientLight = ambientColor.Evaluate(t); // Ambient glow
                directionalLight.transform.rotation = Quaternion.Euler((t * 360f) - 90f, 170f, 0); // Spin the light
            }
        }

        public void SetWeather(string newWeather)
        {
            isRaining = isStorming = isClear = false; // Reset the drama
            rainFX?.SetActive(false); stormFX?.SetActive(false); clearFX?.SetActive(false); // Turn off the lights

            switch (newWeather.ToLower())
            {
                case "rain":
                    isRaining = true; currentWeather = "Rain";
                    if (rainFX && !isIndoors) { SetFXRotation(rainFX); rainFX.SetActive(true); } // Rain dance on!
                    PlayWeatherAudio(rainClip);
                    break;

                case "storm":
                    isStorming = true; currentWeather = "Storm";
                    if (stormFX && !isIndoors) { SetFXRotation(stormFX); stormFX.SetActive(true); } // Stormy vibes!
                    PlayWeatherAudio(stormClip);
                    break;

                default:
                    isClear = true; currentWeather = "Clear";
                    if (clearFX && !isIndoors) { SetFXRotation(clearFX); clearFX.SetActive(true); } // Clear skies ahead!
                    PlayWeatherAudio(clearClip);
                    break;
            }
            Debug.Log($"[GlobalWorldManager] Weather set to {currentWeather} (Indoors: {isIndoors})"); // Weather report!
        }

        private void PlayWeatherAudio(AudioClip clip)
        {
            if (ambientAudio)
            {
                if (clip == null)
                    ambientAudio.Stop();            // Silence the noise
                else
                {
                    ambientAudio.clip = clip;        // New tune
                    ambientAudio.loop = true;        // Keep it looping
                    ambientAudio.volume = isIndoors ? indoorMuffleVolume : 1f; // Indoor hush or full blast
                    ambientAudio.Play();            // Hit play!
                }
            }
        }

        public void SetCell(SceneCellContainer cell)
        {
            if (cell == currentCell) return;    // No repeat performances
            currentCell = cell;                 // New star on stage
            currentCellName = cell?.cellName ?? currentCellName; // Name check
            isIndoors = cell?.isIndoor ?? isIndoors; // Indoor or out?
            ApplyCellSettings();                // Set the scene
            OnCellChanged?.Invoke(cell);        // Alert the crew
            Debug.Log($"[GlobalWorldManager] Entered cell '{currentCellName}' (Indoors: {isIndoors})"); // Welcome message!
        }

        public void SetCell(SceneCellContainer cell, string fallbackName, bool fallbackIndoor)
        {
            if (cell == currentCell) return;    // No duplicates, please
            currentCell = cell;                 // New lead
            currentCellName = cell?.cellName ?? fallbackName; // Fallback name
            isIndoors = cell?.isIndoor ?? fallbackIndoor; // Fallback indoor
            ApplyCellSettings();                // Tweak the settings
            OnCellChanged?.Invoke(cell);        // Ring the bell
        }

        private void ApplyCellSettings()
        {
            rainFX?.SetActive(!isIndoors && isRaining); SetFXRotation(rainFX); // Rain check
            stormFX?.SetActive(!isIndoors && isStorming); SetFXRotation(stormFX); // Storm check
            clearFX?.SetActive(!isIndoors && isClear); SetFXRotation(clearFX); // Clear check
            if (ambientAudio?.isPlaying ?? false) ambientAudio.volume = isIndoors ? indoorMuffleVolume : 1f; // Volume vibes
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var cell = DataContainerManager.Instance?.GetAllContainers()
                .OfType<SceneCellContainer>().FirstOrDefault(c => c.sceneName == scene.name); // Find the cell
            SetCell(cell ?? null, scene.name, false); // Set the stage
        }
    }
}