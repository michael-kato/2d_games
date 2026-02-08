using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using SlotsExtensions;
using UnityEngine.Rendering;


public class GameManager : MonoBehaviour
{
    [SerializeField] bool debug;
    
    [SerializeField] public GameObject gridContainer;
    [SerializeField] public GameObject cellPrefab;
    [SerializeField] public GameObject cardPrefab;
    [SerializeField] public GameObject lootPrefab;
    [SerializeField] public GameObject CthulhuPrefab;
    
    [SerializeField] public int difficulty = 1;

    // random tiles to use
    [SerializeField] public List<Sprite> backTiles;
    [SerializeField] public Sprite fillerTile;

    [SerializeField] private Sprite _mysteryTile;
    private static List<GameObject> _flippedTiles;

    private List<Transform> _cellTransforms = new List<Transform>();
    
    private int _score;
    public int Score     {
        get { return _score; }
        set
        {
            _score = value;
            CheckScore();
        }
    }
    
    // events
    public delegate void GameReadyAction();
    public static event GameReadyAction OnGameStarted;
    
    public delegate void SummonCthulhuAction();
    public static event SummonCthulhuAction OnSummonCthulhu;
    
    private static GameManager _instance;

    private GameManager()
    {
    }

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameManager();
            }
            return _instance;
        }
    }
    
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
                Debug.Log("Bad guess! -Aura");
            }

            _flippedTiles.Clear();
        }
    }

    IEnumerator Start()
    {
        _flippedTiles = new List<GameObject>();

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
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                Sprite revealSprite = allPossibleSprites[x + y];
                
                // ideally would pool these
                GameObject cell = Instantiate(cellPrefab, gridContainer.transform);
                GameObject card = Instantiate(cardPrefab, cell.transform);

                _cellTransforms.Add(cell.transform);
                
                // CRITICAL: Set scale to zero immediately so they are ready to animate
                cell.transform.localScale = Vector3.zero;

                card.name = revealSprite.name;
                CardManager cm = card.GetComponent<CardManager>();
                cm.mysterySprite = _mysteryTile;
                cm.revealSprite = revealSprite;

                if (debug)
                {
                    Image img = card.GetComponent<Image>();
                    img.sprite = revealSprite;
                }
            }
        }

        // Wait for Grid Layout to calculate
        yield return new WaitForEndOfFrame();
        
        // ANIMATE CARDS
        foreach (Transform cell in _cellTransforms)
        {
            StartCoroutine(AnimateCardAndLoot(cell));
            yield return new WaitForSeconds(0.05f);
        }

        // drop loot
        foreach (Transform cell in _cellTransforms)
        {
            StartCoroutine(DropLootRoutine(cell));
            yield return new WaitForSeconds(0.05f);
        }

        // WAIT FOR ALL ANIMATIONS TO FINISH
        yield return new WaitForSeconds(_cellTransforms.Count * 0.07f);

        // SHUFFLE THE CELLS
        yield return StartCoroutine(SlidingShuffleRoutine());
        
        OnGameStarted?.Invoke();
    }
    
    IEnumerator AnimateCardAndLoot(Transform cell)
    {
        // 1. POP IN THE CARD
        float duration = 0.4f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            float curve = Mathf.Sin(percent * Mathf.PI * 0.85f) * 1.15f;
            cell.localScale = Vector3.one * curve;
            yield return null;
        }

        if(cell != null) cell.localScale = Vector3.one;

    }

    IEnumerator DropLootRoutine(Transform cell)
    {
        Vector3 targetPos = GetWorldPosForLoot(cell);
        Vector3 spawnPos = Vector3.up * 5f; // todo: 
        
        // TODO: pool
        GameObject loot = Instantiate(lootPrefab, spawnPos, Quaternion.identity);
        
        var card = cell.GetComponentInChildren<CardManager>();
        loot.GetComponent<SpriteRenderer>().sprite = card.revealSprite;
        card.lootDrop = loot;

        // Drop 
        float dropDur = 0.5f;
        float t = 0;
        while (t < 1.0f)
        {
            t += Time.deltaTime / dropDur;
            // Ease in
            float smoothT = t * t;
            loot.transform.position = Vector3.Lerp(spawnPos, targetPos, smoothT);
            yield return null;
        }

        card.gameObject.GetComponent<Image>().sprite = card.revealSprite;
        loot.SetActive(false);
        
        // hide the loot!
        card.Reset();

    }
    
    private IEnumerator SlidingShuffleRoutine()
    {
        // 1. Setup Data Structures
        Dictionary<Transform, Vector3> targetLocalPositions = new Dictionary<Transform, Vector3>();
        List<Transform> cells = new List<Transform>();
        
        // We need to keep track of which loot belongs to which cell/card
        Dictionary<Transform, GameObject> cellToLoot = new Dictionary<Transform, GameObject>();

        foreach (Transform cell in gridContainer.transform)
        {
            targetLocalPositions[cell] = cell.localPosition;
            cells.Add(cell);

            // Find the loot associated with this cell's card
            CardManager cm = cell.GetComponentInChildren<CardManager>();
            if (cm != null && cm.lootDrop != null)
            {
                cellToLoot[cell] = cm.lootDrop;
                // Ensure loot is INACTIVE during shuffle so gravity doesn't take it
                cm.lootDrop.SetActive(false);
            }
        }

        // 3. Randomize the mapping
        List<Vector3> shuffledPositions = new List<Vector3>(targetLocalPositions.Values);
        for (int i = 0; i < shuffledPositions.Count; i++)
        {
            Vector3 temp = shuffledPositions[i];
            int randomIndex = Random.Range(i, shuffledPositions.Count);
            shuffledPositions[i] = shuffledPositions[randomIndex];
            shuffledPositions[randomIndex] = temp;
        }

        // 4. Animate the Slide
        float duration = 0.8f;
        float elapsed = 0f;
        Dictionary<Transform, Vector3> startLocalPositions = new Dictionary<Transform, Vector3>();
        
        foreach(var cell in cells) 
            startLocalPositions[cell] = cell.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = t * t * (3f - 2f * t); // SmoothStep

            for (int i = 0; i < cells.Count; i++)
            {
                Transform currentCell = cells[i];
                
                // Move the Cell (UI)
                currentCell.localPosition = Vector3.Lerp(startLocalPositions[currentCell], shuffledPositions[i], smoothT);
                
                // Move the Loot (World Space) to match the card's new position
                if (cellToLoot.ContainsKey(currentCell))
                {
                    // card.position is the world space center of the UI element
                    Vector3 cardWorldPos = currentCell.GetChild(0).position; 
                    cardWorldPos.z = 0; 
                    cellToLoot[currentCell].transform.position = cardWorldPos;
                }
            }
            yield return null;
        }

        // 5. Cleanup
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].localPosition = shuffledPositions[i];
            
        }
    }
    
    private Vector3 GetWorldPosForLoot(Transform card)
    {
        // Get the card's world position (which works in Screen Space - Camera)
        Vector3 worldPos = card.position;
        worldPos.z = 0; // Ensure it's on the gameplay plane
        return worldPos;
    }

    private void CheckScore()
    {
        if (_score >= 10 || (debug  && _score >= 1))
        {
            SummonCthulhu();
        }
    }

    private void SummonCthulhu()
    {
        Debug.Log("Summoning Cthulhu!!!!!");
        CameraShake.Instance.AddTrauma(50);
        OnSummonCthulhu?.Invoke();
    }
    
}



