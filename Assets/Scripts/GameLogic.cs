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

    private static List<Sprite> _flippedTiles;

    
    public  bool CheckGuess(Sprite sprite)
    {
        if (_flippedTiles.Count == 2)
        {
            // check matches
            if (_flippedTiles[0].name == _flippedTiles[1].name)
            {
                Destroy(_flippedTiles[0]);
                Destroy(_flippedTiles[1]);
                _flippedTiles.Clear();
                Debug.Log("You got it! +1");
                return true;
            }

            _flippedTiles[1].GetComponent<CardManager>();
            return false;
        }

        _flippedTiles.Add(sprite);
        return false;
    }
    
    void Start()
    {
        _flippedTiles = new List<Sprite>();
        
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
