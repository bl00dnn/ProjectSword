using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public sealed class SergiusAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip walkingClip;
    [SerializeField] private AnimationClip runClip;
    [SerializeField] private AnimationClip[] additionalClips;
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private Avatar characterAvatar;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string sprintParameter = "Sprint";
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private float blendSharpness = 10f;
    [SerializeField] private bool driveClipsDirectly = true;

    private readonly Dictionary<string, AnimationClip> clipsByName = new Dictionary<string, AnimationClip>();
    private PlayableGraph graph;
    private AnimationMixerPlayable locomotionMixer;
    private AnimationClipPlayable idlePlayable;
    private AnimationClipPlayable walkingPlayable;
    private AnimationClipPlayable runPlayable;
    private AnimationClipPlayable jumpPlayable;
    private AnimationClip idlePlayableClip;
    private AnimationClip walkingPlayableClip;
    private AnimationClip runPlayableClip;
    private AnimationClip jumpClip;
    private int jumpInputIndex = -1;
    private float jumpTimer;
    private float targetMoveAmount;
    private float currentMoveAmount;
    private float targetSprintAmount;
    private float currentSprintAmount;
    private bool configured;
    private bool useAnimatorController;

    public void Configure(Animator targetAnimator, AnimationClip idle, AnimationClip run, AnimationClip[] extraClips, RuntimeAnimatorController controller, Avatar avatar, string parameterName)
    {
        Configure(targetAnimator, idle, run, null, extraClips, controller, avatar, parameterName);
    }

    public void Configure(Animator targetAnimator, AnimationClip idle, AnimationClip walking, AnimationClip run, AnimationClip[] extraClips, RuntimeAnimatorController controller, Avatar avatar, string parameterName)
    {
        animator = targetAnimator;
        idleClip = idle;
        walkingClip = walking;
        runClip = run;
        additionalClips = extraClips;
        animatorController = controller;
        characterAvatar = avatar;
        speedParameter = string.IsNullOrWhiteSpace(parameterName) ? "Speed" : parameterName;

        RegisterClips();

        AssignAnimatorAssets();

        if (driveClipsDirectly && (idleClip != null || walkingClip != null || runClip != null))
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

    public void SetMoveAmount(float amount, bool sprinting)
    {
        SetMoveAmount(amount);
        SetSprinting(sprinting);
    }

    public void SetSprinting(bool sprinting)
    {
        targetSprintAmount = sprinting ? 1f : 0f;
    }

    public void TriggerJump()
    {
        if (animator == null || string.IsNullOrWhiteSpace(jumpTrigger))
        {
            return;
        }

        if (jumpClip != null && graph.IsValid() && locomotionMixer.IsValid() && jumpInputIndex >= 0)
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
        if (!configured && animator != null && driveClipsDirectly && (idleClip != null || walkingClip != null || runClip != null))
        {
            BuildGraph();
        }
        else if (!configured && animator != null && animatorController != null)
        {
            RegisterClips();
            UseAnimatorController();
        }
        else if (!configured && animator != null && (idleClip != null || walkingClip != null || runClip != null))
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
                currentSprintAmount = Mathf.Lerp(currentSprintAmount, targetSprintAmount, 1f - Mathf.Exp(-blendSharpness * Time.deltaTime));
                animator.SetFloat(speedParameter, currentMoveAmount);
                SetAnimatorBool(sprintParameter, currentSprintAmount > 0.5f);
            }

            return;
        }

        currentMoveAmount = Mathf.Lerp(currentMoveAmount, targetMoveAmount, 1f - Mathf.Exp(-blendSharpness * Time.deltaTime));
        currentSprintAmount = Mathf.Lerp(currentSprintAmount, targetSprintAmount, 1f - Mathf.Exp(-blendSharpness * Time.deltaTime));
        LoopPlayable(idlePlayable, idlePlayableClip);
        LoopPlayable(walkingPlayable, walkingPlayableClip);
        LoopPlayable(runPlayable, runPlayableClip);

        if (jumpTimer > 0f && jumpInputIndex >= 0)
        {
            jumpTimer -= Time.deltaTime;
            for (int i = 0; i < locomotionMixer.GetInputCount(); i++)
            {
                locomotionMixer.SetInputWeight(i, i == jumpInputIndex ? 1f : 0f);
            }

            return;
        }

        float runWeight = runPlayable.IsValid() ? currentMoveAmount * currentSprintAmount : 0f;
        float walkingWeight = currentMoveAmount - runWeight;
        locomotionMixer.SetInputWeight(0, 1f - currentMoveAmount);
        locomotionMixer.SetInputWeight(1, walkingWeight);
        if (runPlayable.IsValid())
        {
            locomotionMixer.SetInputWeight(2, runWeight);
        }

        if (jumpInputIndex >= 0)
        {
            locomotionMixer.SetInputWeight(jumpInputIndex, 0f);
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
        if (animator == null || (idleClip == null && walkingClip == null && runClip == null))
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

        AnimationClip fallbackClip = idleClip != null ? idleClip : walkingClip != null ? walkingClip : runClip;
        idlePlayableClip = idleClip != null ? idleClip : fallbackClip;
        walkingPlayableClip = walkingClip != null ? walkingClip : fallbackClip;
        runPlayableClip = runClip;
        idlePlayable = AnimationClipPlayable.Create(graph, idlePlayableClip);
        walkingPlayable = AnimationClipPlayable.Create(graph, walkingPlayableClip);
        jumpClip = FindJumpClip();
        jumpInputIndex = -1;

        idlePlayable.SetApplyFootIK(true);
        walkingPlayable.SetApplyFootIK(true);

        int inputCount = 2;
        bool hasRun = runPlayableClip != null;
        if (hasRun)
        {
            inputCount++;
        }

        if (jumpClip != null)
        {
            jumpInputIndex = inputCount;
            inputCount++;
        }

        locomotionMixer = AnimationMixerPlayable.Create(graph, inputCount);
        graph.Connect(idlePlayable, 0, locomotionMixer, 0);
        graph.Connect(walkingPlayable, 0, locomotionMixer, 1);

        locomotionMixer.SetInputWeight(0, 1f);
        locomotionMixer.SetInputWeight(1, 0f);

        if (hasRun)
        {
            runPlayable = AnimationClipPlayable.Create(graph, runPlayableClip);
            runPlayable.SetApplyFootIK(true);
            graph.Connect(runPlayable, 0, locomotionMixer, 2);
            locomotionMixer.SetInputWeight(2, 0f);
        }

        if (jumpClip != null)
        {
            jumpPlayable = AnimationClipPlayable.Create(graph, jumpClip);
            jumpPlayable.SetApplyFootIK(true);
            graph.Connect(jumpPlayable, 0, locomotionMixer, jumpInputIndex);
            locomotionMixer.SetInputWeight(jumpInputIndex, 0f);
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
        currentSprintAmount = 0f;
        targetSprintAmount = 0f;
        animator.SetFloat(speedParameter, 0f);
        SetAnimatorBool(sprintParameter, false);
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
        RegisterClip(walkingClip);
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

    private void SetAnimatorBool(string parameterName, bool value)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        for (int i = 0; i < animator.parameterCount; i++)
        {
            AnimatorControllerParameter parameter = animator.parameters[i];
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
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
