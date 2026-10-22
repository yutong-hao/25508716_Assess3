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
    public float Speed => player.MovementSpeed * 0.9f * (states.IsNormal(gameObject) ? 1f : 0.5f);

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
    private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    public void Initialize(int number, PacStudentController pacStudent, PowerPelletController controller)
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
        if (states == null || !states.CanMoveGhosts) return;
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
                TargetCell = !HasExited ? ChooseExitCell() : ChooseNextCell(CurrentCell, PreviousCell);
                BeginSegment(player.CellToWorld(TargetCell));
            }

            float speed = Speed;
            float stepTime = Mathf.Min(remaining, Mathf.Max(0f, segmentLength - segmentTravel) / speed);
            segmentTravel = Mathf.Min(segmentLength, segmentTravel + speed * stepTime);
            remaining -= stepTime;
            Vector3 previousPosition = transform.position;
            transform.position = segmentLength > 0f ? Vector3.Lerp(segmentStart, segmentEnd, segmentTravel / segmentLength) : segmentEnd;
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
                    if (!HasExited && (CurrentCell.y == 11 || CurrentCell.y == 17)) HasExited = true;
                }
            }
            if (!wasDead && !states.IsDead(gameObject))
                states.CheckGhostContact(gameObject, previousPosition, transform.position);
            if (!states.CanMoveGhosts) return;
            if (stepTime == 0f && IsMoving) return;
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
        int exitColumn = Number % 2 == 1 ? 13 : 14;
        if (CurrentCell.x != exitColumn)
            return CurrentCell + new Vector2Int(CurrentCell.x < exitColumn ? 1 : -1, 0);
        return CurrentCell + (Number % 2 == 1 ? Vector2Int.down : Vector2Int.up);
    }

    public bool IsValidCell(Vector2Int cell)
    {
        if (!player.IsWalkable(cell)) return false;
        if (cell.x >= 10 && cell.x <= 17 && cell.y >= 12 && cell.y <= 16) return false;
        if (cell.y == 14 && (cell.x < 6 || cell.x > 21)) return false;
        return true;
    }

    private List<Vector2Int> LegalNeighbours(Vector2Int cell, Vector2Int previous)
    {
        var candidates = new List<Vector2Int>();
        foreach (Vector2Int direction in Directions)
        {
            Vector2Int next = cell + direction;
            if (IsValidCell(next) && next != previous) candidates.Add(next);
        }
        if (candidates.Count == 0 && IsValidCell(previous) && previous != cell) candidates.Add(previous);
        return candidates;
    }

    private Vector2Int ChooseNextCell(Vector2Int cell, Vector2Int previous)
    {
        List<Vector2Int> candidates = LegalNeighbours(cell, previous);
        if (candidates.Count == 0)
            throw new System.InvalidOperationException("Ghost has no valid neighbouring cell: " + cell);
        bool normal = states.IsNormal(gameObject);
        if (normal && Number == 4)
        {
            int bestDistance = int.MaxValue;
            Vector2Int choice = candidates[0];
            int routeIndex = clockwiseRoute.IndexOf(cell);
            if (routeIndex >= 0)
            {
                Vector2Int clockwise = clockwiseRoute[(routeIndex + 1) % clockwiseRoute.Count];
                if (candidates.Contains(clockwise)) return clockwise;
            }
            foreach (Vector2Int candidate in candidates)
            {
                int distance = DistanceToClockwiseRoute(candidate, cell);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    choice = candidate;
                }
            }
            return choice;
        }
        if (normal && Number == 3) return candidates[Random.Range(0, candidates.Count)];
        bool approach = normal && Number == 2;
        float currentDistance = (player.CellToWorld(cell) - player.transform.position).sqrMagnitude;
        var preferred = candidates.FindAll(candidate => approach
            ? (player.CellToWorld(candidate) - player.transform.position).sqrMagnitude <= currentDistance + 0.000001f
            : (player.CellToWorld(candidate) - player.transform.position).sqrMagnitude >= currentDistance - 0.000001f);
        List<Vector2Int> choices = preferred.Count > 0 ? preferred : candidates;
        return choices[Random.Range(0, choices.Count)];
    }

    private int DistanceToClockwiseRoute(Vector2Int start, Vector2Int previous)
    {
        var queue = new Queue<(Vector2Int cell, Vector2Int previous, int distance)>();
        var visited = new HashSet<(Vector2Int, Vector2Int)>();
        queue.Enqueue((start, previous, 0));
        visited.Add((start, previous));
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int index = clockwiseRoute.IndexOf(current.cell);
            if (index >= 0 && clockwiseRoute[(index + 1) % clockwiseRoute.Count] != current.previous)
                return current.distance;
            foreach (Vector2Int next in LegalNeighbours(current.cell, current.previous))
                if (visited.Add((next, current.cell))) queue.Enqueue((next, current.cell, current.distance + 1));
        }
        return int.MaxValue;
    }

    private void BuildClockwiseRoute()
    {
        Vector2Int[] corners = {
            new Vector2Int(1, 1), new Vector2Int(12, 1), new Vector2Int(12, 5), new Vector2Int(15, 5),
            new Vector2Int(15, 1), new Vector2Int(26, 1), new Vector2Int(26, 8), new Vector2Int(21, 8),
            new Vector2Int(21, 20), new Vector2Int(26, 20), new Vector2Int(26, 27), new Vector2Int(15, 27),
            new Vector2Int(15, 23), new Vector2Int(12, 23), new Vector2Int(12, 27), new Vector2Int(1, 27),
            new Vector2Int(1, 20), new Vector2Int(6, 20), new Vector2Int(6, 8), new Vector2Int(1, 8)
        };
        for (int index = 0; index < corners.Length; index++)
        {
            Vector2Int cell = corners[index];
            Vector2Int end = corners[(index + 1) % corners.Length];
            Vector2Int direction = new Vector2Int(System.Math.Sign(end.x - cell.x), System.Math.Sign(end.y - cell.y));
            while (cell != end)
            {
                clockwiseRoute.Add(cell);
                cell += direction;
            }
        }
    }
}
