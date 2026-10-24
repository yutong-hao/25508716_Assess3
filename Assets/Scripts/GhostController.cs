using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(20)]
public class GhostController : MonoBehaviour
{
    public int Number { get; private set; }
    public bool Exited { get; private set; }
    public Vector2Int CurCell { get; private set; }
    public Vector2Int PrevCell { get; private set; }
    public Vector2Int TgtCell { get; private set; }
    public bool Moving { get; private set; }

    public float Speed
    {
        get
        {
            float speed = pc.MoveSpd * 0.9f;

            if (ppc.IsNormal(gameObject))
            {
                return speed * 1f;
            }

            return speed * 0.5f;
        }
    }

    private PacStudentController pc;
    private pp ppc;
    private Animator anim;
    private Vector3 spawnPos;
    private Vector3 segStart;
    private Vector3 segEnd;
    private float segTravel;
    private float segLength;
    private bool goingHome;

    private readonly List<Vector2Int> patrol = new List<Vector2Int>();

    private static readonly Vector2Int[] moveDirs =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    public void Initialize(
        int number,
        PacStudentController pacStudent,
        pp controller
    )
    {
        Number = number;
        pc = pacStudent;
        ppc = controller;
        anim = GetComponent<Animator>();
        spawnPos = transform.position;

        BuildClockwiseRoute();
        ResetMovement();
    }

    public void ResetMovement()
    {
        transform.position = spawnPos;

        CurCell = pc.WorldToCell(spawnPos);
        PrevCell = CurCell;
        TgtCell = CurCell;

        Exited = false;
        goingHome = false;
        Moving = false;
        segTravel = 0f;
    }

    private void Update()
    {
        AdvanceMovement(Time.deltaTime);
    }

    private void AdvanceMovement(float dt)
    {
        if (ppc == null || !ppc.CanMoveGh)
        {
            return;
        }

        anim.speed = 1f;

        float timeLeft = dt;

        while (timeLeft > 0f && ppc.CanMoveGh)
        {
            bool deadBefore = ppc.IsDead(gameObject);

            if (deadBefore)
            {
                if (!goingHome)
                {
                    goingHome = true;
                    BeginSegment(spawnPos);
                }
            }
            else if (!Moving)
            {
                if (!Exited)
                {
                    TgtCell = ChooseExitCell();
                }
                else
                {
                    TgtCell = ChooseNextCell(CurCell, PrevCell);
                }

                BeginSegment(pc.CellToWorld(TgtCell));
            }

            float speed = Speed;
            float distLeft = Mathf.Max(0f, segLength - segTravel);
            float moveTime = Mathf.Min(timeLeft, distLeft / speed);

            segTravel = Mathf.Min(
                segLength,
                segTravel + speed * moveTime
            );

            timeLeft = timeLeft - moveTime;

            Vector3 prevPos = transform.position;

            if (segLength > 0f)
            {
                transform.position = Vector3.Lerp(
                    segStart,
                    segEnd,
                    segTravel / segLength
                );
            }
            else
            {
                transform.position = segEnd;
            }

            if (segTravel >= segLength)
            {
                transform.position = segEnd;
                Moving = false;

                if (goingHome)
                {
                    ppc.RestoreAtHome(gameObject);
                    ResetMovement();
                }
                else
                {
                    PrevCell = CurCell;
                    CurCell = TgtCell;

                    if (!Exited &&
                        (CurCell.y == 11 || CurCell.y == 17))
                    {
                        Exited = true;
                    }
                }
            }

            if (!deadBefore && !ppc.IsDead(gameObject))
            {
                ppc.CheckGhostContact(
                    gameObject,
                    prevPos,
                    transform.position
                );
            }

            if (!ppc.CanMoveGh)
            {
                return;
            }

            if (moveTime == 0f && Moving)
            {
                return;
            }
        }
    }

    private void BeginSegment(Vector3 endPos)
    {
        segStart = transform.position;
        segEnd = new Vector3(endPos.x, endPos.y, spawnPos.z);
        segLength = Vector3.Distance(segStart, segEnd);
        segTravel = 0f;
        Moving = true;
    }

    private Vector2Int ChooseExitCell()
    {
        int exitCol;

        if (Number % 2 == 1)
        {
            exitCol = 13;
        }
        else
        {
            exitCol = 14;
        }

        if (CurCell.x != exitCol)
        {
            int xDir;

            if (CurCell.x < exitCol)
            {
                xDir = 1;
            }
            else
            {
                xDir = -1;
            }

            return CurCell + new Vector2Int(xDir, 0);
        }

        if (Number % 2 == 1)
        {
            return CurCell + Vector2Int.down;
        }

        return CurCell + Vector2Int.up;
    }

    public bool IsValidCell(Vector2Int cell)
    {
        if (!pc.IsWalkable(cell))
        {
            return false;
        }

        if (cell.x >= 10 && cell.x <= 17 &&
            cell.y >= 12 && cell.y <= 16)
        {
            return false;
        }

        if (cell.y == 14 && (cell.x < 6 || cell.x > 21))
        {
            return false;
        }

        return true;
    }

    private List<Vector2Int> LegalNeighbours(
        Vector2Int cell,
        Vector2Int previous
    )
    {
        List<Vector2Int> opts = new List<Vector2Int>();

        for (int i = 0; i < moveDirs.Length; i = i + 1)
        {
            Vector2Int dir = moveDirs[i];
            Vector2Int next = cell + dir;

            if (IsValidCell(next) && next != previous)
            {
                opts.Add(next);
            }
        }

        if (opts.Count == 0 &&
            IsValidCell(previous) &&
            previous != cell)
        {
            opts.Add(previous);
        }

        return opts;
    }

    private Vector2Int ChooseNextCell(
        Vector2Int cell,
        Vector2Int previous
    )
    {
        List<Vector2Int> opts = LegalNeighbours(cell, previous);

        if (opts.Count == 0)
        {
            throw new System.InvalidOperationException(
                "Ghost has no valid neighbouring cell: " + cell
            );
        }

        bool isNormal = ppc.IsNormal(gameObject);

        if (isNormal && Number == 4)
        {
            int bestDist = int.MaxValue;
            Vector2Int pickCell = opts[0];

            int routeIdx = patrol.IndexOf(cell);

            if (routeIdx >= 0)
            {
                int nextIdx = (routeIdx + 1) % patrol.Count;
                Vector2Int nextCell = patrol[nextIdx];

                if (opts.Contains(nextCell))
                {
                    return nextCell;
                }
            }

            for (int i = 0; i < opts.Count; i = i + 1)
            {
                Vector2Int option = opts[i];
                int dist = DistanceToClockwiseRoute(option, cell);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    pickCell = option;
                }
            }

            return pickCell;
        }

        if (isNormal && Number == 3)
        {
            int randIndex = Random.Range(0, opts.Count);
            return opts[randIndex];
        }

        bool closer = isNormal && Number == 2;

        Vector3 curOffset =
            pc.CellToWorld(cell) - pc.transform.position;

        float curDist = curOffset.sqrMagnitude;

        List<Vector2Int> prefCells = new List<Vector2Int>();

        for (int i = 0; i < opts.Count; i = i + 1)
        {
            Vector2Int option = opts[i];

            Vector3 offset =
                pc.CellToWorld(option) - pc.transform.position;

            float dist = offset.sqrMagnitude;

            if (closer)
            {
                if (dist <= curDist + 0.000001f)
                {
                    prefCells.Add(option);
                }
            }
            else
            {
                if (dist >= curDist - 0.000001f)
                {
                    prefCells.Add(option);
                }
            }
        }

        List<Vector2Int> finalOpts;

        if (prefCells.Count > 0)
        {
            finalOpts = prefCells;
        }
        else
        {
            finalOpts = opts;
        }

        int pickIndex = Random.Range(0, finalOpts.Count);
        return finalOpts[pickIndex];
    }

    private int DistanceToClockwiseRoute(
        Vector2Int start,
        Vector2Int previous
    )
    {
        Queue<(Vector2Int cell, Vector2Int previous, int distance)> searchQueue =
            new Queue<(Vector2Int cell, Vector2Int previous, int distance)>();

        HashSet<(Vector2Int, Vector2Int)> checkedStates =
            new HashSet<(Vector2Int, Vector2Int)>();

        searchQueue.Enqueue((start, previous, 0));
        checkedStates.Add((start, previous));

        while (searchQueue.Count > 0)
        {
            (Vector2Int cell, Vector2Int previous, int distance) current =
                searchQueue.Dequeue();

            int routeIdx = patrol.IndexOf(current.cell);

            if (routeIdx >= 0)
            {
                int nextIdx = (routeIdx + 1) % patrol.Count;

                if (patrol[nextIdx] != current.previous)
                {
                    return current.distance;
                }
            }

            List<Vector2Int> nearCells = LegalNeighbours(
                current.cell,
                current.previous
            );

            for (int i = 0; i < nearCells.Count; i = i + 1)
            {
                Vector2Int next = nearCells[i];

                if (checkedStates.Add((next, current.cell)))
                {
                    searchQueue.Enqueue((
                        next,
                        current.cell,
                        current.distance + 1
                    ));
                }
            }
        }

        return int.MaxValue;
    }

    private void BuildClockwiseRoute()
    {
        Vector2Int[] corners =
        {
            new Vector2Int(1, 1),
            new Vector2Int(12, 1),
            new Vector2Int(12, 5),
            new Vector2Int(15, 5),
            new Vector2Int(15, 1),
            new Vector2Int(26, 1),
            new Vector2Int(26, 8),
            new Vector2Int(21, 8),
            new Vector2Int(21, 20),
            new Vector2Int(26, 20),
            new Vector2Int(26, 27),
            new Vector2Int(15, 27),
            new Vector2Int(15, 23),
            new Vector2Int(12, 23),
            new Vector2Int(12, 27),
            new Vector2Int(1, 27),
            new Vector2Int(1, 20),
            new Vector2Int(6, 20),
            new Vector2Int(6, 8),
            new Vector2Int(1, 8)
        };

        for (int i = 0; i < corners.Length; i = i + 1)
        {
            Vector2Int cell = corners[i];

            int nextIdx = (i + 1) % corners.Length;
            Vector2Int end = corners[nextIdx];

            int xDir = System.Math.Sign(end.x - cell.x);
            int yDir = System.Math.Sign(end.y - cell.y);

            Vector2Int dir = new Vector2Int(xDir, yDir);

            while (cell != end)
            {
                patrol.Add(cell);
                cell = cell + dir;
            }
        }
    }
}