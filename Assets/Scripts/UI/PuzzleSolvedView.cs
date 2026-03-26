using DG.Tweening;
using PurrNet;
using UnityEngine;

public class PuzzleSolvedView : View
{
    [SerializeField] float fadeDuration = 0.5f;
    [SerializeField] float stayDuration = 2f;

    Tween currentTween;
    
    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<PuzzleSolvedView>();
    }

    public override void OnShow()
    {
        currentTween?.Kill();

        canvasGroup.alpha = 0f;

        currentTween = canvasGroup
            .DOFade(1f, fadeDuration)
            .OnComplete(() =>
            {
                DOVirtual.DelayedCall(stayDuration, () =>
                {
                    canvasGroup.DOFade(0f, fadeDuration);
                });
            });
    }

    public override void OnHide()
    {
        canvasGroup.alpha = 0f;
    }
}
