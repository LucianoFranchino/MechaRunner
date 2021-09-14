using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public Text scoreText, secondaryScore;
    public Text hiScoreText;
    public float scoreCount;
    public float pointsPerSecond;
    public bool scoreIncreasing;
    public float multiplier = 1f;
    public float timer;
    public float baseTimeMultiplier = 5f, multBase = 2f;

    void Start()
    {
        if (PlayerPrefs.HasKey("highscore"))
            hiScoreText.text = "Highscore: " + Mathf.Round(PlayerPrefs.GetFloat("highscore"));
    }

    void Update()
    {
        if (timer > 0) timer -= Time.deltaTime;
        else if (timer <= 0 && multiplier > 1) multiplier = 1;

        if (scoreIncreasing)
            scoreCount += (pointsPerSecond * multiplier) * Time.deltaTime;
        scoreText.text = secondaryScore.text = "Score: " + Mathf.Round(scoreCount);
    }

    public void PUPMult()
    {
        if (timer <= 0) timer += baseTimeMultiplier;
        else timer = baseTimeMultiplier;
        multiplier = multBase;
    }

    public void Save()
    {
        if (PlayerPrefs.HasKey("highscore"))
        {
            if (scoreCount > PlayerPrefs.GetFloat("highscore"))
                PlayerPrefs.SetFloat("highscore", scoreCount);
        }
        else
            PlayerPrefs.SetFloat("highscore", scoreCount);
    }
}
