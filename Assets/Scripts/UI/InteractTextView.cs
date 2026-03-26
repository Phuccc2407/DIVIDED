using PurrNet;
using TMPro;
using UnityEngine;

public class InteractTextView : View
{
    [SerializeField] private TMP_Text text;

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<InteractTextView>();
    }

    public override void OnShow()
    {
        canvasGroup.alpha = 1f;
    }

    public override void OnHide()
    {
        canvasGroup.alpha = 0f;
    }

    public void SetText(string content)
    {
        text.text = content;
    }
}