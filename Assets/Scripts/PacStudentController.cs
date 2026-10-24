using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [SerializeField] private Transform gridOrg;
    [SerializeField, Min(0.001f)] private float cellSz = 0.125f;
    [SerializeField, Min(0.01f)] private float tileSpd = 5f;
    [SerializeField] private Transform pelRoot;
    [SerializeField] private AudioClip moveSfx;
    [SerializeField] private AudioClip pelMoveSfx;
    [SerializeField] private ParticleSystem moveFxPrefab;
    [SerializeField] private hudc hud;
    [SerializeField] private AudioClip wallSfx;
    [SerializeField] private Transform ppRoot;
    [SerializeField] private pp ppc;
    [SerializeField] private am audMgr;
    [SerializeField] private bool jumpOn;

    public int TpVer { get; private set; }
    public bool Dying { get; private set; }
    public bool Jumping { get; private set; }

    public bool CanAct
    {
        get
        {
            if (!isActiveAndEnabled || Dying)
            {
                return false;
            }

            if (hud == null)
            {
                return true;
            }

            return hud.Started && !hud.GameOver && hud.Lives > 0;
        }
    }

    public float MoveSpd
    {
        get
        {
            Vector3 start = CellToWorld(Vector2Int.zero);
            Vector3 end = CellToWorld(Vector2Int.right);
            float distance = Vector3.Distance(start, end);

            return tileSpd * distance;
        }
    }

    public Vector2Int WorldToCell(Vector3 pos)
    {
        Vector3 local = gridOrg.InverseTransformPoint(pos);

        int x = Mathf.RoundToInt(local.x / cellSz);
        int y = Mathf.RoundToInt(-local.y / cellSz);

        return new Vector2Int(x, y);
    }

    private readonly int[,] lvlMap =
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0}
    };

    private Vector2Int lastInput;
    private Vector2Int currentInput;
    private Vector2Int curCell;
    private Vector2Int targetCell;
    private Vector3 tweenStart;
    private Vector3 tweenEnd;
    private float tweenT;
    private float tweenDur;
    private float gridZ;
    private bool lerping;
    private Animator anim;
    private AudioSource moveAud;
    private ParticleSystem moveFx;
    private SpriteRenderer[] pellets;
    private SpriteRenderer[] pPellets;
    private string moveState = "right";
    private bool feedbackOn;
    private AudioSource hitAud;
    private ParticleSystem wallFx;
    private Vector2Int blockDir;
    private Vector2Int spawnCell;
    private ParticleSystem deathFx;
    private float deathT;
    private float deathDur;
    private float jumpT;
    private SpriteRenderer spr;
    private SpriteRenderer jumpSpr;

    private void Start()
    {
        if (gridOrg == null || cellSz <= 0f || tileSpd <= 0f)
        {
            Debug.LogError(
                "PacStudent needs a grid origin, positive cell size and positive movement speed.",
                this
            );

            enabled = false;
            return;
        }

        Vector3 localPos = gridOrg.InverseTransformPoint(transform.position);
        gridZ = localPos.z;

        int x = Mathf.RoundToInt(localPos.x / cellSz);
        int y = Mathf.RoundToInt(-localPos.y / cellSz);

        curCell = new Vector2Int(x, y);

        if (!IsWalkable(curCell))
        {
            Debug.LogError("PacStudent must start on a walkable grid cell.", this);
            enabled = false;
            return;
        }

        transform.position = CellToWorld(curCell);
        spawnCell = curCell;

        anim = GetComponent<Animator>();

        if (anim != null)
        {
            anim.SetBool("RouteControlled", true);
            anim.Play("right", 0, 0f);
            anim.Update(0f);
            anim.speed = 0f;
        }

        if (jumpOn)
        {
            spr = GetComponent<SpriteRenderer>();

            GameObject obj = new GameObject("JumpSprite");
            obj.transform.SetParent(transform, false);
            jumpSpr = obj.AddComponent<SpriteRenderer>();
            jumpSpr.sharedMaterial = spr.sharedMaterial;
            jumpSpr.sortingLayerID = spr.sortingLayerID;
            jumpSpr.sortingOrder = spr.sortingOrder + 1;
            jumpSpr.enabled = false;
        }

        moveAud = GetComponent<AudioSource>();

        hitAud = gameObject.AddComponent<AudioSource>();
        hitAud.playOnAwake = false;
        hitAud.spatialBlend = 0f;

        if (moveAud != null)
        {
            moveAud.Stop();
            moveAud.playOnAwake = false;
            moveAud.loop = true;
            moveAud.spatialBlend = 0f;
        }

        if (pelRoot != null)
        {
            pellets = pelRoot.GetComponentsInChildren<SpriteRenderer>(true);
        }
        else
        {
            pellets = new SpriteRenderer[0];
        }

        if (ppRoot != null)
        {
            pPellets = ppRoot.GetComponentsInChildren<SpriteRenderer>(true);
        }
        else
        {
            pPellets = new SpriteRenderer[0];
        }

        if (hud != null)
        {
            int pelCount = 0;

            for (int i = 0; i < pellets.Length; i = i + 1)
            {
                if (pellets[i].enabled && pellets[i].gameObject.activeInHierarchy)
                {
                    pelCount = pelCount + 1;
                }
            }

            for (int i = 0; i < pPellets.Length; i = i + 1)
            {
                if (pPellets[i].enabled && pPellets[i].gameObject.activeInHierarchy)
                {
                    pelCount = pelCount + 1;
                }
            }

            hud.RegisterPellets(pelCount);
        }

        if (moveFxPrefab != null)
        {
            moveFx = Instantiate(moveFxPrefab, transform);
            moveFx.name = "MovementDust";
            moveFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            wallFx = Instantiate(moveFxPrefab, transform);
            wallFx.name = "WallCollisionDust";
            wallFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = wallFx.main;
            main.loop = false;

            ParticleSystem.EmissionModule emission = wallFx.emission;
            emission.enabled = false;

            deathFx = Instantiate(moveFxPrefab, transform);
            deathFx.name = "PlayerDeathParticles";
            deathFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule deathMain = deathFx.main;
            deathMain.loop = false;

            ParticleSystem.EmissionModule deathEmission = deathFx.emission;
            deathEmission.enabled = false;
        }
    }

    private void Update()
    {
        if (Dying)
        {
            AdvanceDeath(Time.deltaTime);
            return;
        }

        if (!CanAct)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            lastInput = Vector2Int.down;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            lastInput = Vector2Int.left;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            lastInput = Vector2Int.up;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            lastInput = Vector2Int.right;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            BeginJump();
        }

        AdvanceJump(Time.deltaTime);
        AdvanceMovement(Time.deltaTime);
    }

    private void BeginJump()
    {
        if (!jumpOn)
        {
            return;
        }

        if (Jumping)
        {
            return;
        }

        if (!CanAct)
        {
            return;
        }

        Jumping = true;
        jumpT = 0f;
    }

    private void AdvanceJump(float dt)
    {
        if (Jumping)
        {
            jumpT = jumpT + dt;

            if (jumpT >= 0.6f)
            {
                ResetJump();
            }
        }
    }

    private void LateUpdate()
    {
        if (jumpSpr == null)
        {
            return;
        }

        if (Jumping)
        {
            spr.forceRenderingOff = true;
            jumpSpr.enabled = true;

            jumpSpr.sprite = spr.sprite;
            jumpSpr.color = spr.color;
            jumpSpr.flipX = spr.flipX;
            jumpSpr.flipY = spr.flipY;

            float time = jumpT / 0.6f;
            float angle = time * Mathf.PI;
            float height = Mathf.Sin(angle);
            height = height * cellSz * 1.5f;

            Vector3 offset = new Vector3(0f, height, 0f);
            offset = gridOrg.TransformVector(offset);
            jumpSpr.transform.position = transform.position + offset;
        }
        else
        {
            spr.forceRenderingOff = false;
            jumpSpr.enabled = false;
        }
    }

    private void ResetJump()
    {
        Jumping = false;
        jumpT = 0f;

        if (spr != null)
        {
            spr.forceRenderingOff = false;
        }

        if (jumpSpr != null)
        {
            jumpSpr.enabled = false;
        }
    }

    private void AdvanceMovement(float dt)
    {
        if (!CanAct)
        {
            return;
        }

        if (ppc != null)
        {
            bool canCont = ppc.CheckPlayerContact(
                transform.position,
                transform.position
            );

            if (!canCont)
            {
                return;
            }
        }

        float timeLeft = dt;

        while (timeLeft > 0f)
        {
            if (!lerping && !TryBeginMove())
            {
                StopMovementFeedback();
                PlayWallFeedback();
                return;
            }

            if (!feedbackOn)
            {
                PlayMovementFeedback();
            }

            float stepT = Mathf.Min(
                timeLeft,
                tweenDur - tweenT
            );

            tweenT = tweenT + stepT;
            timeLeft = timeLeft - stepT;

            Vector3 prevPos = transform.position;

            transform.position = Vector3.Lerp(
                tweenStart,
                tweenEnd,
                tweenT / tweenDur
            );

            if (tweenT >= tweenDur)
            {
                transform.position = tweenEnd;
                curCell = targetCell;
                lerping = false;

                CollectPellet();
            }

            if (ppc != null)
            {
                bool canCont = ppc.CheckPlayerContact(
                    prevPos,
                    transform.position
                );

                if (!canCont)
                {
                    return;
                }
            }

            if (!lerping)
            {
                TeleportAtTunnelEnd();
            }
        }

        if (!lerping && !CanMove(lastInput) && !CanMove(currentInput))
        {
            StopMovementFeedback();
            PlayWallFeedback();
        }
    }

    private bool TryBeginMove()
    {
        if (CanMove(lastInput))
        {
            currentInput = lastInput;
        }
        else if (!CanMove(currentInput))
        {
            return false;
        }

        targetCell = curCell + currentInput;
        blockDir = Vector2Int.zero;

        CollectPellet();

        if (!CanAct)
        {
            return false;
        }

        tweenStart = CellToWorld(curCell);
        tweenEnd = CellToWorld(targetCell);
        tweenT = 0f;
        tweenDur = 1f / tileSpd;
        lerping = true;

        PlayMovementFeedback();

        return true;
    }

    public void BeginDeath()
    {
        if (!CanAct)
        {
            return;
        }

        ResetJump();
        Dying = true;

        hud.LoseLife();
        StopMovementFeedback();
        hitAud.Stop();

        if (wallFx != null)
        {
            wallFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        lerping = false;
        lastInput = Vector2Int.zero;
        currentInput = Vector2Int.zero;
        blockDir = Vector2Int.zero;

        ppc.SuspendForPlayerDeath();

        anim.speed = 1f;
        anim.Play("dead", 0, 0f);
        anim.Update(0f);

        float animDur = anim.GetCurrentAnimatorStateInfo(0).length;

        deathDur = Mathf.Max(
            animDur,
            audMgr.dead.clip.length
        );

        anim.speed = animDur / deathDur;
        deathT = 0f;

        audMgr.BeginPlayerDeath();

        if (deathFx != null)
        {
            deathFx.transform.localPosition = Vector3.zero;

            ParticleSystem.MainModule main = deathFx.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(
                0.35f,
                deathDur * 0.8f
            );

            deathFx.Play();
            deathFx.Emit(18);
        }
    }

    private void AdvanceDeath(float dt)
    {
        deathT = deathT + dt;

        if (deathT < deathDur)
        {
            return;
        }

        anim.speed = 0f;
        Dying = false;

        if (hud.Lives == 0 || hud.GameOver)
        {
            return;
        }

        curCell = spawnCell;
        targetCell = spawnCell;
        transform.position = CellToWorld(spawnCell);

        TpVer = TpVer + 1;

        tweenT = 0f;
        lastInput = Vector2Int.zero;
        currentInput = Vector2Int.zero;
        moveState = "right";

        anim.Play("right", 0, 0f);
        anim.Update(0f);

        if (deathFx != null)
        {
            deathFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        ppc.ResetAfterPlayerDeath();
        audMgr.EndPlayerDeath();
    }

    private bool CanMove(Vector2Int dir)
    {
        if (dir == Vector2Int.zero)
        {
            return false;
        }

        return IsWalkable(curCell + dir);
    }

    private void CollectPellet()
    {
        if (!CanAct)
        {
            return;
        }

        CollectPowerPellet();

        if (pellets == null)
        {
            return;
        }

        for (int i = 0; i < pellets.Length; i = i + 1)
        {
            SpriteRenderer pellet = pellets[i];

            if (pellet == null || !pellet.enabled || !pellet.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 diff = pellet.transform.position - transform.position;

            if (diff.sqrMagnitude >= 0.000001f)
            {
                continue;
            }

            pellet.gameObject.SetActive(false);
            Destroy(pellet.gameObject);

            if (hud != null)
            {
                hud.AddScore(10);
                hud.PelletCollected();
            }
        }
    }

    private void CollectPowerPellet()
    {
        if (!CanAct)
        {
            return;
        }

        if (pPellets == null || ppc == null)
        {
            return;
        }

        for (int i = 0; i < pPellets.Length; i = i + 1)
        {
            SpriteRenderer pellet = pPellets[i];

            if (pellet == null || !pellet.enabled || !pellet.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 diff = pellet.transform.position - transform.position;

            if (diff.sqrMagnitude >= 0.000001f)
            {
                continue;
            }

            pellet.gameObject.SetActive(false);
            Destroy(pellet.gameObject);

            if (hud != null)
            {
                hud.AddScore(50);
                hud.PelletCollected();
            }

            ppc.Activate();
        }
    }

    private void TeleportAtTunnelEnd()
    {
        int lastCol = lvlMap.GetLength(1) * 2 - 1;

        if (curCell.y != lvlMap.GetLength(0) - 1)
        {
            return;
        }

        if (curCell.x == 0 && currentInput == Vector2Int.left)
        {
            curCell.x = lastCol;
        }
        else if (curCell.x == lastCol && currentInput == Vector2Int.right)
        {
            curCell.x = 0;
        }
        else
        {
            return;
        }

        transform.position = CellToWorld(curCell);
        TpVer = TpVer + 1;

        if (moveFx != null)
        {
            moveFx.Clear(true);
        }
    }

    private void PlayWallFeedback()
    {
        Vector2Int dir;

        if (currentInput != Vector2Int.zero)
        {
            dir = currentInput;
        }
        else
        {
            dir = lastInput;
        }

        if (dir == Vector2Int.zero || dir == blockDir)
        {
            return;
        }

        blockDir = dir;

        if (hitAud != null && wallSfx != null)
        {
            hitAud.PlayOneShot(wallSfx);
        }

        if (wallFx != null)
        {
            Vector3 offset = new Vector3(dir.x, -dir.y, 0f);
            offset = offset * cellSz * 0.5f;

            wallFx.transform.position =
                CellToWorld(curCell) + gridOrg.TransformVector(offset);

            wallFx.Play();
            wallFx.Emit(8);
        }
    }

    private void PlayMovementFeedback()
    {
        string nextState;

        if (currentInput.x != 0)
        {
            if (currentInput.x > 0)
            {
                nextState = "right";
            }
            else
            {
                nextState = "left";
            }
        }
        else
        {
            if (currentInput.y < 0)
            {
                nextState = "back";
            }
            else
            {
                nextState = "player-walk";
            }
        }

        if (anim != null)
        {
            if (!feedbackOn || moveState != nextState)
            {
                anim.Play(nextState, 0, 0f);
            }

            anim.speed = 1f;
        }

        moveState = nextState;

        if (moveAud != null)
        {
            AudioClip nextSfx;

            if (HasPelletAt(tweenEnd))
            {
                nextSfx = pelMoveSfx;
            }
            else
            {
                nextSfx = moveSfx;
            }

            if (moveAud.clip != nextSfx)
            {
                moveAud.Stop();
                moveAud.clip = nextSfx;
            }

            if (nextSfx != null && !moveAud.isPlaying)
            {
                moveAud.Play();
            }
        }

        if (moveFx != null)
        {
            float x = -currentInput.x * 0.3f;
            float y = currentInput.y * 0.3f - 0.42f;

            moveFx.transform.localPosition = new Vector3(x, y, 0f);

            if (!moveFx.isPlaying)
            {
                moveFx.Play();
            }
        }

        feedbackOn = true;
    }

    private bool HasPelletAt(Vector3 pos)
    {
        if (pellets == null)
        {
            return false;
        }

        for (int i = 0; i < pellets.Length; i = i + 1)
        {
            SpriteRenderer pellet = pellets[i];

            if (pellet != null && pellet.enabled && pellet.gameObject.activeInHierarchy)
            {
                Vector3 diff = pellet.transform.position - pos;

                if (diff.sqrMagnitude < 0.000001f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void StopMovementFeedback()
    {
        if (anim != null)
        {
            anim.speed = 0f;
        }

        if (moveAud != null && moveAud.isPlaying)
        {
            moveAud.Stop();
        }

        if (moveFx != null)
        {
            if (moveFx.isPlaying || moveFx.particleCount > 0)
            {
                moveFx.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }
        }

        feedbackOn = false;
    }

    public void StopForGameOver()
    {
        ResetJump();
        lerping = false;
        lastInput = Vector2Int.zero;
        currentInput = Vector2Int.zero;

        StopMovementFeedback();

        if (hitAud != null)
        {
            hitAud.Stop();
        }

        if (wallFx != null)
        {
            wallFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnDisable()
    {
        ResetJump();
        StopMovementFeedback();

        if (hitAud != null)
        {
            hitAud.Stop();
        }

        if (wallFx != null)
        {
            wallFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public bool IsWalkable(Vector2Int cell)
    {
        int width = lvlMap.GetLength(1) * 2;
        int height = lvlMap.GetLength(0) * 2 - 1;

        if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
        {
            return false;
        }

        int column = Mathf.Min(cell.x, width - 1 - cell.x);
        int row = Mathf.Min(cell.y, height - 1 - cell.y);
        int tile = lvlMap[row, column];

        if (tile == 0 || tile == 5 || tile == 6)
        {
            return true;
        }

        return false;
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        float x = cell.x * cellSz;
        float y = -cell.y * cellSz;

        Vector3 localPos = new Vector3(x, y, gridZ);

        return gridOrg.TransformPoint(localPos);
    }
}
