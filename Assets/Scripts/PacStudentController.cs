using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [SerializeField] private Transform gridOrigin;
    [SerializeField, Min(0.001f)] private float cellSize = 0.125f;
    [SerializeField, Min(0.01f)] private float tilesPerSecond = 5f;
    [SerializeField] private Transform pelletsRoot;
    [SerializeField] private AudioClip movementClip;
    [SerializeField] private AudioClip pelletMovementClip;
    [SerializeField] private ParticleSystem movementDustPrefab;
    [SerializeField] private InGameHUDController hud;
    [SerializeField] private AudioClip wallCollisionClip;

    public int TeleportVersion { get; private set; }

    private readonly int[,] levelMap =
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
    private Vector2Int currentCell;
    private Vector2Int targetCell;
    private Vector3 tweenStart;
    private Vector3 tweenEnd;
    private float tweenElapsed;
    private float tweenDuration;
    private float gridDepth;
    private bool isLerping;
    private Animator animator;
    private AudioSource movementAudio;
    private ParticleSystem movementDust;
    private SpriteRenderer[] pellets;
    private string movementState = "right";
    private bool feedbackPlaying;
    private AudioSource collisionAudio;
    private ParticleSystem wallDust;
    private Vector2Int blockedDirection;

    private void Start()
    {
        if (gridOrigin == null || cellSize <= 0f || tilesPerSecond <= 0f)
        {
            Debug.LogError("PacStudent needs a grid origin, positive cell size and positive movement speed.", this);
            enabled = false;
            return;
        }

        Vector3 localPosition = gridOrigin.InverseTransformPoint(transform.position);
        gridDepth = localPosition.z;
        currentCell = new Vector2Int(
            Mathf.RoundToInt(localPosition.x / cellSize),
            Mathf.RoundToInt(-localPosition.y / cellSize));

        if (!IsWalkable(currentCell))
        {
            Debug.LogError("PacStudent must start on a walkable grid cell.", this);
            enabled = false;
            return;
        }

        transform.position = CellToWorld(currentCell);
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("RouteControlled", true);
            animator.Play("right", 0, 0f);
            animator.Update(0f);
            animator.speed = 0f;
        }

        movementAudio = GetComponent<AudioSource>();
        collisionAudio = gameObject.AddComponent<AudioSource>();
        collisionAudio.playOnAwake = false;
        collisionAudio.spatialBlend = 0f;
        if (movementAudio != null)
        {
            movementAudio.Stop();
            movementAudio.playOnAwake = false;
            movementAudio.loop = true;
            movementAudio.spatialBlend = 0f;
        }

        pellets = pelletsRoot != null
            ? pelletsRoot.GetComponentsInChildren<SpriteRenderer>(true)
            : new SpriteRenderer[0];
        if (movementDustPrefab != null)
        {
            movementDust = Instantiate(movementDustPrefab, transform);
            movementDust.name = "MovementDust";
            movementDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            wallDust = Instantiate(movementDustPrefab, transform);
            wallDust.name = "WallCollisionDust";
            wallDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = wallDust.main;
            main.loop = false;
            ParticleSystem.EmissionModule emission = wallDust.emission;
            emission.enabled = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) lastInput = Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.A)) lastInput = Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.S)) lastInput = Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.D)) lastInput = Vector2Int.right;

        AdvanceMovement(Time.deltaTime);
    }

    private void AdvanceMovement(float deltaTime)
    {
        float remainingTime = deltaTime;
        while (remainingTime > 0f)
        {
            if (!isLerping && !TryBeginMove())
            {
                StopMovementFeedback();
                PlayWallFeedback();
                return;
            }

            if (!feedbackPlaying) PlayMovementFeedback();

            float stepTime = Mathf.Min(remainingTime, tweenDuration - tweenElapsed);
            tweenElapsed += stepTime;
            remainingTime -= stepTime;
            transform.position = Vector3.Lerp(tweenStart, tweenEnd, tweenElapsed / tweenDuration);

            if (tweenElapsed >= tweenDuration)
            {
                transform.position = tweenEnd;
                currentCell = targetCell;
                isLerping = false;
                CollectPellet();
                TeleportAtTunnelEnd();
            }
        }

        if (!isLerping && !CanMove(lastInput) && !CanMove(currentInput))
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

        targetCell = currentCell + currentInput;
        blockedDirection = Vector2Int.zero;
        CollectPellet();
        tweenStart = CellToWorld(currentCell);
        tweenEnd = CellToWorld(targetCell);
        tweenElapsed = 0f;
        tweenDuration = 1f / tilesPerSecond;
        isLerping = true;
        PlayMovementFeedback();
        return true;
    }

    private bool CanMove(Vector2Int direction)
    {
        return direction != Vector2Int.zero && IsWalkable(currentCell + direction);
    }

    private void CollectPellet()
    {
        if (pellets == null) return;
        foreach (SpriteRenderer pellet in pellets)
        {
            if (pellet == null || !pellet.enabled || !pellet.gameObject.activeInHierarchy ||
                (pellet.transform.position - transform.position).sqrMagnitude >= 0.000001f) continue;
            pellet.gameObject.SetActive(false);
            Destroy(pellet.gameObject);
            if (hud != null) hud.AddScore(10);
        }
    }

    private void TeleportAtTunnelEnd()
    {
        int lastColumn = levelMap.GetLength(1) * 2 - 1;
        if (currentCell.y != levelMap.GetLength(0) - 1) return;
        if (currentCell.x == 0 && currentInput == Vector2Int.left)
            currentCell.x = lastColumn;
        else if (currentCell.x == lastColumn && currentInput == Vector2Int.right)
            currentCell.x = 0;
        else return;
        transform.position = CellToWorld(currentCell);
        TeleportVersion++;
        if (movementDust != null) movementDust.Clear(true);
    }

    private void PlayWallFeedback()
    {
        Vector2Int direction = currentInput != Vector2Int.zero ? currentInput : lastInput;
        if (direction == Vector2Int.zero || direction == blockedDirection) return;
        blockedDirection = direction;
        if (collisionAudio != null && wallCollisionClip != null)
            collisionAudio.PlayOneShot(wallCollisionClip);
        if (wallDust != null)
        {
            wallDust.transform.position = CellToWorld(currentCell) +
                gridOrigin.TransformVector(new Vector3(direction.x, -direction.y, 0f) * cellSize * 0.5f);
            wallDust.Play();
            wallDust.Emit(8);
        }
    }

    private void PlayMovementFeedback()
    {
        string nextState = currentInput.x != 0
            ? (currentInput.x > 0 ? "right" : "left")
            : (currentInput.y < 0 ? "back" : "player-walk");
        if (animator != null)
        {
            if (!feedbackPlaying || movementState != nextState)
                animator.Play(nextState, 0, 0f);
            animator.speed = 1f;
        }
        movementState = nextState;

        if (movementAudio != null)
        {
            AudioClip nextClip = HasPelletAt(tweenEnd) ? pelletMovementClip : movementClip;
            if (movementAudio.clip != nextClip)
            {
                movementAudio.Stop();
                movementAudio.clip = nextClip;
            }
            if (nextClip != null && !movementAudio.isPlaying) movementAudio.Play();
        }

        if (movementDust != null)
        {
            movementDust.transform.localPosition = new Vector3(
                -currentInput.x * 0.3f, currentInput.y * 0.3f - 0.42f, 0f);
            if (!movementDust.isPlaying) movementDust.Play();
        }
        feedbackPlaying = true;
    }

    private bool HasPelletAt(Vector3 position)
    {
        if (pellets == null) return false;
        foreach (SpriteRenderer pellet in pellets)
        {
            if (pellet != null && pellet.enabled && pellet.gameObject.activeInHierarchy &&
                (pellet.transform.position - position).sqrMagnitude < 0.000001f)
                return true;
        }
        return false;
    }

    private void StopMovementFeedback()
    {
        if (animator != null) animator.speed = 0f;
        if (movementAudio != null && movementAudio.isPlaying) movementAudio.Stop();
        if (movementDust != null && (movementDust.isPlaying || movementDust.particleCount > 0))
            movementDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        feedbackPlaying = false;
    }

    private void OnDisable()
    {
        StopMovementFeedback();
        if (collisionAudio != null) collisionAudio.Stop();
        if (wallDust != null) wallDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private bool IsWalkable(Vector2Int cell)
    {
        int width = levelMap.GetLength(1) * 2;
        int height = levelMap.GetLength(0) * 2 - 1;
        if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height) return false;

        int column = Mathf.Min(cell.x, width - 1 - cell.x);
        int row = Mathf.Min(cell.y, height - 1 - cell.y);
        int tile = levelMap[row, column];
        return tile == 0 || tile == 5 || tile == 6;
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        return gridOrigin.TransformPoint(new Vector3(cell.x * cellSize, -cell.y * cellSize, gridDepth));
    }
}
