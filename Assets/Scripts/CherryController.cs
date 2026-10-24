using UnityEngine;

[DefaultExecutionOrder(100)]
public class CherryController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bnPrefab;
    [SerializeField] private Transform gridOrg;
    [SerializeField] private Camera cam;
    [SerializeField] private Rect lvlBounds = new Rect(-0.0625f, -3.5625f, 3.5f, 3.625f);
    [SerializeField, Min(0.01f)] private float moveSpd = 0.625f;
    [SerializeField] private PacStudentController pc;
    [SerializeField] private hudc hud;
    [SerializeField] private AudioClip collectSfx;

    private const float SpawnWait = 5f;
    private const int BonusOrder = 100;

    private SpriteRenderer activeBn;
    private SpriteMask visMask;
    private Sprite maskSpr;
    private Vector3 startPos;
    private Vector3 endPos;
    private float spawnT;
    private float moveT;
    private float moveDur;
    private float prevPerimPos = -1f;
    private bool hadBn;
    private AudioSource collectAud;
    private Vector3 prevPcPos;
    private int prevTpVer;
    private Collider2D pcCol;

    private void Start()
    {
        if (bnPrefab == null || bnPrefab.sprite == null || gridOrg == null ||
            cam == null || moveSpd <= 0f ||
            lvlBounds.width <= 0f || lvlBounds.height <= 0f)
        {
            Debug.LogError(
                "Bonus needs its popcorn prefab, grid origin, camera, positive map bounds and movement speed.",
                this
            );

            enabled = false;
            return;
        }

        CreateVisibilityMask();

        collectAud = gameObject.AddComponent<AudioSource>();
        collectAud.playOnAwake = false;
        collectAud.spatialBlend = 0f;

        if (pc != null)
        {
            pcCol = pc.GetComponent<Collider2D>();
        }

        RememberPlayerPosition();
    }

    private void Update()
    {
        if (hud.GameOver)
        {
            return;
        }

        SpriteRenderer prevBn = activeBn;
        Vector3 prevBnPos;

        if (prevBn != null)
        {
            prevBnPos = prevBn.transform.position;
        }
        else
        {
            prevBnPos = Vector3.zero;
        }

        AdvanceBonus(Time.deltaTime);

        if (activeBn != null && pc != null && pc.CanAct)
        {
            if (prevBn == activeBn)
            {
                CheckPlayerCollision(prevBnPos);
            }
            else
            {
                CheckPlayerCollision(activeBn.transform.position);
            }
        }

        RememberPlayerPosition();
    }

    private void RememberPlayerPosition()
    {
        if (pc == null)
        {
            return;
        }

        prevPcPos = pc.transform.position;
        prevTpVer = pc.TpVer;
    }

    private void CheckPlayerCollision(Vector3 prevBnPos)
    {
        Vector3 pcPos = pc.transform.position;
        Vector3 localPlayerPos = gridOrg.InverseTransformPoint(pcPos);

        if (!lvlBounds.Contains(localPlayerPos))
        {
            return;
        }

        Vector2 relEnd = activeBn.transform.position - pcPos;
        Vector2 relStart;

        if (prevTpVer == pc.TpVer)
        {
            relStart = prevBnPos - prevPcPos;
        }
        else
        {
            relStart = relEnd;
        }

        Vector2 motion = relEnd - relStart;
        float frac;

        if (motion.sqrMagnitude > 0f)
        {
            float closePt = -Vector2.Dot(relStart, motion);
            frac = Mathf.Clamp01(closePt / motion.sqrMagnitude);
        }
        else
        {
            frac = 0f;
        }

        float pcRad;

        if (pcCol != null)
        {
            pcRad = Mathf.Min(
                pcCol.bounds.extents.x,
                pcCol.bounds.extents.y
            );
        }
        else
        {
            pcRad = 0f;
        }

        float bnRad = Mathf.Min(
            activeBn.bounds.extents.x,
            activeBn.bounds.extents.y
        );

        float radius = bnRad + pcRad;
        Vector2 closePos = relStart + motion * frac;

        if (closePos.sqrMagnitude > radius * radius)
        {
            return;
        }

        activeBn.gameObject.SetActive(false);
        Destroy(activeBn.gameObject);

        activeBn = null;
        hadBn = false;
        spawnT = 0f;

        if (hud != null)
        {
            hud.AddScore(100);
        }

        if (collectAud != null && collectSfx != null)
        {
            collectAud.PlayOneShot(collectSfx);
        }
    }

    private void AdvanceBonus(float dt)
    {
        float timeLeft = dt;

        if (activeBn == null)
        {
            if (hadBn)
            {
                hadBn = false;
                spawnT = 0f;
                return;
            }

            float waitT = Mathf.Min(
                timeLeft,
                SpawnWait - spawnT
            );

            spawnT = spawnT + waitT;
            timeLeft = timeLeft - waitT;

            if (spawnT < SpawnWait)
            {
                return;
            }

            SpawnBonus();
        }

        moveT = moveT + timeLeft;

        Vector3 localPos = Vector3.Lerp(
            startPos,
            endPos,
            moveT / moveDur
        );

        activeBn.transform.position = gridOrg.TransformPoint(localPos);

        if (moveT >= moveDur)
        {
            Destroy(activeBn.gameObject);

            activeBn = null;
            hadBn = false;
            spawnT = 0f;
        }
    }

    private void SpawnBonus()
    {
        float sprRad = bnPrefab.sprite.bounds.extents.magnitude;

        float scale = Mathf.Max(
            Mathf.Abs(bnPrefab.transform.localScale.x),
            Mathf.Abs(bnPrefab.transform.localScale.y)
        );

        float margin = sprRad * scale + 0.0625f;
        Rect spawnRect = ExpandBounds(lvlBounds, margin);

        float perimPos = Random.value;

        if (Mathf.Abs(perimPos - prevPerimPos) < 0.001f)
        {
            perimPos = Mathf.Repeat(perimPos + 0.5f, 1f);
        }

        prevPerimPos = perimPos;
        startPos = PointOnPerimeter(spawnRect, perimPos);

        Vector2 dir = lvlBounds.center - (Vector2)startPos;
        dir = dir.normalized;

        Rect camBounds = CameraBoundsInGrid();

        float xMin = Mathf.Min(lvlBounds.xMin, camBounds.xMin);
        float yMin = Mathf.Min(lvlBounds.yMin, camBounds.yMin);
        float xMax = Mathf.Max(lvlBounds.xMax, camBounds.xMax);
        float yMax = Mathf.Max(lvlBounds.yMax, camBounds.yMax);

        Rect allBounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        Rect outBounds = ExpandBounds(allBounds, margin);

        float exitDist = DistanceToEdge(
            outBounds,
            lvlBounds.center,
            dir
        );

        endPos = lvlBounds.center + dir * exitDist;

        Vector3 worldA = gridOrg.TransformPoint(startPos);
        Vector3 worldB = gridOrg.TransformPoint(endPos);

        moveDur = Vector3.Distance(worldA, worldB) / moveSpd;
        moveT = 0f;

        activeBn = Instantiate(bnPrefab, transform);
        activeBn.name = "MovingBonus_Popcorn";
        activeBn.transform.position = gridOrg.TransformPoint(startPos);
        activeBn.sortingLayerID = 0;
        activeBn.sortingOrder = BonusOrder;
        activeBn.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        hadBn = true;
    }

    private void CreateVisibilityMask()
    {
        Texture2D texture = Texture2D.whiteTexture;

        Rect texBounds = new Rect(
            0f,
            0f,
            texture.width,
            texture.height
        );

        Vector2 pivot = new Vector2(0.5f, 0.5f);

        maskSpr = Sprite.Create(texture, texBounds, pivot, 1f);
        maskSpr.name = "BonusMapBoundsMask";

        GameObject maskObj = new GameObject("BonusVisibilityMask");

        maskObj.transform.SetParent(gridOrg, false);
        maskObj.transform.localPosition = lvlBounds.center;

        float scaleX = lvlBounds.width / maskSpr.bounds.size.x;
        float scaleY = lvlBounds.height / maskSpr.bounds.size.y;

        maskObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        visMask = maskObj.AddComponent<SpriteMask>();
        visMask.sprite = maskSpr;
        visMask.alphaCutoff = 0.5f;
        visMask.isCustomRangeActive = true;
        visMask.backSortingLayerID = 0;
        visMask.frontSortingLayerID = 0;
        visMask.backSortingOrder = BonusOrder - 1;
        visMask.frontSortingOrder = BonusOrder;
    }

    private Rect CameraBoundsInGrid()
    {
        Vector3 camOffset = gridOrg.position - cam.transform.position;

        float depth = Vector3.Dot(
            camOffset,
            cam.transform.forward
        );

        Vector3 lowerW = cam.ViewportToWorldPoint(
            new Vector3(0f, 0f, depth)
        );

        Vector3 upperW = cam.ViewportToWorldPoint(
            new Vector3(1f, 1f, depth)
        );

        Vector3 lower = gridOrg.InverseTransformPoint(lowerW);
        Vector3 upper = gridOrg.InverseTransformPoint(upperW);

        float xMin = Mathf.Min(lower.x, upper.x);
        float yMin = Mathf.Min(lower.y, upper.y);
        float xMax = Mathf.Max(lower.x, upper.x);
        float yMax = Mathf.Max(lower.y, upper.y);

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static Rect ExpandBounds(Rect bounds, float margin)
    {
        float xMin = bounds.xMin - margin;
        float yMin = bounds.yMin - margin;
        float xMax = bounds.xMax + margin;
        float yMax = bounds.yMax + margin;

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static Vector2 PointOnPerimeter(Rect bounds, float pos)
    {
        float distance = pos * 2f * (bounds.width + bounds.height);

        if (distance < bounds.width)
        {
            return new Vector2(bounds.xMin + distance, bounds.yMin);
        }

        distance = distance - bounds.width;

        if (distance < bounds.height)
        {
            return new Vector2(bounds.xMax, bounds.yMin + distance);
        }

        distance = distance - bounds.height;

        if (distance < bounds.width)
        {
            return new Vector2(bounds.xMax - distance, bounds.yMax);
        }

        distance = distance - bounds.width;

        return new Vector2(bounds.xMin, bounds.yMax - distance);
    }

    private static float DistanceToEdge(
        Rect bounds,
        Vector2 origin,
        Vector2 dir
    )
    {
        float horiz;

        if (Mathf.Abs(dir.x) < 0.000001f)
        {
            horiz = float.PositiveInfinity;
        }
        else
        {
            float edgeX;

            if (dir.x > 0f)
            {
                edgeX = bounds.xMax;
            }
            else
            {
                edgeX = bounds.xMin;
            }

            horiz = (edgeX - origin.x) / dir.x;
        }

        float vertical;

        if (Mathf.Abs(dir.y) < 0.000001f)
        {
            vertical = float.PositiveInfinity;
        }
        else
        {
            float edgeY;

            if (dir.y > 0f)
            {
                edgeY = bounds.yMax;
            }
            else
            {
                edgeY = bounds.yMin;
            }

            vertical = (edgeY - origin.y) / dir.y;
        }

        return Mathf.Min(horiz, vertical);
    }

    private void OnEnable()
    {
        if (visMask != null)
        {
            visMask.enabled = true;
        }
    }

    private void OnDisable()
    {
        if (activeBn != null)
        {
            Destroy(activeBn.gameObject);
        }

        activeBn = null;
        hadBn = false;
        spawnT = 0f;

        if (visMask != null)
        {
            visMask.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (visMask != null)
        {
            Destroy(visMask.gameObject);
        }

        if (maskSpr != null)
        {
            Destroy(maskSpr);
        }
    }
}
