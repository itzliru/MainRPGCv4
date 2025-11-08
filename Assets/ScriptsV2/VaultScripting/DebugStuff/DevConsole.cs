using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class DevConsole : MonoBehaviour
{
    public static DevConsole Instance;

    [Header("UI")]
    public GameObject consoleUI;
    public InputField inputField;
    public Text outputLog;

    private Dictionary<string, Action<string[]>> commandTable = new Dictionary<string, Action<string[]>>();
    private List<string> history = new List<string>();
    private int historyIndex = -1;
    private bool isOpen = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        RegisterDefaultCommands();
        consoleUI.SetActive(false);
    }

    private void Update()
    {
        // Toggle console with `
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            ToggleConsole();
        }

        if (isOpen && inputField.isFocused)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SubmitCommand(inputField.text);
                inputField.text = "";
                inputField.ActivateInputField();
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                NavigateHistory(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                NavigateHistory(1);
            }
        }
    }

    private void ToggleConsole()
    {
        isOpen = !isOpen;
        consoleUI.SetActive(isOpen);

        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            inputField.ActivateInputField();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void SubmitCommand(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return;

        outputLog.text += "\n> " + commandLine;
        history.Add(commandLine);
        historyIndex = history.Count;

        string[] parts = commandLine.Split(' ');
        string cmd = parts[0].ToLower();
        string[] args = new string[parts.Length - 1];
        Array.Copy(parts, 1, args, 0, args.Length);

        if (commandTable.TryGetValue(cmd, out var action))
        {
            try
            {
                action.Invoke(args);
            }
            catch (Exception e)
            {
                outputLog.text += "\n[Error] " + e.Message;
            }
        }
        else
        {
            outputLog.text += "\n[Unknown Command]";
        }
    }

    private void NavigateHistory(int direction)
    {
        if (history.Count == 0) return;

        historyIndex = Mathf.Clamp(historyIndex + direction, 0, history.Count - 1);
        inputField.text = history[historyIndex];
        inputField.caretPosition = inputField.text.Length;
    }

    private void RegisterDefaultCommands()
    {
DevConsole.Instance.RegisterCommand("teleport", args =>
{
    if (GameManager.Instance.playerTransform == null) return;

    float x = float.Parse(args[0]);
    float y = float.Parse(args[1]);
    float z = float.Parse(args[2]);

    GameManager.Instance.playerTransform.position = new Vector3(x, y, z);
    DevConsole.Instance.outputLog.text += $"\nTeleported player to ({x},{y},{z})";
});

// Teleport
DevConsole.Instance.RegisterCommand("teleport", args =>
{
    if (Nocliping.Instance == null) return;
    Vector3 pos = new Vector3(
        float.Parse(args[0]),
        float.Parse(args[1]),
        float.Parse(args[2])
    );
    Nocliping.Instance.transform.position = pos;
    DevConsole.Instance.outputLog.text += $"\nTeleported player to {pos}";
});

// Fly toggle
DevConsole.Instance.RegisterCommand("fly", args =>
{
    if (Nocliping.Instance == null) return;
    Nocliping.Instance.ToggleFly();
    DevConsole.Instance.outputLog.text += "\nFly mode toggled";
});

// Noclip toggle
DevConsole.Instance.RegisterCommand("noclip", args =>
{
    if (Nocliping.Instance == null) return;
    Nocliping.Instance.ToggleNoclip();
    DevConsole.Instance.outputLog.text += "\nNoclip mode toggled";
});

        // FPS toggles
        RegisterCommand("fps_on", args => 
        { 
            Application.targetFrameRate = -1; 
            outputLog.text += "\nFPS Unlocked"; 
        });
        RegisterCommand("fps_off", args => 
        { 
            Application.targetFrameRate = 60; 
            outputLog.text += "\nFPS Locked to 60"; 
        });

        // Save/Load slots
        RegisterCommand("save0", args => 
        { 
            GameManager.Instance.currentSlot = 0; 
            GameManager.Instance.SaveGame(); 
            outputLog.text += "\nSave Slot 0"; 
        });
        RegisterCommand("load0", args => 
        { 
            GameManager.Instance.currentSlot = 0; 
            GameManager.Instance.LoadGame(); 
            outputLog.text += "\nLoad Slot 0"; 
        });
        RegisterCommand("save1", args => 
        { 
            GameManager.Instance.currentSlot = 1; 
            GameManager.Instance.SaveGame(); 
            outputLog.text += "\nSave Slot 1"; 
        });
        RegisterCommand("load1", args => 
        { 
            GameManager.Instance.currentSlot = 1; 
            GameManager.Instance.LoadGame(); 
            outputLog.text += "\nLoad Slot 1"; 
        });

        // Reset data
        RegisterCommand("reset_data", args =>
        {
            PlayerPrefs.DeleteAll();
            outputLog.text += "\nData Reset";
        });

        // Example tool toggle
        RegisterCommand("toggle_idtool", args =>
        {
            // Hook into your tool here
            outputLog.text += "\nID Tool toggled";
        });
    }

    /// <summary>
    /// Register commands dynamically from other scripts
    /// </summary>
    public void RegisterCommand(string command, Action<string[]> action)
    {
        commandTable[command.ToLower()] = action;
    }
}
