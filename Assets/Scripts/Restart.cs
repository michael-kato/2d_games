
using UnityEngine.SceneManagement;
using UnityEngine;

public class Restart : MonoBehaviour {
    
    public void RestartGame() {
        Debug.Log("Restarting Game");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
}