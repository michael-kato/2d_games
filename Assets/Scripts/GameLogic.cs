using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using MyExtensions;
using Unity.VisualScripting;

public class GameLogic : MonoBehaviour
{
    [SerializeField] public Camera camera;
    [SerializeField] public GameObject cardPrefab;
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
        float halfWidth = camera.orthographicSize / 2f;
        int numDuplicates = 1 + difficulty;
        int numTilesGuessable = backTiles.Count * numDuplicates;
        int squareSize = Mathf.CeilToInt(Mathf.Sqrt(numTilesGuessable));
        int totalTiles = squareSize * squareSize;
        
        List<Sprite> allPossibleSprites = new List<Sprite>();
        foreach (Sprite sprite in backTiles)
        {
            allPossibleSprites.Add(sprite);
            allPossibleSprites.Add(sprite);
        }
        while (allPossibleSprites.Count < totalTiles)
        {
            allPossibleSprites.Add(fillerTile);
        }

        allPossibleSprites.Shuffle();
        // initialize random tiles
        tileLayout = new List<List<Sprite>>();
        for (int i = 0; i < squareSize; i++)
        {
            tileLayout.Add(new List<Sprite>(squareSize));
            for (int j = 0; j < squareSize; j++)
            {
                // sprite setup
                Sprite sprite = allPossibleSprites[i + j];
                tileLayout[i].Add(sprite);
                
                GameObject card = Instantiate(cardPrefab);
                card.name = sprite.name;
                SpriteRenderer sr = card.GetComponent<SpriteRenderer>();
                CardManager cm = card.GetComponent<CardManager>();
                cm.frontSprite = frontTile;
                cm.backSprite = sprite;
                sr.sprite = sprite; //frontTile; //
                
                card.transform.position = new Vector3(j-halfWidth, i, 0);

            }
        }
    }

    void Update()
    {
        
    }
    
    
}
