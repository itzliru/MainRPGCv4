using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour
{
    public GameObject iconPrefab;
    private List<QuestMarker> questMarkers = new List<QuestMarker>();
    public RawImage compassImage;
    public Transform player;
    public float maxDistance = 200f;
    float compassUnit;

    public QuestMarker one;
    public QuestMarker two;
    public QuestMarker three;

    [Header("Dynamic Player Grab")]
    public float pollInterval = 0.5f; // how often to try grabbing the player

    private void Start()
    {
        compassUnit = compassImage.rectTransform.rect.width / 360f;

        AddQuestMarker(one);
        AddQuestMarker(two);
        AddQuestMarker(three);

        StartCoroutine(TryAssignPlayer());
    }

    private IEnumerator TryAssignPlayer()
    {
        while (player == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.playerObject != null)
            {
                player = GameManager.Instance.playerObject.transform;
                Debug.Log("[Compass] Player transform assigned dynamically from GameManager");
                yield break; // stop the coroutine once we succeed
            }

            yield return new WaitForSeconds(pollInterval);
        }
    }

    private void Update()
    {
        if (player == null) return;

        compassImage.uvRect = new Rect(player.localEulerAngles.y / 360f, 0f, 1f, 1f);

        foreach (QuestMarker marker in questMarkers)
        {
            marker.image.rectTransform.anchoredPosition = GetPosOnCompass(marker);

            float dst = Vector2.Distance(
                new Vector2(player.position.x, player.position.z),
                marker.position
            );
            float scale = 0f;

            if (dst < maxDistance)
                scale = 1f - (dst / maxDistance);

            marker.image.rectTransform.localScale = Vector3.one * scale;
        }
    }

    public void AddQuestMarker(QuestMarker marker)
    {
        GameObject newMarker = Instantiate(iconPrefab, compassImage.transform);
        marker.image = newMarker.GetComponent<Image>();
        marker.image.sprite = marker.icon;

        questMarkers.Add(marker);
    }

    Vector2 GetPosOnCompass(QuestMarker marker)
    {
        Vector2 playerPos = new Vector2(player.position.x, player.position.z);
        Vector2 playerFwd = new Vector2(player.forward.x, player.forward.z);

        float angle = Vector2.SignedAngle(marker.position - playerPos, playerFwd);
        return new Vector2(compassUnit * angle, 0f);
    }
}
