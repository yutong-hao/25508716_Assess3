using UnityEngine;

[DefaultExecutionOrder(100)]
public class CherryController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bonusPrefab;
    [SerializeField] private Transform gridOrigin;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Rect levelBounds = new Rect(-0.0625f, -3.5625f, 3.5f, 3.625f);
    [SerializeField, Min(0.01f)] private float moveSpeed = 0.625f;
    [SerializeField] private PacStudentController player;
    [SerializeField] private InGameHUDController hud;
    [SerializeField] private AudioClip collectClip;

    private const float SpawnDelay = 5f;
    private const int BonusSortingOrder = 100;

    private SpriteRenderer activeBonus;
    private SpriteMask visibilityMask;
    private Sprite maskSprite;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float spawnElapsed;
    private float movementElapsed;
    private float movementDuration;
    private float previousPerimeterPosition = -1f;
    private bool hadActiveBonus;
    private AudioSource collectAudio;
    private Vector3 previousPlayerPosition;
    private int previousTeleportVersion;
    private Collider2D playerCollider;

    private void Start()
    {
        if (bonusPrefab == null || bonusPrefab.sprite == null || gridOrigin == null ||
            gameCamera == null || moveSpeed <= 0f ||
            levelBounds.width <= 0f || levelBounds.height <= 0f)
        {
            Debug.LogError(
                "Bonus needs its popcorn prefab, grid origin, camera, positive map bounds and movement speed.",
                this
            );

            enabled = false;
            return;
        }

        CreateVisibilityMask();

        collectAudio = gameObject.AddComponent<AudioSource>();
        collectAudio.playOnAwake = false;
        collectAudio.spatialBlend = 0f;

        if (player != null)
        {
            playerCollider = player.GetComponent<Collider2D>();
        }

        RememberPlayerPosition();
    }

    private void Update()
    {
        if (hud.IsGameOver)
        {
            return;
        }

        SpriteRenderer previousBonus = activeBonus;
        Vector3 previousBonusPosition;

        if (previousBonus != null)
        {
            previousBonusPosition = previousBonus.transform.position;
        }
        else
        {
            previousBonusPosition = Vector3.zero;
        }

        AdvanceBonus(Time.deltaTime);

        if (activeBonus != null && player != null && player.CanInteract)
        {
            if (previousBonus == activeBonus)
            {
                CheckPlayerCollision(previousBonusPosition);
            }
            else
            {
                CheckPlayerCollision(activeBonus.transform.position);
            }
        }

        RememberPlayerPosition();
    }

    private void RememberPlayerPosition()
    {
        if (player == null)
        {
            return;
        }

        previousPlayerPosition = player.transform.position;
        previousTeleportVersion = player.TeleportVersion;
    }

    private void CheckPlayerCollision(Vector3 previousBonusPosition)
    {
        Vector3 playerPosition = player.transform.position;
        Vector3 localPlayerPosition = gridOrigin.InverseTransformPoint(playerPosition);

        if (!levelBounds.Contains(localPlayerPosition))
        {
            return;
        }

        Vector2 relativeEnd = activeBonus.transform.position - playerPosition;
        Vector2 relativeStart;

        if (previousTeleportVersion == player.TeleportVersion)
        {
            relativeStart = previousBonusPosition - previousPlayerPosition;
        }
        else
        {
            relativeStart = relativeEnd;
        }

        Vector2 motion = relativeEnd - relativeStart;
        float fraction;

        if (motion.sqrMagnitude > 0f)
        {
            float closestPoint = -Vector2.Dot(relativeStart, motion);
            fraction = Mathf.Clamp01(closestPoint / motion.sqrMagnitude);
        }
        else
        {
            fraction = 0f;
        }

        float playerRadius;

        if (playerCollider != null)
        {
            playerRadius = Mathf.Min(
                playerCollider.bounds.extents.x,
                playerCollider.bounds.extents.y
            );
        }
        else
        {
            playerRadius = 0f;
        }

        float bonusRadius = Mathf.Min(
            activeBonus.bounds.extents.x,
            activeBonus.bounds.extents.y
        );

        float radius = bonusRadius + playerRadius;
        Vector2 closestPosition = relativeStart + motion * fraction;

        if (closestPosition.sqrMagnitude > radius * radius)
        {
            return;
        }

        activeBonus.gameObject.SetActive(false);
        Destroy(activeBonus.gameObject);

        activeBonus = null;
        hadActiveBonus = false;
        spawnElapsed = 0f;

        if (hud != null)
        {
            hud.AddScore(100);
        }

        if (collectAudio != null && collectClip != null)
        {
            collectAudio.PlayOneShot(collectClip);
        }
    }

    private void AdvanceBonus(float deltaTime)
    {
        float remainingTime = deltaTime;

        if (activeBonus == null)
        {
            if (hadActiveBonus)
            {
                hadActiveBonus = false;
                spawnElapsed = 0f;
                return;
            }

            float waitingTime = Mathf.Min(
                remainingTime,
                SpawnDelay - spawnElapsed
            );

            spawnElapsed = spawnElapsed + waitingTime;
            remainingTime = remainingTime - waitingTime;

            if (spawnElapsed < SpawnDelay)
            {
                return;
            }

            SpawnBonus();
        }

        movementElapsed = movementElapsed + remainingTime;

        Vector3 localPosition = Vector3.Lerp(
            startPosition,
            endPosition,
            movementElapsed / movementDuration
        );

        activeBonus.transform.position = gridOrigin.TransformPoint(localPosition);

        if (movementElapsed >= movementDuration)
        {
            Destroy(activeBonus.gameObject);

            activeBonus = null;
            hadActiveBonus = false;
            spawnElapsed = 0f;
        }
    }

    private void SpawnBonus()
    {
        float spriteRadius = bonusPrefab.sprite.bounds.extents.magnitude;

        float scale = Mathf.Max(
            Mathf.Abs(bonusPrefab.transform.localScale.x),
            Mathf.Abs(bonusPrefab.transform.localScale.y)
        );

        float margin = spriteRadius * scale + 0.0625f;
        Rect spawnBounds = ExpandBounds(levelBounds, margin);

        float perimeterPosition = Random.value;

        if (Mathf.Abs(perimeterPosition - previousPerimeterPosition) < 0.001f)
        {
            perimeterPosition = Mathf.Repeat(perimeterPosition + 0.5f, 1f);
        }

        previousPerimeterPosition = perimeterPosition;
        startPosition = PointOnPerimeter(spawnBounds, perimeterPosition);

        Vector2 direction = levelBounds.center - (Vector2)startPosition;
        direction = direction.normalized;

        Rect cameraBounds = CameraBoundsInGrid();

        float xMin = Mathf.Min(levelBounds.xMin, cameraBounds.xMin);
        float yMin = Mathf.Min(levelBounds.yMin, cameraBounds.yMin);
        float xMax = Mathf.Max(levelBounds.xMax, cameraBounds.xMax);
        float yMax = Mathf.Max(levelBounds.yMax, cameraBounds.yMax);

        Rect combinedBounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        Rect exitBounds = ExpandBounds(combinedBounds, margin);

        float exitDistance = DistanceToEdge(
            exitBounds,
            levelBounds.center,
            direction
        );

        endPosition = levelBounds.center + direction * exitDistance;

        Vector3 worldStart = gridOrigin.TransformPoint(startPosition);
        Vector3 worldEnd = gridOrigin.TransformPoint(endPosition);

        movementDuration = Vector3.Distance(worldStart, worldEnd) / moveSpeed;
        movementElapsed = 0f;

        activeBonus = Instantiate(bonusPrefab, transform);
        activeBonus.name = "MovingBonus_Popcorn";
        activeBonus.transform.position = gridOrigin.TransformPoint(startPosition);
        activeBonus.sortingLayerID = 0;
        activeBonus.sortingOrder = BonusSortingOrder;
        activeBonus.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        hadActiveBonus = true;
    }

    private void CreateVisibilityMask()
    {
        Texture2D texture = Texture2D.whiteTexture;

        Rect textureBounds = new Rect(
            0f,
            0f,
            texture.width,
            texture.height
        );

        Vector2 pivot = new Vector2(0.5f, 0.5f);

        maskSprite = Sprite.Create(texture, textureBounds, pivot, 1f);
        maskSprite.name = "BonusMapBoundsMask";

        GameObject maskObject = new GameObject("BonusVisibilityMask");

        maskObject.transform.SetParent(gridOrigin, false);
        maskObject.transform.localPosition = levelBounds.center;

        float scaleX = levelBounds.width / maskSprite.bounds.size.x;
        float scaleY = levelBounds.height / maskSprite.bounds.size.y;

        maskObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        visibilityMask = maskObject.AddComponent<SpriteMask>();
        visibilityMask.sprite = maskSprite;
        visibilityMask.alphaCutoff = 0.5f;
        visibilityMask.isCustomRangeActive = true;
        visibilityMask.backSortingLayerID = 0;
        visibilityMask.frontSortingLayerID = 0;
        visibilityMask.backSortingOrder = BonusSortingOrder - 1;
        visibilityMask.frontSortingOrder = BonusSortingOrder;
    }

    private Rect CameraBoundsInGrid()
    {
        Vector3 cameraOffset = gridOrigin.position - gameCamera.transform.position;

        float depth = Vector3.Dot(
            cameraOffset,
            gameCamera.transform.forward
        );

        Vector3 lowerWorld = gameCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, depth)
        );

        Vector3 upperWorld = gameCamera.ViewportToWorldPoint(
            new Vector3(1f, 1f, depth)
        );

        Vector3 lower = gridOrigin.InverseTransformPoint(lowerWorld);
        Vector3 upper = gridOrigin.InverseTransformPoint(upperWorld);

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

    private static Vector2 PointOnPerimeter(Rect bounds, float position)
    {
        float distance = position * 2f * (bounds.width + bounds.height);

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
        Vector2 direction
    )
    {
        float horizontal;

        if (Mathf.Abs(direction.x) < 0.000001f)
        {
            horizontal = float.PositiveInfinity;
        }
        else
        {
            float edgeX;

            if (direction.x > 0f)
            {
                edgeX = bounds.xMax;
            }
            else
            {
                edgeX = bounds.xMin;
            }

            horizontal = (edgeX - origin.x) / direction.x;
        }

        float vertical;

        if (Mathf.Abs(direction.y) < 0.000001f)
        {
            vertical = float.PositiveInfinity;
        }
        else
        {
            float edgeY;

            if (direction.y > 0f)
            {
                edgeY = bounds.yMax;
            }
            else
            {
                edgeY = bounds.yMin;
            }

            vertical = (edgeY - origin.y) / direction.y;
        }

        return Mathf.Min(horizontal, vertical);
    }

    private void OnEnable()
    {
        if (visibilityMask != null)
        {
            visibilityMask.enabled = true;
        }
    }

    private void OnDisable()
    {
        if (activeBonus != null)
        {
            Destroy(activeBonus.gameObject);
        }

        activeBonus = null;
        hadActiveBonus = false;
        spawnElapsed = 0f;

        if (visibilityMask != null)
        {
            visibilityMask.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (visibilityMask != null)
        {
            Destroy(visibilityMask.gameObject);
        }

        if (maskSprite != null)
        {
            Destroy(maskSprite);
        }
    }
}
