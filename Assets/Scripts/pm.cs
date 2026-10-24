using UnityEngine;

public class pm : MonoBehaviour
{
    public Transform[] points;
    public float speed = 0.15f;

    [Tooltip("勾选：原地循环展示四向行走和死亡动画；不勾选：沿路点连续移动。")]
    public bool previewAnims;

    private int curPoint;
    private float elapsed;
    private float dur;
    private Vector3 segStart;
    private Vector3 segEnd;
    private Animator anim;
    private AudioSource moveAud;
    private bool initDone;
    private bool activePrev;

    void OnEnable()
    {
        anim = GetComponent<Animator>();
        moveAud = GetComponent<AudioSource>();
        if (initDone) ApplyPreviewMode();
    }

    void Start()
    {
        if (anim == null || points == null || points.Length != 4 || speed <= 0f)
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
        initDone = true;
        ApplyPreviewMode();
    }

    void Update()
    {
        if (activePrev != previewAnims) ApplyPreviewMode();
        if (activePrev) return;

        elapsed += Time.deltaTime;
        while (elapsed >= dur)
        {
            elapsed -= dur;
            transform.position = segEnd;
            curPoint = (curPoint + 1) % points.Length;
            BeginSegment();
        }

        transform.position = Vector3.Lerp(segStart, segEnd, elapsed / dur);
    }

    void ApplyPreviewMode()
    {
        activePrev = previewAnims;
        anim.SetBool("RouteControlled", !activePrev);
        if (activePrev)
        {
            anim.Play("right", 0, 0f);
            if (moveAud != null) moveAud.Stop();
        }
        else
        {
            BeginSegment();
        }
    }

    void BeginSegment()
    {
        segStart = points[curPoint].position;
        segEnd = points[(curPoint + 1) % points.Length].position;
        dur = Vector3.Distance(segStart, segEnd) / speed;
        Vector3 dir = segEnd - segStart;
        string state = Mathf.Abs(dir.x) > Mathf.Abs(dir.y)
            ? (dir.x > 0f ? "right" : "left")
            : (dir.y > 0f ? "back" : "player-walk");
        anim.Play(state, 0, 0f);
        if (moveAud != null && !moveAud.isPlaying) moveAud.Play();
    }

    void OnDisable()
    {
        if (anim != null) anim.SetBool("RouteControlled", false);
        if (moveAud != null) moveAud.Stop();
    }
}
