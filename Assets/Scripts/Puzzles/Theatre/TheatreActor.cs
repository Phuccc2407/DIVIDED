using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class TheatreActor : MonoBehaviour
{
    public TheatreMotion lift = new TheatreMotion();
    [SerializeField] private Animator animator;
    [SerializeField] private TMP_Text symbolDisplay;
    [SerializeField] private TheatreGestureRig gestureRig;
    private int gesture = -1;
    private PlayableGraph graph;
    private AnimationClipPlayable playable;
    private AnimationClip activeClip;
    private RuntimeAnimatorController savedController;
    private bool controllerDetached;
    private float requestedTime, sampledTime;
    private bool sampleRequested;

    public bool IsValid => lift.IsValid && (animator != null || (gestureRig != null && gestureRig.IsValid)) && symbolDisplay != null;
    public bool SupportsBuiltInGestures => gestureRig != null && gestureRig.IsValid;

    public void ConfigureGesture(string symbol, int index)
    {
        StopAnimation();
        if (symbolDisplay != null) symbolDisplay.text = symbol;
        gesture = index;
        if (gestureRig != null) gestureRig.Sample(index, 0f);
    }

    public void Configure(string symbol, AnimationClip clip)
    {
        StopAnimation();
        if (symbolDisplay != null) symbolDisplay.text = symbol;
        activeClip = clip;
        if (animator == null || clip == null) return;
        if (clip.humanMotion && (animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman))
        {
            Debug.LogError($"Theatre actor {name}: {clip.name} needs a valid Humanoid Avatar on {animator.name}.", this);
            return;
        }
        animator.enabled = true;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.applyRootMotion = false;
        savedController = animator.runtimeAnimatorController;
        animator.runtimeAnimatorController = null;
        controllerDetached = true;
        graph = PlayableGraph.Create(name + " Theatre");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        playable = AnimationClipPlayable.Create(graph, clip);
        playable.SetApplyFootIK(false);
        playable.SetApplyPlayableIK(false);
        AnimationPlayableOutput.Create(graph, "Pose", animator).SetSourcePlayable(playable);
        graph.Play();
        sampledTime = 0f;
        graph.Evaluate(0f);
        Sample(0f);
    }

    public void Sample(float elapsed)
    {
        if (gesture >= 0 && gestureRig != null) { gestureRig.Sample(gesture, elapsed); return; }
        if (!graph.IsValid() || activeClip == null) return;
        requestedTime = Mathf.Max(0f, elapsed);
        sampleRequested = true;
    }

    private void LateUpdate()
    {
        if (!sampleRequested || !graph.IsValid() || activeClip == null) return;
        sampleRequested = false;
        float delta = requestedTime - sampledTime;
        float length = Mathf.Max(0.01f, activeClip.length);
        if (delta <= 0f)
        {
            playable.SetTime(requestedTime % length);
            graph.Evaluate(0f);
        }
        else
        {
            // Advance the graph with a real delta after normal Animator evaluation.
            // Seek from the preceding sample to keep both rooms on the same round time.
            playable.SetTime((requestedTime - delta) % length);
            graph.Evaluate(delta);
            if (playable.GetTime() >= length)
            {
                playable.SetTime(requestedTime % length);
                graph.Evaluate(0f);
            }
        }
        sampledTime = requestedTime;
    }

    public void StopAnimation()
    {
        if (graph.IsValid()) graph.Destroy();
        if (controllerDetached && animator != null) animator.runtimeAnimatorController = savedController;
        controllerDetached = false;
        savedController = null;
        sampleRequested = false;
        activeClip = null;
        gesture = -1;
    }

    private void OnDisable() => StopAnimation();
}
