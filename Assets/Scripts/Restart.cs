
using UnityEngine.SceneManagement;
using UnityEngine;

public class Restart : MonoBehaviour {
    
    public void RestartGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // loads current scene
    }
    
}