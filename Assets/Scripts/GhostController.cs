using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(20)]
public class GhostController : MonoBehaviour
{
    public int Number { get; private set; }
    public bool HasExited { get; private set; }
    public Vector2Int CurrentCell { get; private set; }
    public Vector2Int PreviousCell { get; private set; }
    public Vector2Int TargetCell { get; private set; }
    public bool IsMoving { get; private set; }

    public float Speed
    {
        get
        {
            float speed = player.MovementSpeed * 0.9f;

            if (states.IsNormal(gameObject))
            {
                return speed * 1f;
            }

            return speed * 0.5f;
        }
    }

    private PacStudentController player;
    private PowerPelletController states;
    private Animator animator;
    private Vector3 spawnPosition;
    private Vector3 segmentStart;
    private Vector3 segmentEnd;
    private float segmentTravel;
    private float segmentLength;
    private bool returningHome;

    private readonly List<Vector2Int> clockwiseRoute = new List<Vector2Int>();

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    public void Initialize(
        int number,
        PacStudentController pacStudent,
        PowerPelletController controller
    )
    {
        Number = number;
        player = pacStudent;
        states = controller;
        animator = GetComponent<Animator>();
        spawnPosition = transform.position;

        BuildClockwiseRoute();
        ResetMovement();
    }

    public void ResetMovement()
    {
        transform.position = spawnPosition;

        CurrentCell = player.WorldToCell(spawnPosition);
        PreviousCell = CurrentCell;
        TargetCell = CurrentCell;

        HasExited = false;
        returningHome = false;
        IsMoving = false;
        segmentTravel = 0f;
    }

    private void Update()
    {
        AdvanceMovement(Time.deltaTime);
    }

    private void AdvanceMovement(float deltaTime)
    {
        if (states == null || !states.CanMoveGhosts)
        {
            return;
        }

        animator.speed = 1f;

        float remaining = deltaTime;

        while (remaining > 0f && states.CanMoveGhosts)
        {
            bool wasDead = states.IsDead(gameObject);

            if (wasDead)
            {
                if (!returningHome)
                {
                    returningHome = true;
                    BeginSegment(spawnPosition);
                }
            }
            else if (!IsMoving)
            {
                if (!HasExited)
                {
                    TargetCell = ChooseExitCell();
                }
                else
                {
                    TargetCell = ChooseNextCell(CurrentCell, PreviousCell);
                }

                BeginSegment(player.CellToWorld(TargetCell));
            }

            float speed = Speed;
            float distanceLeft = Mathf.Max(0f, segmentLength - segmentTravel);
            float stepTime = Mathf.Min(remaining, distanceLeft / speed);

            segmentTravel = Mathf.Min(
                segmentLength,
                segmentTravel + speed * stepTime
            );

            remaining = remaining - stepTime;

            Vector3 previousPosition = transform.position;

            if (segmentLength > 0f)
            {
                transform.position = Vector3.Lerp(
                    segmentStart,
                    segmentEnd,
                    segmentTravel / segmentLength
                );
            }
            else
            {
                transform.position = segmentEnd;
            }

            if (segmentTravel >= segmentLength)
            {
                transform.position = segmentEnd;
                IsMoving = false;

                if (returningHome)
                {
                    states.RestoreAtHome(gameObject);
                    ResetMovement();
                }
                else
                {
                    PreviousCell = CurrentCell;
                    CurrentCell = TargetCell;

                    if (!HasExited &&
                        (CurrentCell.y == 11 || CurrentCell.y == 17))
                    {
                        HasExited = true;
                    }
                }
            }

            if (!wasDead && !states.IsDead(gameObject))
            {
                states.CheckGhostContact(
                    gameObject,
                    previousPosition,
                    transform.position
                );
            }

            if (!states.CanMoveGhosts)
            {
                return;
            }

            if (stepTime == 0f && IsMoving)
            {
                return;
            }
        }
    }

    private void BeginSegment(Vector3 end)
    {
        segmentStart = transform.position;
        segmentEnd = new Vector3(end.x, end.y, spawnPosition.z);
        segmentLength = Vector3.Distance(segmentStart, segmentEnd);
        segmentTravel = 0f;
        IsMoving = true;
    }

    private Vector2Int ChooseExitCell()
    {
        int exitColumn;

        if (Number % 2 == 1)
        {
            exitColumn = 13;
        }
        else
        {
            exitColumn = 14;
        }

        if (CurrentCell.x != exitColumn)
        {
            int xDirection;

            if (CurrentCell.x < exitColumn)
            {
                xDirection = 1;
            }
            else
            {
                xDirection = -1;
            }

            return CurrentCell + new Vector2Int(xDirection, 0);
        }

        if (Number % 2 == 1)
        {
            return CurrentCell + Vector2Int.down;
        }

        return CurrentCell + Vector2Int.up;
    }

    public bool IsValidCell(Vector2Int cell)
    {
        if (!player.IsWalkable(cell))
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
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int i = 0; i < Directions.Length; i = i + 1)
        {
            Vector2Int direction = Directions[i];
            Vector2Int next = cell + direction;

            if (IsValidCell(next) && next != previous)
            {
                candidates.Add(next);
            }
        }

        if (candidates.Count == 0 &&
            IsValidCell(previous) &&
            previous != cell)
        {
            candidates.Add(previous);
        }

        return candidates;
    }

    private Vector2Int ChooseNextCell(
        Vector2Int cell,
        Vector2Int previous
    )
    {
        List<Vector2Int> candidates = LegalNeighbours(cell, previous);

        if (candidates.Count == 0)
        {
            throw new System.InvalidOperationException(
                "Ghost has no valid neighbouring cell: " + cell
            );
        }

        bool normal = states.IsNormal(gameObject);

        if (normal && Number == 4)
        {
            int bestDistance = int.MaxValue;
            Vector2Int choice = candidates[0];

            int routeIndex = clockwiseRoute.IndexOf(cell);

            if (routeIndex >= 0)
            {
                int nextIndex = (routeIndex + 1) % clockwiseRoute.Count;
                Vector2Int clockwise = clockwiseRoute[nextIndex];

                if (candidates.Contains(clockwise))
                {
                    return clockwise;
                }
            }

            for (int i = 0; i < candidates.Count; i = i + 1)
            {
                Vector2Int candidate = candidates[i];
                int distance = DistanceToClockwiseRoute(candidate, cell);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    choice = candidate;
                }
            }

            return choice;
        }

        if (normal && Number == 3)
        {
            int randomIndex = Random.Range(0, candidates.Count);
            return candidates[randomIndex];
        }

        bool approach = normal && Number == 2;

        Vector3 currentDifference =
            player.CellToWorld(cell) - player.transform.position;

        float currentDistance = currentDifference.sqrMagnitude;

        List<Vector2Int> preferred = new List<Vector2Int>();

        for (int i = 0; i < candidates.Count; i = i + 1)
        {
            Vector2Int candidate = candidates[i];

            Vector3 difference =
                player.CellToWorld(candidate) - player.transform.position;

            float distance = difference.sqrMagnitude;

            if (approach)
            {
                if (distance <= currentDistance + 0.000001f)
                {
                    preferred.Add(candidate);
                }
            }
            else
            {
                if (distance >= currentDistance - 0.000001f)
                {
                    preferred.Add(candidate);
                }
            }
        }

        List<Vector2Int> choices;

        if (preferred.Count > 0)
        {
            choices = preferred;
        }
        else
        {
            choices = candidates;
        }

        int choiceIndex = Random.Range(0, choices.Count);
        return choices[choiceIndex];
    }

    private int DistanceToClockwiseRoute(
        Vector2Int start,
        Vector2Int previous
    )
    {
        Queue<(Vector2Int cell, Vector2Int previous, int distance)> queue =
            new Queue<(Vector2Int cell, Vector2Int previous, int distance)>();

        HashSet<(Vector2Int, Vector2Int)> visited =
            new HashSet<(Vector2Int, Vector2Int)>();

        queue.Enqueue((start, previous, 0));
        visited.Add((start, previous));

        while (queue.Count > 0)
        {
            (Vector2Int cell, Vector2Int previous, int distance) current =
                queue.Dequeue();

            int index = clockwiseRoute.IndexOf(current.cell);

            if (index >= 0)
            {
                int nextIndex = (index + 1) % clockwiseRoute.Count;

                if (clockwiseRoute[nextIndex] != current.previous)
                {
                    return current.distance;
                }
            }

            List<Vector2Int> neighbours = LegalNeighbours(
                current.cell,
                current.previous
            );

            for (int i = 0; i < neighbours.Count; i = i + 1)
            {
                Vector2Int next = neighbours[i];

                if (visited.Add((next, current.cell)))
                {
                    queue.Enqueue((
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

            int nextIndex = (i + 1) % corners.Length;
            Vector2Int end = corners[nextIndex];

            int xDirection = System.Math.Sign(end.x - cell.x);
            int yDirection = System.Math.Sign(end.y - cell.y);

            Vector2Int direction = new Vector2Int(xDirection, yDirection);

            while (cell != end)
            {
                clockwiseRoute.Add(cell);
                cell = cell + direction;
            }
        }
    }
}