using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using VaultSystems.Data;


/// <summary>
/// Handles Main Menu navigation, Character Selection, Configuration, and Scene Loading.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject characterSelectPanel;
    public GameObject optionsPanel;
    public GameObject confirmModal;
    public GameObject configurePanel; // Configuration UI for save slot + outfit

    [Header("Buttons")]
    public List<Button> mainButtons;            // Play, Options, Quit
    public List<Button> characterButtons;       // Lira, Kinuee, Hos
    public List<Button> optionButtons;          // Back, Volume, etc.
    public Button confirmButton;
    public Button cancelButton;
    public Button configureButton; // "Configure" in modal

    [Header("Character Preview Meshes")]
    public GameObject liraMesh;
    public GameObject kinueeMesh;
    public GameObject hosMesh;
    public GameObject defaultMesh;
    public GameObject previewRotator; // Parent that spins preview

    [Header("Character Outfit Mesh Variants")]
    public GameObject[] liraOutfits;   // 3 total
    public GameObject[] kinueeOutfits; // 3 total
    public GameObject[] hosOutfits;    // 3 total

    [Header("Character Data Prefabs")]
    public LiraData liraDataPrefab;
    public KinueeData kinueeDataPrefab;
    public HosData hosDataPrefab;

    [Header("Character Preview Animators")]
    public Animator liraAnimator;
    public Animator kinueeAnimator;
    public Animator hosAnimator;

    [Header("Audio")]
    public AudioSource uiAudioSource;
    public AudioClip hoverClip;
    public AudioClip confirmClip;
    public AudioClip cancelClip;

    [Header("Button Hover Effect")]
    public float hoverZ = -5f;             // Forward movement when highlighted
    public float hoverSpeed = 4f;          // How fast it moves
    public float waveAmplitude = 0.1f;     // Small oscillation
    public float waveFrequency = 2f;

    [Header("UI References (Configure Panel)")]
    public Button slotLeftButton;
    public Button slotRightButton;
    public Text slotDisplayText;
    public Button outfitLeftButton;
    public Button outfitRightButton;
    public Text outfitDisplayText;
    public Button backButton;
    public Button confirmConfigButton;

    // --- Private runtime vars ---
    private string pendingCharacter = "";
    private PlayerDataContainer selectedCharacterData;
    private GameObject currentActiveMesh;

    private int selectedSlot = 0;
    private int currentSlot = 0;
    private int currentOutfitIndex = 0;
    private int maxOutfits = 3; // per character

    private List<Button> currentButtonList;
    private int selectedButtonIndex = 0;

    private GameObject lastHighlightedButton = null;
    private Vector3 lastButtonStartPos;
    private float waveTimer = 2f;

    private float axisCooldown = 0.2f;
    private float lastAxisTime;

    private void Start()
    {
        // --- Hide loading screen if active ---
        LoadingScreenManager.Instance?.Hide();

        // --- Default mesh setup ---
        liraMesh.SetActive(false);
        kinueeMesh.SetActive(false);
        hosMesh.SetActive(false);
        defaultMesh.SetActive(true);

        // --- Setup UI ---
        mainPanel.SetActive(true);
        characterSelectPanel.SetActive(false);
        optionsPanel.SetActive(false);
        confirmModal.SetActive(false);
        configurePanel.SetActive(false);

        UpdateCharacterPreview("Default");

        // --- Setup default navigation ---
        currentButtonList = mainButtons;
        HighlightSelectedButton();

        // --- Assign hover events ---
        AssignHoverEvents(mainButtons);
        AssignHoverEvents(characterButtons);
        AssignHoverEvents(optionButtons);

        // --- Default slot ---
        currentSlot = GameManager.Instance != null ? GameManager.Instance.currentSlot : 1;

        // --- Hook up configure panel UI buttons ---
        if (slotLeftButton) slotLeftButton.onClick.AddListener(() => ChangeSlot(-1));
        if (slotRightButton) slotRightButton.onClick.AddListener(() => ChangeSlot(1));
        if (outfitLeftButton) outfitLeftButton.onClick.AddListener(() => ChangeOutfit(-1));
        if (outfitRightButton) outfitRightButton.onClick.AddListener(() => ChangeOutfit(1));
        if (backButton) backButton.onClick.AddListener(() => CloseConfigurePanel());
        if (confirmConfigButton) confirmConfigButton.onClick.AddListener(() => ConfirmConfiguration());
    }

    private void Update()
    {
        HandleNavigationInput();
        HandleSubmitCancel();
        UpdateButtonHoverWave();
    }

    #region --- Hover Effects ---
    private void UpdateButtonHoverWave()
    {
        if (lastHighlightedButton == null) return;

        waveTimer += Time.deltaTime * waveFrequency;
        float wave = Mathf.Sin(waveTimer) * waveAmplitude;

        Vector3 targetPos = lastButtonStartPos;
        targetPos.z += hoverZ + wave;

        lastHighlightedButton.transform.localPosition = Vector3.Lerp(
            lastHighlightedButton.transform.localPosition,
            targetPos,
            Time.deltaTime * hoverSpeed
        );

        foreach (var btn in currentButtonList)
        {
            if (btn.gameObject != lastHighlightedButton)
            {
                Vector3 resetPos = btn.transform.localPosition;
                resetPos.z = 0;
                btn.transform.localPosition = Vector3.Lerp(
                    btn.transform.localPosition,
                    resetPos,
                    Time.deltaTime * hoverSpeed
                );
            }
        }
    }
    #endregion

    #region --- Input + Highlight ---
    private void HandleNavigationInput()
    {
        bool moved = false;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedButtonIndex = Mathf.Max(0, selectedButtonIndex - 1);
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedButtonIndex = Mathf.Min(currentButtonList.Count - 1, selectedButtonIndex + 1);
            moved = true;
        }

        if (moved)
        {
            PlayAudio(hoverClip);
            HighlightSelectedButton();
        }
    }

    private void HandleSubmitCancel()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            currentButtonList[selectedButtonIndex].onClick.Invoke();
            PlayAudio(confirmClip);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
            PlayAudio(cancelClip);
        }
    }

    private void HighlightSelectedButton()
    {
        GameObject currentButton = currentButtonList[selectedButtonIndex].gameObject;
        if (EventSystem.current.currentSelectedGameObject != currentButton)
            EventSystem.current.SetSelectedGameObject(currentButton);

        if (currentButtonList == characterButtons)
        {
            string charName = currentButton.name;
            UpdateCharacterPreview(charName);

            // Hover animation
            Animator anim = null;
            switch (charName)
            {
                case "Lira": anim = liraAnimator; break;
                case "Kinuee": anim = kinueeAnimator; break;
                case "Hos": anim = hosAnimator; break;
            }

            anim?.ResetTrigger("Hover");
            anim?.SetTrigger("Hover");
        }

        // hover z-move reset
        if (lastHighlightedButton != currentButton)
        {
            if (lastHighlightedButton != null)
                lastHighlightedButton.transform.localPosition = lastButtonStartPos;

            lastHighlightedButton = currentButton;
            lastButtonStartPos = currentButton.transform.localPosition;
            waveTimer = 2f;
        }
    }

    private void AssignHoverEvents(List<Button> buttons)
    {
        foreach (var button in buttons)
        {
            EventTrigger trigger = button.GetComponent<EventTrigger>() ?? button.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
            entry.callback.AddListener((data) =>
            {
                lastHighlightedButton = button.gameObject;
                lastButtonStartPos = button.transform.localPosition;
                waveTimer = 2f;
            });
            trigger.triggers.Add(entry);
        }
    }
    #endregion

    #region --- Character + Outfit Logic ---
    public void UpdateCharacterPreview(string characterName)
    {
        liraMesh.SetActive(false);
        kinueeMesh.SetActive(false);
        hosMesh.SetActive(false);
        defaultMesh.SetActive(false);

        Animator anim = null;
        switch (characterName)
        {
            case "Lira": currentActiveMesh = liraMesh; anim = liraAnimator; break;
            case "Kinuee": currentActiveMesh = kinueeMesh; anim = kinueeAnimator; break;
            case "Hos": currentActiveMesh = hosMesh; anim = hosAnimator; break;
            default: currentActiveMesh = defaultMesh; break;
        }

        if (currentActiveMesh) currentActiveMesh.SetActive(true);
        anim?.SetTrigger("Hover");
    }

    private void ApplyOutfitToPreview(int outfitIndex)
    {
        outfitIndex = Mathf.Clamp(outfitIndex, 0, 2);

        foreach (var m in liraOutfits) if (m) m.SetActive(false);
        foreach (var m in kinueeOutfits) if (m) m.SetActive(false);
        foreach (var m in hosOutfits) if (m) m.SetActive(false);

        switch (pendingCharacter)
        {
            case "Lira":
                if (liraOutfits.Length > outfitIndex) liraOutfits[outfitIndex].SetActive(true);
                break;
            case "Kinuee":
                if (kinueeOutfits.Length > outfitIndex) kinueeOutfits[outfitIndex].SetActive(true);
                break;
            case "Hos":
                if (hosOutfits.Length > outfitIndex) hosOutfits[outfitIndex].SetActive(true);
                break;
        }
    }

    private void ChangeOutfit(int direction)
    {
        currentOutfitIndex += direction;
        if (currentOutfitIndex > 2) currentOutfitIndex = 0;
        if (currentOutfitIndex < 0) currentOutfitIndex = 2;

        outfitDisplayText.text = $"Outfit: {currentOutfitIndex}";
        ApplyOutfitToPreview(currentOutfitIndex);
        PlayAudio(hoverClip);
    }

    private void ChangeSlot(int direction)
    {
        currentSlot = Mathf.Clamp(currentSlot + direction, 0, 2);
        slotDisplayText.text = $"Slot: {currentSlot}";
        PlayAudio(hoverClip);
    }

    private void InitializeFromData()
    {
        if (selectedCharacterData == null)
        {
            currentSlot = GameManager.Instance != null ? GameManager.Instance.currentSlot : 0;
            currentOutfitIndex = 0;
            return;
        }

        switch (pendingCharacter)
        {
            case "Lira": if (selectedCharacterData is LiraData lira) currentOutfitIndex = lira.outfitIndex; break;
            case "Kinuee": if (selectedCharacterData is KinueeData kin) currentOutfitIndex = kin.outfitIndex; break;
            case "Hos": if (selectedCharacterData is HosData hos) currentOutfitIndex = hos.outfitIndex; break;
        }

        currentSlot = GameManager.Instance != null ? GameManager.Instance.currentSlot : 0;
        slotDisplayText.text = $"Slot: {currentSlot}";
        outfitDisplayText.text = $"Outfit: {currentOutfitIndex}";
        ApplyOutfitToPreview(currentOutfitIndex);
    }
    #endregion

    #region --- Configure Panel ---
    public void OpenConfigurePanel()
    {
        if (!configurePanel) return;

        UpdateCharacterPreview(pendingCharacter);
        confirmModal.SetActive(false);
        configurePanel.SetActive(true);

        InitializeFromData();

        currentButtonList = new List<Button>
        {
            slotLeftButton, slotRightButton,
            outfitLeftButton, outfitRightButton,
            confirmConfigButton, backButton
        };
        selectedButtonIndex = 0;
        HighlightSelectedButton();
    }

    public void CloseConfigurePanel()
    {
        configurePanel.SetActive(false);
       confirmModal.SetActive(false);
        characterSelectPanel.SetActive(true);
        UpdateCharacterPreview(pendingCharacter);
        currentButtonList = characterButtons;
        selectedButtonIndex = 0;
        HighlightSelectedButton();
    }

    public void ConfirmConfiguration()
    {
        currentSlot = Mathf.Clamp(currentSlot, 0, 2);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentSlot = currentSlot;
            GameManager.Instance.SaveGameSlot(currentSlot);
        }

        if (selectedCharacterData != null)
        {
            switch (pendingCharacter)
            {
                case "Lira": if (selectedCharacterData is LiraData lira) lira.outfitIndex = currentOutfitIndex; break;
                case "Kinuee": if (selectedCharacterData is KinueeData kin) kin.outfitIndex = currentOutfitIndex; break;
                case "Hos": if (selectedCharacterData is HosData hos) hos.outfitIndex = currentOutfitIndex; break;
            }
        }

        slotDisplayText.text = $"Slot: {currentSlot}";
        outfitDisplayText.text = $"Outfit: {currentOutfitIndex}";

        configurePanel.SetActive(false);
        confirmModal.SetActive(true);
        PlayAudio(confirmClip);
    }
    #endregion

    #region --- Character Selection ---
    public void OnPlayButton()
    {
        mainPanel.SetActive(false);
        characterSelectPanel.SetActive(true);
        currentButtonList = characterButtons;
        selectedButtonIndex = 0;
        HighlightSelectedButton();
    }

  public void OnCharacterSelect(string characterName)
{
    pendingCharacter = characterName;
    confirmModal.SetActive(true);
    DataContainerManager.Instance?.ClearAll(); // optional cleanup before registering new data

    confirmButton.onClick.RemoveAllListeners();
    cancelButton.onClick.RemoveAllListeners();
    configureButton?.onClick.RemoveAllListeners();

    confirmButton.onClick.AddListener(() => ConfirmCharacter());
    cancelButton.onClick.AddListener(() => CancelCharacter());
    configureButton?.onClick.AddListener(() => OpenConfigurePanel());

    // --- Prevent multiple data containers ---
    if (selectedCharacterData != null)
    {
        // If already exists and matches the selected character, just reuse it
        string existingType = selectedCharacterData.GetType().Name;
        if ((characterName == "Lira" && existingType.Contains("Lira")) ||
            (characterName == "Kinuee" && existingType.Contains("Kinuee")) ||
            (characterName == "Hos" && existingType.Contains("Hos")))
        {
            // Already the correct one, don't instantiate again
            return;
        }
        else
        {
            // Different character selected, destroy old one
            Destroy(selectedCharacterData.gameObject);
            selectedCharacterData = null;
        }
    }           


    // --- Instantiate new one for the pending character ---
    switch (pendingCharacter)
    {
        case "Lira":
            selectedCharacterData = Instantiate(liraDataPrefab);
            selectedSlot = 1;
            break;
        case "Kinuee":
            selectedCharacterData = Instantiate(kinueeDataPrefab);
            selectedSlot = 2;
            break;
        case "Hos":
            selectedCharacterData = Instantiate(hosDataPrefab);
            selectedSlot = 3;
            break;
        default:
            selectedCharacterData = null;
            selectedSlot = 0;
            break;
    }
}


    private async void ConfirmCharacter()
    {
        if (selectedCharacterData == null) return;

        confirmModal.SetActive(false);
        characterSelectPanel.SetActive(true);
        mainPanel.SetActive(false);
        optionsPanel.SetActive(false);

        selectedCharacterData.outfitIndex = currentOutfitIndex;
        selectedCharacterData.isActivePlayer = true;
        GameManager.Instance.currentSlot = selectedSlot;
        GameManager.Instance.SaveGameSlot(selectedSlot);

    var lsm = LoadingScreenManager.Instance;
    if (lsm != null)
    {
        lsm.gameObject.SetActive(true);   // activate object
        lsm.Show("Loading DevHub...");    // handles alpha & interactable
        lsm.SetProgress(0f);
        lsm.SetMessage("Loading DevHub...");
    }
    else
    {
        Debug.LogWarning("[MainMenuController] LoadingScreenManager not found!");
    }  
        var asyncOp = SceneManager.LoadSceneAsync("InitalizationDebug_SpaceHub");
        asyncOp.allowSceneActivation = false;

        while (asyncOp.progress < 0.9f)
            await System.Threading.Tasks.Task.Yield();

        DontDestroyOnLoad(selectedCharacterData.gameObject);
        DataContainerManager.Instance?.Register(selectedCharacterData);
        //skipfownow
        //PersistentWorldManager.Instance?.Initialize(selectedCharacterData);

        asyncOp.allowSceneActivation = true;
        this.enabled = false;
    }

    private void CancelCharacter()
    {
        confirmModal.SetActive(false);
        PlayAudio(cancelClip);
    }
    #endregion

    #region --- Misc ---
    public void OnOptionsButton()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
        currentButtonList = optionButtons;
        selectedButtonIndex = 0;
        HighlightSelectedButton();
    }

    public void OnQuitButton() => Application.Quit();

    public void GoBack()
    {
        if (confirmModal.activeSelf) { CancelCharacter(); return; }

        if (characterSelectPanel.activeSelf)
        {
            characterSelectPanel.SetActive(false);
            mainPanel.SetActive(true);
            currentButtonList = mainButtons;
        }
        else if (optionsPanel.activeSelf)
        {
            optionsPanel.SetActive(false);
            mainPanel.SetActive(true);
            currentButtonList = mainButtons;
        }

        selectedButtonIndex = 0;
        HighlightSelectedButton();
    }

    private void PlayAudio(AudioClip clip)
    {
        if (uiAudioSource && clip)
            uiAudioSource.PlayOneShot(clip);
    }
    #endregion
}
