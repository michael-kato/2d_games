using UnityEngine;

public class Loot : MonoBehaviour
{
    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.activeLoot.Add(this);
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.activeLoot.Remove(this);
        }
    }
}
