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

    public bool CanMoveGhosts
    {
        get
        {
            return hud.RoundStarted &&
                !hud.IsGameOver &&
                !playerDeathPaused &&
                !player.IsDying;
        }
    }

    private bool recovering;
    private bool playerDeathPaused;
    private BoxCollider2D playerCollider;

    private void Start()
    {
        playerCollider = player.GetComponent<BoxCollider2D>();

        for (int i = 0; i < ghosts.Length; i = i + 1)
        {
            GhostVisual ghost = ghosts[i];

            ghost.collider = ghost.target.GetComponent<CircleCollider2D>();
            ghost.spawnPosition = ghost.target.transform.position;
            ghost.animator = ghost.target.GetComponent<Animator>();

            if (ghost.animator == null)
            {
                ghost.animator = ghost.target.AddComponent<Animator>();
            }

            ghost.animator.runtimeAnimatorController = ghost.controller;
            ghost.animator.applyRootMotion = false;
            ghost.animator.Play("Normal", 0, 0f);
            ghost.animator.Update(0f);
            ghost.animator.speed = 0f;

            ghost.movement = ghost.target.GetComponent<GhostController>();

            if (ghost.movement == null)
            {
                ghost.movement = ghost.target.AddComponent<GhostController>();
            }

            ghost.movement.Initialize(i + 1, player, this);
        }

        hud.SetGhostTimer(0f);
    }

    private void Update()
    {
        if (playerDeathPaused || !hud.RoundStarted || hud.IsGameOver)
        {
            return;
        }

        AdvanceScaredTimer(Time.deltaTime);
    }

    public bool CheckPlayerContact(Vector3 start, Vector3 end)
    {
        if (playerDeathPaused || !player.CanInteract)
        {
            return false;
        }

        Vector2 motion = end - start;
        Vector2 playerOffset = player.transform.TransformVector(playerCollider.offset);

        float playerWidth =
            playerCollider.size.x * Mathf.Abs(player.transform.lossyScale.x);

        float playerHeight =
            playerCollider.size.y * Mathf.Abs(player.transform.lossyScale.y);

        float playerRadius = Mathf.Min(playerWidth, playerHeight) * 0.5f;

        for (int i = 0; i < ghosts.Length; i = i + 1)
        {
            GhostVisual ghost = ghosts[i];

            if (!ghost.target.activeInHierarchy ||
                ghost.collider == null ||
                !ghost.collider.enabled ||
                ghost.animator.GetCurrentAnimatorStateInfo(0).IsName("Dead"))
            {
                continue;
            }

            Vector2 center = ghost.target.transform.TransformPoint(
                ghost.collider.offset
            );

            float fraction;

            if (motion.sqrMagnitude > 0f)
            {
                Vector2 difference = center - (Vector2)start - playerOffset;
                float dot = Vector2.Dot(difference, motion);

                fraction = Mathf.Clamp01(dot / motion.sqrMagnitude);
            }
            else
            {
                fraction = 0f;
            }

            Vector2 nearest = (Vector2)start + playerOffset + motion * fraction;

            float ghostScale = Mathf.Max(
                Mathf.Abs(ghost.target.transform.lossyScale.x),
                Mathf.Abs(ghost.target.transform.lossyScale.y)
            );

            float radius = playerRadius + ghost.collider.radius * ghostScale;
            Vector2 distance = center - nearest;

            if (distance.sqrMagnitude > radius * radius)
            {
                continue;
            }

            ResolveContact(ghost);

            if (!player.CanInteract)
            {
                return false;
            }
        }

        return true;
    }

    public void CheckGhostContact(GameObject target, Vector3 start, Vector3 end)
    {
        if (!CanMoveGhosts || !player.CanInteract)
        {
            return;
        }

        GhostVisual ghost = null;

        for (int i = 0; i < ghosts.Length; i = i + 1)
        {
            if (ghosts[i].target == target)
            {
                ghost = ghosts[i];
                break;
            }
        }

        if (ghost == null ||
            IsDead(target) ||
            !ghost.collider.enabled ||
            !target.activeInHierarchy)
        {
            return;
        }

        Vector2 offset = target.transform.TransformVector(ghost.collider.offset);

        Vector2 playerCenter = player.transform.TransformPoint(
            playerCollider.offset
        );

        Vector2 motion = end - start;
        float fraction;

        if (motion.sqrMagnitude > 0f)
        {
            Vector2 difference = playerCenter - (Vector2)start - offset;
            float dot = Vector2.Dot(difference, motion);

            fraction = Mathf.Clamp01(dot / motion.sqrMagnitude);
        }
        else
        {
            fraction = 0f;
        }

        float playerWidth =
            playerCollider.size.x * Mathf.Abs(player.transform.lossyScale.x);

        float playerHeight =
            playerCollider.size.y * Mathf.Abs(player.transform.lossyScale.y);

        float playerRadius = Mathf.Min(playerWidth, playerHeight) * 0.5f;

        float ghostScale = Mathf.Max(
            Mathf.Abs(target.transform.lossyScale.x),
            Mathf.Abs(target.transform.lossyScale.y)
        );

        float radius = playerRadius + ghost.collider.radius * ghostScale;

        Vector2 nearest = (Vector2)start + offset + motion * fraction;
        Vector2 distance = playerCenter - nearest;

        if (distance.sqrMagnitude <= radius * radius)
        {
            ResolveContact(ghost);
        }
    }

    private void ResolveContact(GhostVisual ghost)
    {
        if (IsNormal(ghost.target))
        {
            player.BeginDeath();
        }
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
        Animator animator = target.GetComponent<Animator>();
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        return state.IsName("Dead");
    }

    public bool IsNormal(GameObject target)
    {
        Animator animator = target.GetComponent<Animator>();
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        return state.IsName("Normal");
    }

    public void RestoreAtHome(GameObject target)
    {
        GhostVisual ghost = null;

        for (int i = 0; i < ghosts.Length; i = i + 1)
        {
            if (ghosts[i].target == target)
            {
                ghost = ghosts[i];
                break;
            }
        }

        if (ghost == null || !IsDead(target))
        {
            return;
        }

        float distance = Vector3.Distance(
            target.transform.position,
            ghost.spawnPosition
        );

        if (distance > 0.0001f)
        {
            return;
        }

        string state;

        if (RemainingTime <= 0f)
        {
            state = "Normal";
        }
        else if (RemainingTime <= 3f)
        {
            state = "Recovering";
        }
        else
        {
            state = "Scared";
        }

        ghost.animator.Play(state, 0, 0f);
        ghost.animator.Update(0f);

        bool hasDeadGhost = false;

        for (int i = 0; i < ghosts.Length; i = i + 1)
        {
            if (IsDead(ghosts[i].target))
            {
                hasDeadGhost = true;
                break;
            }
        }

        audioManager.SetGhostDeadMusic(hasDeadGhost);
    }

    public void SuspendForPlayerDeath()
    {
        playerDeathPaused = true;

        for (int i = 0; i < ghosts.Length; i = i + 1)
        {
            GhostVisual ghost = ghosts[i];
            ghost.animator.speed = 0f;
        }
    }

    public void ResetAfterPlayerDeath()
    {
        RemainingTime = 0f;
        recovering = false;
        playerDeathPaused = false;

        for (int i = 0; i < ghosts.Length; i = i + 1)
        {
            GhostVisual ghost = ghosts[i];

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
        if (playerDeathPaused || !hud.RoundStarted || hud.IsGameOver)
        {
            return;
        }

        RemainingTime = 10f;
        recovering = false;

        SetLivingGhostState("Scared");

        audioManager.SetGhostScaredMusic(true);
        hud.SetGhostTimer(RemainingTime);
    }

    private void AdvanceScaredTimer(float deltaTime)
    {
        if (RemainingTime <= 0f)
        {
            return;
        }

        RemainingTime = Mathf.Max(0f, RemainingTime - deltaTime);

        if (RemainingTime < 0.0001f)
        {
            RemainingTime = 0f;
        }

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
        for (int i = 0; i < ghosts.Length; i = i + 1)
        {
            GhostVisual ghost = ghosts[i];
            Animator animator = ghost.animator;

            if (animator == null)
            {
                continue;
            }

            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Dead"))
            {
                continue;
            }

            if (animator.IsInTransition(0))
            {
                if (animator.GetNextAnimatorStateInfo(0).IsName("Dead"))
                {
                    continue;
                }
            }

            animator.Play(state, 0, 0f);
            animator.Update(0f);
        }
    }
}
