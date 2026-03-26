using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{

    public static int activeLevel;
    public static bool isPlay;
    [SerializeField] GameObject gamePanel, level, winPanel, losePanel, menuPanel;

    [Header("UI Backgrounds")]
    public UnityEngine.UI.Image gameBackgroundImage;
    public UnityEngine.UI.Image menuBackgroundImage;
    
    [Header("Game Background Cycle")]
    public Sprite[] gameBackgroundSprites; // Add your 10 background sprites here
    private int currentBackgroundIndex = 0;

    [Header("Menu Background")]
    public Sprite menuBackgroundSprite;
    // Start is called before the first frame update
    public float backgroundChangeInterval = 5.0f;
    public float fadeDuration = 3.5f;
    public float maxAlpha = 0.25f;

    private Coroutine backgroundRoutine;

    void Start()
    {
        if (PlayerPrefs.HasKey("activeLevel") == true)
            activeLevel = PlayerPrefs.GetInt("activeLevel");
        else
            activeLevel = 0;

        if (isPlay == true)
        {
            StartGame();
        }

        // Apply external background sprites if assigned
        if (menuBackgroundImage != null && menuBackgroundSprite != null)
            menuBackgroundImage.sprite = menuBackgroundSprite;

        // Start cycling game backgrounds
        if (gameBackgroundSprites != null && gameBackgroundSprites.Length > 0 && gameBackgroundImage != null)
        {
            backgroundRoutine = StartCoroutine(ChangeBackgroundRoutine());
        }
    }

    private IEnumerator ChangeBackgroundRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(backgroundChangeInterval);

            Sprite nextSprite = gameBackgroundSprites[currentBackgroundIndex];
            currentBackgroundIndex = (currentBackgroundIndex + 1) % gameBackgroundSprites.Length;

            yield return StartCoroutine(FadeBackground(nextSprite));
        }
    }

    private IEnumerator FadeBackground(Sprite newSprite)
    {
        float t = 0f;
        float startAlpha = gameBackgroundImage.color.a;

        // Fade out (mevcut alpha -> 0)
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0.1f, t / fadeDuration);
            gameBackgroundImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        gameBackgroundImage.color = new Color(1f, 1f, 1f, 0f);

        // Sprite değiştir
        gameBackgroundImage.sprite = newSprite;

        // Fade in (0 -> maxAlpha)
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0.1f, maxAlpha, t / fadeDuration);
            gameBackgroundImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        gameBackgroundImage.color = new Color(1f, 1f, 1f, maxAlpha);
    }

    // Update is called once per frame
    public void StartGame()
    {
        menuPanel.SetActive(false);
        gamePanel.SetActive(true);
        level.SetActive(true);
    }




    public void WinPanel()
    {
        winPanel.SetActive(true);
    }

    public void NextLevel()
    {
        AdsController.Instance.ShowTransition();
        MenuController.isPlay = true;
        activeLevel++;
        SceneManager.LoadScene(0);
        PlayerPrefs.SetInt("activeLevel", activeLevel);
    }

    public void Reload()
    {
        MenuController.isPlay = true;
        SceneManager.LoadScene(0);
        PlayerPrefs.SetInt("activeLevel", activeLevel);

    }

    public void LevelSkip()
    {

        if (AdsController.Instance.ShowReward(RewardState.AddMoney))
        {
            WinPanel();
        }
        else
        {
            Debug.Log("Fail Skip Level");
        }
    }


    public void MainMenu()
    {
        menuPanel.SetActive(true);
        gamePanel.SetActive(false);
        level.SetActive(false);
    }


    public void CloseMenu(GameObject obj)
    {
        obj.SetActive(false);
    }

    public void OpenMenu(GameObject obj)
    {
        obj.SetActive(true);

    }
}
