using PurrNet;
using TMPro;
using UnityEngine;

public class MainGameView : View
{

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<MainGameView>();
    }

    public override void OnHide()
    {

    }

    public override void OnShow()
    {

    }
}
