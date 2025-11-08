using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Assuming TextMeshPro is available

namespace VaultSystems
{
    /// <summary>
    /// Simple dialogue manager for NPC conversations with branching options.
    /// Integrates with FactionSystem for faction-related dialogue choices.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("UI References")]
        public GameObject dialoguePanel;
        public TextMeshProUGUI npcNameText;
        public TextMeshProUGUI dialogueText;
        public Transform optionsContainer;
        public GameObject optionButtonPrefab; // Button prefab with TextMeshProUGUI child

        [Header("Settings")]
        public float textSpeed = 0.05f;
        public AudioClip dialogueSound;

        private Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();
        private bool isTyping = false;
        private Coroutine typingCoroutine;
        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        /// <summary>
        /// Show a dialogue with branching options
        /// </summary>
        public void ShowDialogue(string npcId, string text, DialogueOption[] options)
        {
            if (dialoguePanel == null)
            {
                Debug.LogError("[DialogueManager] Dialogue panel not assigned!");
                return;
            }

            // Set NPC name
            if (npcNameText != null)
            {
                npcNameText.text = FormatNpcName(npcId);
            }

            // Clear previous options
            ClearOptions();

            // Create option buttons
            for (int i = 0; i < options.Length; i++)
            {
                CreateOptionButton(options[i], i + 1);
            }

            // Start typing the dialogue
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            typingCoroutine = StartCoroutine(TypeText(text));

            dialoguePanel.SetActive(true);
        }

        /// <summary>
        /// Show a simple dialogue without options (continues automatically)
        /// </summary>
        public void ShowSimpleDialogue(string npcId, string text, Action onComplete = null)
        {
            ShowDialogue(npcId, text, new[]
            {
                new DialogueOption { text = "Continue", action = () =>
                {
                    HideDialogue();
                    onComplete?.Invoke();
                }}
            });
        }

        /// <summary>
        /// Hide the dialogue panel
        /// </summary>
        public void HideDialogue()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            ClearOptions();
        }

        private void ClearOptions()
        {
            if (optionsContainer == null) return;

            foreach (Transform child in optionsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void CreateOptionButton(DialogueOption option, int index)
        {
            if (optionButtonPrefab == null || optionsContainer == null) return;

            GameObject buttonObj = Instantiate(optionButtonPrefab, optionsContainer);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
            {
                buttonText.text = $"{index}. {option.text}";
            }

            button.onClick.AddListener(() =>
            {
                PlaySound();
                option.action?.Invoke();
            });
        }

        private IEnumerator TypeText(string text)
        {
            isTyping = true;
            dialogueText.text = "";

            foreach (char c in text)
            {
                dialogueText.text += c;
                PlaySound();
                yield return new WaitForSeconds(textSpeed);
            }

            isTyping = false;
        }

        private void PlaySound()
        {
            if (audioSource != null && dialogueSound != null)
            {
                audioSource.PlayOneShot(dialogueSound);
            }
        }

        private string FormatNpcName(string npcId)
        {
            // Convert snake_case to Title Case
            string[] words = npcId.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }
            return string.Join(" ", words);
        }

        // Allow skipping typing animation
        private void Update()
        {
            if (isTyping && Input.GetMouseButtonDown(0))
            {
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                }
                dialogueText.text = dialogueQueue.Peek().text;
                isTyping = false;
            }
        }
    }

    /// <summary>
    /// Represents a dialogue option with text and action
    /// </summary>
    [Serializable]
    public struct DialogueOption
    {
        public string text;
        public Action action;
    }

    /// <summary>
    /// Internal dialogue line structure
    /// </summary>
    internal struct DialogueLine
    {
        public string npcId;
        public string text;
        public DialogueOption[] options;
    }
}
