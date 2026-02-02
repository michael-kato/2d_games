using System.Collections.Generic;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

using SlotsExtensions;


public class GameLogic : MonoBehaviour
{
    [SerializeField] public Camera camera;
    [SerializeField] public Canvas canvas;
    [SerializeField] public GameObject gridContainer;
    [SerializeField] public GameObject cellPrefab;
    [SerializeField] public GameObject cardPrefab;
    [SerializeField] public GameObject lootPrefab;
    
    [SerializeField] public int difficulty = 1;
    
    // List is just for animation
    [SerializeField] public List<Sprite> frontTiles;
    
    // random tiles to use
    [SerializeField] public List<Sprite> backTiles;
    [SerializeField] public Sprite fillerTile;

    private Sprite _frontTile;
    [ItemCanBeNull] private List<List<Sprite>> _tileLayout;
    private static List<GameObject> _flippedTiles;

    public static void CheckGuess(GameObject card)
    {
        _flippedTiles.Add(card);
        
        if (_flippedTiles.Count == 2)
        {
            CardManager cm1 = _flippedTiles[0].GetComponent<CardManager>();
            CardManager cm2 = _flippedTiles[1].GetComponent<CardManager>();
            
            // check matches
            if (_flippedTiles[0].name == _flippedTiles[1].name)
            {
                cm1.Vaporize();
                cm2.Vaporize();
                Debug.Log("You got it! +1");
            }
            else
            {
                cm1.Reset();
                cm2.Reset();
            }
            
            _flippedTiles.Clear();
        }
    }
    
    IEnumerator Start()
    {
        _flippedTiles = new List<GameObject>();
        
        _frontTile = frontTiles[0];
        int numDuplicates = 1 + difficulty;
        int numTilesGuessable = backTiles.Count * numDuplicates;
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(numTilesGuessable));
        int totalTiles = gridSize * gridSize;
        
        // fill list with some of each type of sprite available
        List<Sprite> allPossibleSprites = new List<Sprite>();
        foreach (Sprite sprite in backTiles)
        {
            for (int i = 0; i < numDuplicates; i++)
            {
                allPossibleSprites.Add(sprite);
            }
        }
        // fill remaining places with generic cards
        while (allPossibleSprites.Count < totalTiles)
        {
            allPossibleSprites.Add(fillerTile);
        }
        
        // shuffle!
        allPossibleSprites.Shuffle();
        
        // initialize random tiles
        _tileLayout = new List<List<Sprite>>();
        for (int y = 0; y < gridSize; y++)
        {
            _tileLayout.Add(new List<Sprite>(gridSize));
            for (int x = 0; x < gridSize; x++)
            {
                // sprite setup
                Sprite sprite = allPossibleSprites[x + y];
                _tileLayout[y].Add(sprite);

                GameObject cell = Instantiate(cellPrefab, gridContainer.transform);
                GameObject card = Instantiate(cardPrefab, cell.transform);
                card.name = sprite.name;
                CardManager cm = card.GetComponent<CardManager>();
                cm.frontSprite = _frontTile;
                cm.backSprite = sprite;
                
                // DEBUG
                Image img = card.GetComponent<Image>();
                img.sprite = sprite;

            }
        }

        Debug.Log(gridContainer.GetComponentCount());
        
        // Wait until the UI is definitely finished moving
        yield return new WaitForEndOfFrame();

        foreach(CardManager card in gridContainer.GetComponentsInChildren<CardManager>())
        {
            SpawnLoot(card);
        }
    }

    private void SpawnLoot(CardManager card)
    {
        RectTransform rect = card.gameObject.GetComponent<RectTransform>();
        Vector3 cardLocalPos = rect.localPosition; 
        Vector3 containerPos = gridContainer.GetComponent<Transform>().localPosition;
        
        // get a vector from the bottom left corner to the center of the canvas
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        canvasRect.GetWorldCorners(corners);
        Vector3 bottomLeft = corners[0];
        Vector3 resultVector = containerPos - bottomLeft;

        Vector3 worldPos = camera.ScreenToWorldPoint(cardLocalPos);
        Vector3 offset = worldPos + resultVector;
        offset.z = 0;
        
        GameObject loot = Instantiate(lootPrefab, offset, Quaternion.identity);
        loot.GetComponent<SpriteRenderer>().sprite = card.GetComponent<Image>().sprite;
        card.lootDrop = loot;
    }
    
}

