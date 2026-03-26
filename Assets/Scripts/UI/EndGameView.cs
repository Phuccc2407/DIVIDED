using System.Collections;
using PurrNet;
using TMPro;
using UnityEngine;

public class EndGameView : View
{   
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private float fadeDuration = 1f;

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<EndGameView>();
    }

    public void SetWinner(PlayerID winner)
    {
        winnerText.text = $"Player {winner} wins!";
        StartCoroutine(FadeScreen(true));
    }

    private IEnumerator FadeScreen(bool fadeIn)
    {
        float t = 0;
        while (t < 1)
        {
            canvasGroup.alpha = fadeIn ? t / fadeDuration : 1 - t / fadeDuration;

            t += Time.deltaTime;
            yield return null;
        }
    }

    public override void OnHide()
    {
        
    }

    public override void OnShow()
    {
        
    }
}
