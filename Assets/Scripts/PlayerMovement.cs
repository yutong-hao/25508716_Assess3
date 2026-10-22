using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Transform[] points;
    public float speed = 0.15f;

    [Tooltip("勾选：原地循环展示四向行走和死亡动画；不勾选：沿路点连续移动。")]
    public bool previewAnimations;

    private int currentPoint;
    private float elapsed;
    private float duration;
    private Vector3 segmentStart;
    private Vector3 segmentEnd;
    private Animator animator;
    private AudioSource movementAudio;
    private bool initialized;
    private bool activePreview;

    void OnEnable()
    {
        animator = GetComponent<Animator>();
        movementAudio = GetComponent<AudioSource>();
        if (initialized) ApplyPreviewMode();
    }

    void Start()
    {
        if (animator == null || points == null || points.Length != 4 || speed <= 0f)
        {
            enabled = false;
            return;
        }

        for (int index = 0; index < points.Length; index++)
        {
            if (points[index] == null || points[(index + 1) % points.Length] == null ||
                Vector3.Distance(points[index].position, points[(index + 1) % points.Length].position) < 0.0001f)
            {
                enabled = false;
                return;
            }
        }

        transform.position = points[0].position;
        initialized = true;
        ApplyPreviewMode();
    }

    void Update()
    {
        if (activePreview != previewAnimations) ApplyPreviewMode();
        if (activePreview) return;

        elapsed += Time.deltaTime;
        while (elapsed >= duration)
        {
            elapsed -= duration;
            transform.position = segmentEnd;
            currentPoint = (currentPoint + 1) % points.Length;
            BeginSegment();
        }

        transform.position = Vector3.Lerp(segmentStart, segmentEnd, elapsed / duration);
    }

    void ApplyPreviewMode()
    {
        activePreview = previewAnimations;
        animator.SetBool("RouteControlled", !activePreview);
        if (activePreview)
        {
            animator.Play("right", 0, 0f);
            if (movementAudio != null) movementAudio.Stop();
        }
        else
        {
            BeginSegment();
        }
    }

    void BeginSegment()
    {
        segmentStart = points[currentPoint].position;
        segmentEnd = points[(currentPoint + 1) % points.Length].position;
        duration = Vector3.Distance(segmentStart, segmentEnd) / speed;
        Vector3 direction = segmentEnd - segmentStart;
        string state = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? (direction.x > 0f ? "right" : "left")
            : (direction.y > 0f ? "back" : "player-walk");
        animator.Play(state, 0, 0f);
        if (movementAudio != null && !movementAudio.isPlaying) movementAudio.Play();
    }

    void OnDisable()
    {
        if (animator != null) animator.SetBool("RouteControlled", false);
        if (movementAudio != null) movementAudio.Stop();
    }
}
