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
        [System.NonSerialized] public GhostController movement;
    }

    [SerializeField] private GhostVisual[] ghosts;
    [SerializeField] private InGameHUDController hud;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private PacStudentController player;

    public float RemainingTime { get; private set; }
    public bool CanMoveGhosts => hud.RoundStarted && !hud.IsGameOver && !playerDeathPaused && !player.IsDying;
    private bool recovering;
    private bool playerDeathPaused;
    private BoxCollider2D playerCollider;

    private void Start()
    {
        playerCollider = player.GetComponent<BoxCollider2D>();
        for (int index = 0; index < ghosts.Length; index++)
        {
            GhostVisual ghost = ghosts[index];
            ghost.collider = ghost.target.GetComponent<CircleCollider2D>();
            ghost.spawnPosition = ghost.target.transform.position;
            ghost.animator = ghost.target.GetComponent<Animator>();
            if (ghost.animator == null) ghost.animator = ghost.target.AddComponent<Animator>();
            ghost.animator.runtimeAnimatorController = ghost.controller;
            ghost.animator.applyRootMotion = false;
            ghost.animator.Play("Normal", 0, 0f);
            ghost.animator.Update(0f);
            ghost.animator.speed = 0f;
            ghost.movement = ghost.target.GetComponent<GhostController>();
            if (ghost.movement == null) ghost.movement = ghost.target.AddComponent<GhostController>();
            ghost.movement.Initialize(index + 1, player, this);
        }
        hud.SetGhostTimer(0f);
    }

    private void Update()
    {
        if (playerDeathPaused || !hud.RoundStarted || hud.IsGameOver) return;
        AdvanceScaredTimer(Time.deltaTime);
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
            ResolveContact(ghost);
            if (!player.CanInteract) return false;
        }
        return true;
    }

    public void CheckGhostContact(GameObject target, Vector3 start, Vector3 end)
    {
        if (!CanMoveGhosts || !player.CanInteract) return;
        GhostVisual ghost = System.Array.Find(ghosts, candidate => candidate.target == target);
        if (ghost == null || IsDead(target) || !ghost.collider.enabled || !target.activeInHierarchy) return;
        Vector2 offset = target.transform.TransformVector(ghost.collider.offset);
        Vector2 playerCenter = player.transform.TransformPoint(playerCollider.offset);
        Vector2 motion = end - start;
        float fraction = motion.sqrMagnitude > 0f
            ? Mathf.Clamp01(Vector2.Dot(playerCenter - (Vector2)start - offset, motion) / motion.sqrMagnitude) : 0f;
        float radius = Mathf.Min(playerCollider.size.x * Mathf.Abs(player.transform.lossyScale.x),
            playerCollider.size.y * Mathf.Abs(player.transform.lossyScale.y)) * 0.5f +
            ghost.collider.radius * Mathf.Max(Mathf.Abs(target.transform.lossyScale.x), Mathf.Abs(target.transform.lossyScale.y));
        if ((playerCenter - ((Vector2)start + offset + motion * fraction)).sqrMagnitude <= radius * radius)
            ResolveContact(ghost);
    }

    private void ResolveContact(GhostVisual ghost)
    {
        if (IsNormal(ghost.target)) player.BeginDeath();
        else if (!IsDead(ghost.target))
        {
            ghost.animator.Play("Dead", 0, 0f);
            ghost.animator.Update(0f);
            hud.AddScore(300);
            audioManager.Playeatghost();
            audioManager.SetGhostDeadMusic(true);
        }
    }

    public bool IsDead(GameObject target)
    {
        return target.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Dead");
    }

    public bool IsNormal(GameObject target)
    {
        return target.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Normal");
    }

    public void RestoreAtHome(GameObject target)
    {
        GhostVisual ghost = System.Array.Find(ghosts, candidate => candidate.target == target);
        if (ghost == null || !IsDead(target) || Vector3.Distance(target.transform.position, ghost.spawnPosition) > 0.0001f) return;
        ghost.animator.Play(RemainingTime <= 0f ? "Normal" : RemainingTime <= 3f ? "Recovering" : "Scared", 0, 0f);
        ghost.animator.Update(0f);
        audioManager.SetGhostDeadMusic(System.Array.Exists(ghosts, candidate => IsDead(candidate.target)));
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
            ghost.target.transform.position = ghost.spawnPosition;
            ghost.movement.ResetMovement();
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
        if (playerDeathPaused || !hud.RoundStarted || hud.IsGameOver) return;
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
