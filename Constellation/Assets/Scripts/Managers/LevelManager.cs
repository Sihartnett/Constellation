using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour {

    public void ReloadScene() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextScene() {
        int t_totalAmountOfScenes = SceneManager.sceneCountInBuildSettings;
        int t_nextScene = (SceneManager.GetActiveScene().buildIndex + 1) % t_totalAmountOfScenes;
        LoadScene(t_nextScene);
    }

    public void LoadScene(int _scene) {
        if(Application.CanStreamedLevelBeLoaded(_scene)) {
            SceneManager.LoadScene(_scene);
        } else {
            Debug.LogError($"[LEVEL MANAGER] Scene with build index {_scene} couldn't be loaded");
        }
    }

    // -------------------------------------------------------------------
    // MAIN MENU

    public void StartGame() {
        LoadNextScene();
    }

    public void QuitGame() {
        Application.Quit();
    }
}
