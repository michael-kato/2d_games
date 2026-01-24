using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using MyExtensions;
using UnityEngine.UI;

public class GameLogic : MonoBehaviour
{
    [SerializeField] public Camera camera;
    [SerializeField] public GameObject cellPrefab;
    [SerializeField] public GameObject cardPrefab;
    [SerializeField] public GameObject gridContainer;
        
    [SerializeField] public int difficulty = 1;
    
    // List is just for animation
    [SerializeField] public List<Sprite> frontTiles;
    
    // random tiles to use
    [SerializeField] public List<Sprite> backTiles;
    [SerializeField] public Sprite fillerTile;

    private Sprite frontTile;
    [ItemCanBeNull] private List<List<Sprite>> tileLayout;

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
    
    void Start()
    {
        _flippedTiles = new List<GameObject>();
        
        frontTile = frontTiles[0];
        int numDuplicates = 1 + difficulty;
        int numTilesGuessable = backTiles.Count * numDuplicates;
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(numTilesGuessable));
        int totalTiles = gridSize * gridSize;
        
        float screenWidth = camera.orthographicSize;
        
        List<Sprite> allPossibleSprites = new List<Sprite>();
        foreach (Sprite sprite in backTiles)
        {
            for (int i = 0; i < numDuplicates; i++)
            {
                allPossibleSprites.Add(sprite);
            }
        }
        while (allPossibleSprites.Count < totalTiles)
        {
            allPossibleSprites.Add(fillerTile);
        }

        allPossibleSprites.Shuffle();
        // initialize random tiles
        tileLayout = new List<List<Sprite>>();
        for (int y = 0; y < gridSize; y++)
        {
            tileLayout.Add(new List<Sprite>(gridSize));
            for (int x = 0; x < gridSize; x++)
            {
                // sprite setup
                Sprite sprite = allPossibleSprites[x + y];
                tileLayout[y].Add(sprite);

                GameObject cell = Instantiate(cellPrefab, gridContainer.transform);
                GameObject card = Instantiate(cardPrefab, cell.transform);
                card.name = sprite.name;
                CardManager cm = card.GetComponent<CardManager>();
                cm.frontSprite = frontTile;
                cm.backSprite = sprite;
                
                // debug, remove
                Image img = card.GetComponent<Image>();
                img.sprite = sprite;

            }
        }

        var count = gridContainer.GetComponentCount();
        Debug.Log(count);
    }

    void Update()
    {
        
    }
    
    
}
