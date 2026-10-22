using UnityEngine;

[DefaultExecutionOrder(-50)]
public class PowerPelletController : MonoBehaviour
{
    [System.Serializable]
    private class GhostVisual
    {
        public GameObject target;
        public RuntimeAnimatorController controller;
        [System.NonSerialized] public Animator animator;
    }

    [SerializeField] private GhostVisual[] ghosts;
    [SerializeField] private InGameHUDController hud;
    [SerializeField] private AudioManager audioManager;

    public float RemainingTime { get; private set; }
    private bool recovering;

    private void Start()
    {
        foreach (GhostVisual ghost in ghosts)
        {
            ghost.animator = ghost.target.GetComponent<Animator>();
            if (ghost.animator == null) ghost.animator = ghost.target.AddComponent<Animator>();
            ghost.animator.runtimeAnimatorController = ghost.controller;
            ghost.animator.applyRootMotion = false;
            ghost.animator.Play("Normal", 0, 0f);
            ghost.animator.Update(0f);
        }
        hud.SetGhostTimer(0f);
    }

    private void Update()
    {
        AdvanceScaredTimer(Time.deltaTime);
    }

    public void Activate()
    {
        RemainingTime = 10f;
        recovering = false;
        SetLivingGhostState("Scared");
        audioManager.SetGhostScaredMusic(true);
        hud.SetGhostTimer(RemainingTime);
    }

    private void AdvanceScaredTimer(float deltaTime)
    {
        if (RemainingTime <= 0f) return;
        RemainingTime = Mathf.Max(0f, RemainingTime - deltaTime);
        if (RemainingTime < 0.0001f) RemainingTime = 0f;
        if (RemainingTime <= 0f)
        {
            recovering = false;
            SetLivingGhostState("Normal");
            audioManager.SetGhostScaredMusic(false);
        }
        else if (RemainingTime <= 3.0001f && !recovering)
        {
            RemainingTime = Mathf.Min(RemainingTime, 3f);
            recovering = true;
            SetLivingGhostState("Recovering");
        }
        hud.SetGhostTimer(RemainingTime);
    }

    private void SetLivingGhostState(string state)
    {
        foreach (GhostVisual ghost in ghosts)
        {
            Animator animator = ghost.animator;
            if (animator == null || animator.GetCurrentAnimatorStateInfo(0).IsName("Dead") ||
                (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("Dead"))) continue;
            animator.Play(state, 0, 0f);
            animator.Update(0f);
        }
    }
}
