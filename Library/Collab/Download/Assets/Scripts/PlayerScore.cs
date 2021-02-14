using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerScore: MonoBehaviour  {
    // Properties
    public int score = 0;
    public const int minScore = 0;

    void Update() {
        this.score++;
        var t = this.gameObject.GetComponent<Text>();
        t.text = this.score.ToString();
    }

    public void LoseScore(int amount)
    {
        /*
        if (!isServer)
            return;
        */
        score -= amount;
        if (score <= minScore)
        {
            score = minScore;
        }
    }

}
