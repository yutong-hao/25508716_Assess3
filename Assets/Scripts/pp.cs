using UnityEngine;

[DefaultExecutionOrder(-50)]
public class pp : MonoBehaviour
{
    [System.Serializable]
    private class gv
    {
        public GameObject target;
        public RuntimeAnimatorController controller;

        [System.NonSerialized] public Animator anim;
        [System.NonSerialized] public CircleCollider2D collider;
        [System.NonSerialized] public Vector3 spawnPos;
        [System.NonSerialized] public GhostController move;
    }

    [SerializeField] private gv[] gh;
    [SerializeField] private hudc hud;
    [SerializeField] private am audMgr;
    [SerializeField] private PacStudentController pc;

    public float TimeLeft { get; private set; }

    public bool CanMoveGh
    {
        get
        {
            return hud.Started &&
                !hud.GameOver &&
                !deathPaused &&
                !pc.Dying;
        }
    }

    private bool recov;
    private bool deathPaused;
    private BoxCollider2D pcCol;

    private void Start()
    {
        pcCol = pc.GetComponent<BoxCollider2D>();

        for (int i = 0; i < gh.Length; i = i + 1)
        {
            gv ghost = gh[i];

            ghost.collider = ghost.target.GetComponent<CircleCollider2D>();
            ghost.spawnPos = ghost.target.transform.position;
            ghost.anim = ghost.target.GetComponent<Animator>();

            if (ghost.anim == null)
            {
                ghost.anim = ghost.target.AddComponent<Animator>();
            }

            ghost.anim.runtimeAnimatorController = ghost.controller;
            ghost.anim.applyRootMotion = false;
            ghost.anim.Play("Normal", 0, 0f);
            ghost.anim.Update(0f);
            ghost.anim.speed = 0f;

            ghost.move = ghost.target.GetComponent<GhostController>();

            if (ghost.move == null)
            {
                ghost.move = ghost.target.AddComponent<GhostController>();
            }

            ghost.move.Initialize(i + 1, pc, this);
        }

        hud.SetGhostTimer(0f);
    }

    private void Update()
    {
        if (deathPaused || !hud.Started || hud.GameOver)
        {
            return;
        }

        AdvanceScaredTimer(Time.deltaTime);
    }

    public bool CheckPlayerContact(Vector3 start, Vector3 end)
    {
        if (deathPaused || !pc.CanAct)
        {
            return false;
        }

        Vector2 motion = end - start;
        Vector2 pcOffset = pc.transform.TransformVector(pcCol.offset);

        float pcWidth =
            pcCol.size.x * Mathf.Abs(pc.transform.lossyScale.x);

        float pcHeight =
            pcCol.size.y * Mathf.Abs(pc.transform.lossyScale.y);

        float pcRad = Mathf.Min(pcWidth, pcHeight) * 0.5f;

        for (int i = 0; i < gh.Length; i = i + 1)
        {
            gv ghost = gh[i];

            if (!ghost.target.activeInHierarchy ||
                ghost.collider == null ||
                !ghost.collider.enabled ||
                ghost.anim.GetCurrentAnimatorStateInfo(0).IsName("Dead"))
            {
                continue;
            }

            Vector2 center = ghost.target.transform.TransformPoint(
                ghost.collider.offset
            );

            float frac;

            if (motion.sqrMagnitude > 0f)
            {
                Vector2 diff = center - (Vector2)start - pcOffset;
                float dot = Vector2.Dot(diff, motion);

                frac = Mathf.Clamp01(dot / motion.sqrMagnitude);
            }
            else
            {
                frac = 0f;
            }

            Vector2 nearest = (Vector2)start + pcOffset + motion * frac;

            float ghScale = Mathf.Max(
                Mathf.Abs(ghost.target.transform.lossyScale.x),
                Mathf.Abs(ghost.target.transform.lossyScale.y)
            );

            float radius = pcRad + ghost.collider.radius * ghScale;
            Vector2 distance = center - nearest;

            if (distance.sqrMagnitude > radius * radius)
            {
                continue;
            }

            ResolveContact(ghost);

            if (!pc.CanAct)
            {
                return false;
            }
        }

        return true;
    }

    public void CheckGhostContact(GameObject target, Vector3 start, Vector3 end)
    {
        if (!CanMoveGh || !pc.CanAct)
        {
            return;
        }

        gv ghost = null;

        for (int i = 0; i < gh.Length; i = i + 1)
        {
            if (gh[i].target == target)
            {
                ghost = gh[i];
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

        Vector2 pcCenter = pc.transform.TransformPoint(
            pcCol.offset
        );

        Vector2 motion = end - start;
        float frac;

        if (motion.sqrMagnitude > 0f)
        {
            Vector2 diff = pcCenter - (Vector2)start - offset;
            float dot = Vector2.Dot(diff, motion);

            frac = Mathf.Clamp01(dot / motion.sqrMagnitude);
        }
        else
        {
            frac = 0f;
        }

        float pcWidth =
            pcCol.size.x * Mathf.Abs(pc.transform.lossyScale.x);

        float pcHeight =
            pcCol.size.y * Mathf.Abs(pc.transform.lossyScale.y);

        float pcRad = Mathf.Min(pcWidth, pcHeight) * 0.5f;

        float ghScale = Mathf.Max(
            Mathf.Abs(target.transform.lossyScale.x),
            Mathf.Abs(target.transform.lossyScale.y)
        );

        float radius = pcRad + ghost.collider.radius * ghScale;

        Vector2 nearest = (Vector2)start + offset + motion * frac;
        Vector2 distance = pcCenter - nearest;

        if (distance.sqrMagnitude <= radius * radius)
        {
            ResolveContact(ghost);
        }
    }

    private void ResolveContact(gv ghost)
    {
        if (IsNormal(ghost.target))
        {
            pc.BeginDeath();
        }
        else if (!IsDead(ghost.target))
        {
            ghost.anim.Play("Dead", 0, 0f);
            ghost.anim.Update(0f);

            hud.AddScore(300);

            audMgr.Playeatghost();
            audMgr.SetGhostDeadMusic(true);
        }
    }

    public bool IsDead(GameObject target)
    {
        Animator anim = target.GetComponent<Animator>();
        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);

        return state.IsName("Dead");
    }

    public bool IsNormal(GameObject target)
    {
        Animator anim = target.GetComponent<Animator>();
        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);

        return state.IsName("Normal");
    }

    public void RestoreAtHome(GameObject target)
    {
        gv ghost = null;

        for (int i = 0; i < gh.Length; i = i + 1)
        {
            if (gh[i].target == target)
            {
                ghost = gh[i];
                break;
            }
        }

        if (ghost == null || !IsDead(target))
        {
            return;
        }

        float distance = Vector3.Distance(
            target.transform.position,
            ghost.spawnPos
        );

        if (distance > 0.0001f)
        {
            return;
        }

        string state;

        if (TimeLeft <= 0f)
        {
            state = "Normal";
        }
        else if (TimeLeft <= 3f)
        {
            state = "Recovering";
        }
        else
        {
            state = "Scared";
        }

        ghost.anim.Play(state, 0, 0f);
        ghost.anim.Update(0f);

        bool hasDeadGh = false;

        for (int i = 0; i < gh.Length; i = i + 1)
        {
            if (IsDead(gh[i].target))
            {
                hasDeadGh = true;
                break;
            }
        }

        audMgr.SetGhostDeadMusic(hasDeadGh);
    }

    public void SuspendForPlayerDeath()
    {
        deathPaused = true;

        for (int i = 0; i < gh.Length; i = i + 1)
        {
            gv ghost = gh[i];
            ghost.anim.speed = 0f;
        }
    }

    public void ResetAfterPlayerDeath()
    {
        TimeLeft = 0f;
        recov = false;
        deathPaused = false;

        for (int i = 0; i < gh.Length; i = i + 1)
        {
            gv ghost = gh[i];

            ghost.target.transform.position = ghost.spawnPos;
            ghost.move.ResetMovement();

            ghost.anim.speed = 1f;
            ghost.anim.Play("Normal", 0, 0f);
            ghost.anim.Update(0f);
        }

        hud.SetGhostTimer(0f);
        audMgr.SetGhostScaredMusic(false);
        audMgr.SetGhostDeadMusic(false);
    }

    public void Activate()
    {
        if (deathPaused || !hud.Started || hud.GameOver)
        {
            return;
        }

        TimeLeft = 10f;
        recov = false;

        SetLivingGhostState("Scared");

        audMgr.SetGhostScaredMusic(true);
        hud.SetGhostTimer(TimeLeft);
    }

    private void AdvanceScaredTimer(float dt)
    {
        if (TimeLeft <= 0f)
        {
            return;
        }

        TimeLeft = Mathf.Max(0f, TimeLeft - dt);

        if (TimeLeft < 0.0001f)
        {
            TimeLeft = 0f;
        }

        if (TimeLeft <= 0f)
        {
            recov = false;

            SetLivingGhostState("Normal");
            audMgr.SetGhostScaredMusic(false);
        }
        else if (TimeLeft <= 3.0001f && !recov)
        {
            TimeLeft = Mathf.Min(TimeLeft, 3f);
            recov = true;

            SetLivingGhostState("Recovering");
        }

        hud.SetGhostTimer(TimeLeft);
    }

    private void SetLivingGhostState(string state)
    {
        for (int i = 0; i < gh.Length; i = i + 1)
        {
            gv ghost = gh[i];
            Animator anim = ghost.anim;

            if (anim == null)
            {
                continue;
            }

            if (anim.GetCurrentAnimatorStateInfo(0).IsName("Dead"))
            {
                continue;
            }

            if (anim.IsInTransition(0))
            {
                if (anim.GetNextAnimatorStateInfo(0).IsName("Dead"))
                {
                    continue;
                }
            }

            anim.Play(state, 0, 0f);
            anim.Update(0f);
        }
    }
}
