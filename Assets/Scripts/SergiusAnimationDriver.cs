using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public sealed class SergiusAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip runClip;
    [SerializeField] private AnimationClip[] additionalClips;
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private Avatar characterAvatar;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private float blendSharpness = 10f;
    [SerializeField] private bool driveClipsDirectly = true;

    private readonly Dictionary<string, AnimationClip> clipsByName = new Dictionary<string, AnimationClip>();
    private PlayableGraph graph;
    private AnimationMixerPlayable locomotionMixer;
    private AnimationClipPlayable idlePlayable;
    private AnimationClipPlayable runPlayable;
    private AnimationClipPlayable jumpPlayable;
    private AnimationClip idlePlayableClip;
    private AnimationClip runPlayableClip;
    private AnimationClip jumpClip;
    private float jumpTimer;
    private float targetMoveAmount;
    private float currentMoveAmount;
    private bool configured;
    private bool useAnimatorController;

    public void Configure(Animator targetAnimator, AnimationClip idle, AnimationClip run, AnimationClip[] extraClips, RuntimeAnimatorController controller, Avatar avatar, string parameterName)
    {
        animator = targetAnimator;
        idleClip = idle;
        runClip = run;
        additionalClips = extraClips;
        animatorController = controller;
        characterAvatar = avatar;
        speedParameter = string.IsNullOrWhiteSpace(parameterName) ? "Speed" : parameterName;

        RegisterClips();

        AssignAnimatorAssets();

        if (driveClipsDirectly && (idleClip != null || runClip != null))
        {
            BuildGraph();
            return;
        }

        if (animatorController != null)
        {
            UseAnimatorController();
            return;
        }

        BuildGraph();
    }

    public void SetMoveAmount(float amount)
    {
        targetMoveAmount = Mathf.Clamp01(amount);
    }

    public void TriggerJump()
    {
        if (animator == null || string.IsNullOrWhiteSpace(jumpTrigger))
        {
            return;
        }

        if (jumpClip != null && graph.IsValid() && locomotionMixer.IsValid() && locomotionMixer.GetInputCount() > 2)
        {
            jumpTimer = Mathf.Max(0.1f, jumpClip.length);
            if (jumpPlayable.IsValid())
            {
                jumpPlayable.SetTime(0d);
            }
            return;
        }

        animator.ResetTrigger(jumpTrigger);
        animator.SetTrigger(jumpTrigger);
    }

    public bool TryGetClip(string clipName, out AnimationClip clip)
    {
        return clipsByName.TryGetValue(clipName, out clip);
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        AssignAnimatorAssets();
    }

    private void OnEnable()
    {
        if (!configured && animator != null && driveClipsDirectly && (idleClip != null || runClip != null))
        {
            BuildGraph();
        }
        else if (!configured && animator != null && animatorController != null)
        {
            RegisterClips();
            UseAnimatorController();
        }
        else if (!configured && animator != null && (idleClip != null || runClip != null))
        {
            BuildGraph();
        }
        else if (graph.IsValid())
        {
            graph.Play();
        }
    }

    private void Update()
    {
        if (!configured || !locomotionMixer.IsValid())
        {
            if (useAnimatorController && animator != null)
            {
                currentMoveAmount = Mathf.Lerp(currentMoveAmount, targetMoveAmount, 1f - Mathf.Exp(-blendSharpness * Time.deltaTime));
                animator.SetFloat(speedParameter, currentMoveAmount);
            }

            return;
        }

        currentMoveAmount = Mathf.Lerp(currentMoveAmount, targetMoveAmount, 1f - Mathf.Exp(-blendSharpness * Time.deltaTime));
        LoopPlayable(idlePlayable, idlePlayableClip);
        LoopPlayable(runPlayable, runPlayableClip);

        if (jumpTimer > 0f && locomotionMixer.GetInputCount() > 2)
        {
            jumpTimer -= Time.deltaTime;
            locomotionMixer.SetInputWeight(0, 0f);
            locomotionMixer.SetInputWeight(1, 0f);
            locomotionMixer.SetInputWeight(2, 1f);
            return;
        }

        locomotionMixer.SetInputWeight(0, 1f - currentMoveAmount);
        locomotionMixer.SetInputWeight(1, currentMoveAmount);
        if (locomotionMixer.GetInputCount() > 2)
        {
            locomotionMixer.SetInputWeight(2, 0f);
        }
    }

    private void OnDisable()
    {
        if (graph.IsValid())
        {
            graph.Stop();
        }
    }

    private void OnDestroy()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }

    private void BuildGraph()
    {
        if (animator == null || (idleClip == null && runClip == null))
        {
            return;
        }

        if (graph.IsValid())
        {
            graph.Destroy();
        }

        RegisterClips();
        useAnimatorController = false;
        AssignAnimatorAssets();
        animator.runtimeAnimatorController = null;

        graph = PlayableGraph.Create($"{name} Animation Graph");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        AnimationClip fallbackClip = idleClip != null ? idleClip : runClip;
        idlePlayableClip = idleClip != null ? idleClip : fallbackClip;
        runPlayableClip = runClip != null ? runClip : fallbackClip;
        idlePlayable = AnimationClipPlayable.Create(graph, idlePlayableClip);
        runPlayable = AnimationClipPlayable.Create(graph, runPlayableClip);
        jumpClip = FindJumpClip();

        idlePlayable.SetApplyFootIK(true);
        runPlayable.SetApplyFootIK(true);

        int inputCount = jumpClip != null ? 3 : 2;
        locomotionMixer = AnimationMixerPlayable.Create(graph, inputCount);
        graph.Connect(idlePlayable, 0, locomotionMixer, 0);
        graph.Connect(runPlayable, 0, locomotionMixer, 1);

        locomotionMixer.SetInputWeight(0, 1f);
        locomotionMixer.SetInputWeight(1, 0f);

        if (jumpClip != null)
        {
            jumpPlayable = AnimationClipPlayable.Create(graph, jumpClip);
            jumpPlayable.SetApplyFootIK(true);
            graph.Connect(jumpPlayable, 0, locomotionMixer, 2);
            locomotionMixer.SetInputWeight(2, 0f);
        }

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Sergius Locomotion", animator);
        output.SetSourcePlayable(locomotionMixer);

        configured = true;
        graph.Play();
    }

    private void LoopPlayable(AnimationClipPlayable playable, AnimationClip clip)
    {
        if (!playable.IsValid() || clip == null || clip.length <= 0f)
        {
            return;
        }

        double time = playable.GetTime();
        if (time >= clip.length)
        {
            playable.SetTime(time % clip.length);
        }
    }

    private void UseAnimatorController()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }

        AssignAnimatorAssets();
        useAnimatorController = true;
        configured = true;
        currentMoveAmount = 0f;
        targetMoveAmount = 0f;
        animator.SetFloat(speedParameter, 0f);
    }

    private void AssignAnimatorAssets()
    {
        if (animator == null)
        {
            return;
        }

        if (characterAvatar != null)
        {
            animator.avatar = characterAvatar;
        }

        if (animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.Rebind();
        animator.Update(0f);
    }

    private void RegisterClips()
    {
        clipsByName.Clear();
        RegisterClip(idleClip);
        RegisterClip(runClip);

        if (additionalClips == null)
        {
            return;
        }

        for (int i = 0; i < additionalClips.Length; i++)
        {
            RegisterClip(additionalClips[i]);
        }
    }

    private void RegisterClip(AnimationClip clip)
    {
        if (clip == null || string.IsNullOrEmpty(clip.name))
        {
            return;
        }

        clipsByName[clip.name] = clip;
    }

    private AnimationClip FindJumpClip()
    {
        if (additionalClips == null)
        {
            return null;
        }

        for (int i = 0; i < additionalClips.Length; i++)
        {
            AnimationClip clip = additionalClips[i];
            if (clip != null && clip.name.ToLowerInvariant().Contains("jump"))
            {
                return clip;
            }
        }

        return null;
    }
}
