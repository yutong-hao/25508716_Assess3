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
        [System.NonSerialized] public CircleCollider2D collider;
        [System.NonSerialized] public Vector3 spawnPosition;
        [System.NonSerialized] public float deadRemaining;
    }

    [SerializeField] private GhostVisual[] ghosts;
    [SerializeField] private InGameHUDController hud;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private PacStudentController player;

    public float RemainingTime { get; private set; }
    private bool recovering;
    private bool playerDeathPaused;
    private BoxCollider2D playerCollider;

    private void Start()
    {
        playerCollider = player.GetComponent<BoxCollider2D>();
        foreach (GhostVisual ghost in ghosts)
        {
            ghost.collider = ghost.target.GetComponent<CircleCollider2D>();
            ghost.spawnPosition = ghost.target.transform.position;
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
        if (playerDeathPaused || !hud.RoundStarted) return;
        AdvanceScaredTimer(Time.deltaTime);
        AdvanceDeadGhosts(Time.deltaTime);
    }

    public bool CheckPlayerContact(Vector3 start, Vector3 end)
    {
        if (playerDeathPaused || !player.CanInteract) return false;
        Vector2 motion = end - start;
        Vector2 playerOffset = player.transform.TransformVector(playerCollider.offset);
        float playerRadius = Mathf.Min(playerCollider.size.x * Mathf.Abs(player.transform.lossyScale.x),
            playerCollider.size.y * Mathf.Abs(player.transform.lossyScale.y)) * 0.5f;
        foreach (GhostVisual ghost in ghosts)
        {
            if (!ghost.target.activeInHierarchy || ghost.collider == null || !ghost.collider.enabled ||
                ghost.animator.GetCurrentAnimatorStateInfo(0).IsName("Dead")) continue;
            Vector2 center = ghost.target.transform.TransformPoint(ghost.collider.offset);
            float fraction = motion.sqrMagnitude > 0f
                ? Mathf.Clamp01(Vector2.Dot(center - (Vector2)start - playerOffset, motion) / motion.sqrMagnitude) : 0f;
            Vector2 nearest = (Vector2)start + playerOffset + motion * fraction;
            float radius = playerRadius + ghost.collider.radius *
                Mathf.Max(Mathf.Abs(ghost.target.transform.lossyScale.x), Mathf.Abs(ghost.target.transform.lossyScale.y));
            if ((center - nearest).sqrMagnitude > radius * radius) continue;
            if (ghost.animator.GetCurrentAnimatorStateInfo(0).IsName("Normal"))
            {
                player.BeginDeath();
                return false;
            }
            if (ghost.animator.GetCurrentAnimatorStateInfo(0).IsName("Scared") ||
                ghost.animator.GetCurrentAnimatorStateInfo(0).IsName("Recovering"))
            {
                ghost.deadRemaining = 3f;
                ghost.animator.Play("Dead", 0, 0f);
                ghost.animator.Update(0f);
                hud.AddScore(300);
                audioManager.Playeatghost();
                audioManager.SetGhostDeadMusic(true);
            }
        }
        return true;
    }

    private void AdvanceDeadGhosts(float deltaTime)
    {
        bool restored = false;
        bool anyDead = false;
        foreach (GhostVisual ghost in ghosts)
        {
            if (ghost.deadRemaining <= 0f) continue;
            ghost.deadRemaining = Mathf.Max(0f, ghost.deadRemaining - deltaTime);
            if (ghost.deadRemaining < 0.0001f)
            {
                ghost.deadRemaining = 0f;
                ghost.animator.Play(RemainingTime <= 0f ? "Normal" : RemainingTime <= 3f ? "Recovering" : "Scared", 0, 0f);
                ghost.animator.Update(0f);
                restored = true;
            }
            else anyDead = true;
        }
        if (restored) audioManager.SetGhostDeadMusic(anyDead);
    }

    public void SuspendForPlayerDeath()
    {
        playerDeathPaused = true;
        foreach (GhostVisual ghost in ghosts) ghost.animator.speed = 0f;
    }

    public void ResetAfterPlayerDeath()
    {
        RemainingTime = 0f;
        recovering = false;
        playerDeathPaused = false;
        foreach (GhostVisual ghost in ghosts)
        {
            ghost.deadRemaining = 0f;
            ghost.target.transform.position = ghost.spawnPosition;
            ghost.animator.speed = 1f;
            ghost.animator.Play("Normal", 0, 0f);
            ghost.animator.Update(0f);
        }
        hud.SetGhostTimer(0f);
        audioManager.SetGhostScaredMusic(false);
        audioManager.SetGhostDeadMusic(false);
    }

    public void Activate()
    {
        if (playerDeathPaused || !hud.RoundStarted) return;
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
