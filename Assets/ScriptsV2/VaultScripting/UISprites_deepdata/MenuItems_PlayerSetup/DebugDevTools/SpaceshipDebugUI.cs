using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VaultSystems.Data;
public class SpaceshipDebugUI : MonoBehaviour
{
    public Text playerNameText;
    public Text outfitIndexText;
    public Text currentSlotText;
    public Button loadOverworldButton;

    private void Start()
    {
        // Grab the active player data (the one you instantiated in MainMenu)
        var data = FindObjectOfType<PlayerDataContainer>();

        if (data != null)
        {
            playerNameText.text = $"Player: {data.displayName}";
            outfitIndexText.text = $"Outfit Index: {GetOutfitIndex(data)}";
        }
        else
        {
            playerNameText.text = "No PlayerDataContainer found!";
            outfitIndexText.text = "Outfit Index: N/A";
        }

        if (GameManager.Instance != null)
        {
            currentSlotText.text = $"Slot: {GameManager.Instance.currentSlot}";
        }

        loadOverworldButton.onClick.AddListener(() =>
        {
            // For testing, trigger load to overworld
            UnityEngine.SceneManagement.SceneManager.LoadScene("Overworld");
        });
    }

    private int GetOutfitIndex(PlayerDataContainer data)
    {
        if (data is LiraData lira) return lira.outfitIndex;
        if (data is KinueeData kin) return kin.outfitIndex;
        if (data is HosData hos) return hos.outfitIndex;
        return -1;
    }
}
