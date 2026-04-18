using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class SceneController : MonoBehaviour
{
    //the current scene where this class runs will not change, it will always stay as scene 0
    //all scenes must be listed in the build scene list
    public GameObject OpenPage;
    public string StartSceneName;
    public Image BlackFade;
    public bool Fade = false;


    private void Start()
    {
        BlackFade.gameObject.SetActive(false);
    }
    public void LoadStartScene()//start button
    {
        StartCoroutine(LoadSceneAndSetActive(StartSceneName));
    }

    //start with different scenes from saved scene name
    public void Resume(string sceneName)
    {
        Debug.Log(sceneName);
        StartCoroutine(LoadSceneAndSetActive(sceneName));
    }

    //the LoadScene function will keep the current scene as scene 0 and switch the TheAditiveScene scene with a new scene as scene 1
    public void LoadScene(string sceneName)
    {
        StartCoroutine(SwitchScenes(sceneName));
    }
    private IEnumerator SwitchScenes(string sceneName)
    {
        Fade = true;
        BlackFade.gameObject.SetActive(true);
        BlackFade.canvasRenderer.SetAlpha(0.01f);
        BlackFade.CrossFadeAlpha(1.0f, 2, true);
        yield return new WaitForSeconds(2);

        //unload the last aditive scene
        yield return SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        //load the new aditive scene
        yield return StartCoroutine(LoadSceneAndSetActive(sceneName));
        
    }
    private IEnumerator LoadSceneAndSetActive(string sceneName)
    {
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        Scene newlyLoadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
        SceneManager.SetActiveScene(newlyLoadedScene);

        OpenPage.SetActive(false);
       // Game.Instance.openPage.CloseOpenPage();
       // PlayerPrefs.SetString("PlayerScene", sceneName);
       // PlayerPrefs.Save();

        if(Fade == true)
        {
            yield return new WaitForSeconds(0.5f);
            BlackFade.canvasRenderer.SetAlpha(1.0f);
            BlackFade.CrossFadeAlpha(0.01f, 2, true);
            yield return new WaitForSeconds(2);
            BlackFade.gameObject.SetActive(false);
        }
    }
}
