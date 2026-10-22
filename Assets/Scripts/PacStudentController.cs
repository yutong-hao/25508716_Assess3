using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [SerializeField] private Transform gridOrigin;
    [SerializeField, Min(0.001f)] private float cellSize = 0.125f;
    [SerializeField, Min(0.01f)] private float tilesPerSecond = 5f;

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
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("right", 0, 0f);
            animator.Update(0f);
            animator.speed = 0f;
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
            if (!isLerping && !TryBeginMove()) return;

            float stepTime = Mathf.Min(remainingTime, tweenDuration - tweenElapsed);
            tweenElapsed += stepTime;
            remainingTime -= stepTime;
            transform.position = Vector3.Lerp(tweenStart, tweenEnd, tweenElapsed / tweenDuration);

            if (tweenElapsed >= tweenDuration)
            {
                transform.position = tweenEnd;
                currentCell = targetCell;
                isLerping = false;
            }
        }
    }

    private bool TryBeginMove()
    {
        if (lastInput != Vector2Int.zero && IsWalkable(currentCell + lastInput))
        {
            currentInput = lastInput;
        }
        else if (currentInput == Vector2Int.zero || !IsWalkable(currentCell + currentInput))
        {
            return false;
        }

        targetCell = currentCell + currentInput;
        tweenStart = CellToWorld(currentCell);
        tweenEnd = CellToWorld(targetCell);
        tweenElapsed = 0f;
        tweenDuration = 1f / tilesPerSecond;
        isLerping = true;
        return true;
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
