using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VisualScoreboard : MonoBehaviour
{
    [Header("Counter")]
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private string counterFormat = "Matches: {0}";

    [Header("Matched Tiles")]
    [SerializeField] private Transform matchedTilesContainer;
    [SerializeField] private Image matchedTileIconPrefab;

    private readonly List<Image> _matchedTileIcons = new List<Image>();
    private int _matchCount;

    private void OnEnable()
    {
        GameManager.OnGameStarted += ResetScoreboard;
        GameManager.OnMatchedTile += AddMatchedTile;
    }

    private void OnDisable()
    {
        GameManager.OnGameStarted -= ResetScoreboard;
        GameManager.OnMatchedTile -= AddMatchedTile;
    }

    private void Start()
    {
        ResetScoreboard();
    }

    private void ResetScoreboard()
    {
        _matchCount = 0;

        for (int i = 0; i < _matchedTileIcons.Count; i++)
        {
            if (_matchedTileIcons[i] != null)
            {
                Destroy(_matchedTileIcons[i].gameObject);
            }
        }

        _matchedTileIcons.Clear();
        RefreshCounter();
    }

    private void AddMatchedTile(Sprite matchedSprite)
    {
        if (matchedSprite == null || matchedTilesContainer == null || matchedTileIconPrefab == null)
        {
            return;
        }

        Image icon = Instantiate(matchedTileIconPrefab, matchedTilesContainer);
        icon.sprite = matchedSprite;

        _matchedTileIcons.Add(icon);
        _matchCount = _matchedTileIcons.Count;
        
        RefreshCounter();
    }

    private void RefreshCounter()
    {
        if (counterText != null)
        {
            counterText.text = string.Format(counterFormat, _matchCount);
        }
    }
}
