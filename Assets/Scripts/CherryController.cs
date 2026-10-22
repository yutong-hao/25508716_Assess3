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
            gameCamera == null || moveSpeed <= 0f || levelBounds.width <= 0f || levelBounds.height <= 0f)
        {
            Debug.LogError("Bonus needs its popcorn prefab, grid origin, camera, positive map bounds and movement speed.", this);
            enabled = false;
            return;
        }

        CreateVisibilityMask();
        collectAudio = gameObject.AddComponent<AudioSource>();
        collectAudio.playOnAwake = false;
        collectAudio.spatialBlend = 0f;
        if (player != null) playerCollider = player.GetComponent<Collider2D>();
        RememberPlayerPosition();
    }

    private void Update()
    {
        if (hud.IsGameOver) return;
        SpriteRenderer previousBonus = activeBonus;
        Vector3 previousBonusPosition = previousBonus != null ? previousBonus.transform.position : Vector3.zero;
        AdvanceBonus(Time.deltaTime);
        if (activeBonus != null && player != null && player.CanInteract)
            CheckPlayerCollision(previousBonus == activeBonus ? previousBonusPosition : activeBonus.transform.position);
        RememberPlayerPosition();
    }

    private void RememberPlayerPosition()
    {
        if (player == null) return;
        previousPlayerPosition = player.transform.position;
        previousTeleportVersion = player.TeleportVersion;
    }

    private void CheckPlayerCollision(Vector3 previousBonusPosition)
    {
        Vector3 playerPosition = player.transform.position;
        if (!levelBounds.Contains(gridOrigin.InverseTransformPoint(playerPosition))) return;
        Vector2 relativeEnd = activeBonus.transform.position - playerPosition;
        Vector2 relativeStart = previousTeleportVersion == player.TeleportVersion
            ? previousBonusPosition - previousPlayerPosition : relativeEnd;
        Vector2 motion = relativeEnd - relativeStart;
        float fraction = motion.sqrMagnitude > 0f
            ? Mathf.Clamp01(-Vector2.Dot(relativeStart, motion) / motion.sqrMagnitude) : 0f;
        float playerRadius = playerCollider != null
            ? Mathf.Min(playerCollider.bounds.extents.x, playerCollider.bounds.extents.y) : 0f;
        float radius = Mathf.Min(activeBonus.bounds.extents.x, activeBonus.bounds.extents.y) + playerRadius;
        if ((relativeStart + motion * fraction).sqrMagnitude > radius * radius) return;
        activeBonus.gameObject.SetActive(false);
        Destroy(activeBonus.gameObject);
        activeBonus = null;
        hadActiveBonus = false;
        spawnElapsed = 0f;
        if (hud != null) hud.AddScore(100);
        if (collectAudio != null && collectClip != null) collectAudio.PlayOneShot(collectClip);
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

            float waitingTime = Mathf.Min(remainingTime, SpawnDelay - spawnElapsed);
            spawnElapsed += waitingTime;
            remainingTime -= waitingTime;
            if (spawnElapsed < SpawnDelay) return;
            SpawnBonus();
        }

        movementElapsed += remainingTime;
        activeBonus.transform.position = gridOrigin.TransformPoint(
            Vector3.Lerp(startPosition, endPosition, movementElapsed / movementDuration));
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
        float margin = bonusPrefab.sprite.bounds.extents.magnitude *
            Mathf.Max(Mathf.Abs(bonusPrefab.transform.localScale.x), Mathf.Abs(bonusPrefab.transform.localScale.y)) + 0.0625f;
        Rect spawnBounds = ExpandBounds(levelBounds, margin);
        float perimeterPosition = Random.value;
        if (Mathf.Abs(perimeterPosition - previousPerimeterPosition) < 0.001f)
            perimeterPosition = Mathf.Repeat(perimeterPosition + 0.5f, 1f);
        previousPerimeterPosition = perimeterPosition;
        startPosition = PointOnPerimeter(spawnBounds, perimeterPosition);
        Vector2 direction = (levelBounds.center - (Vector2)startPosition).normalized;
        Rect cameraBounds = CameraBoundsInGrid();
        Rect exitBounds = ExpandBounds(Rect.MinMaxRect(
            Mathf.Min(levelBounds.xMin, cameraBounds.xMin), Mathf.Min(levelBounds.yMin, cameraBounds.yMin),
            Mathf.Max(levelBounds.xMax, cameraBounds.xMax), Mathf.Max(levelBounds.yMax, cameraBounds.yMax)), margin);
        endPosition = levelBounds.center + direction * DistanceToEdge(exitBounds, levelBounds.center, direction);
        movementDuration = Vector3.Distance(gridOrigin.TransformPoint(startPosition),
            gridOrigin.TransformPoint(endPosition)) / moveSpeed;
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
        maskSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), 1f);
        maskSprite.name = "BonusMapBoundsMask";
        GameObject maskObject = new GameObject("BonusVisibilityMask");
        maskObject.transform.SetParent(gridOrigin, false);
        maskObject.transform.localPosition = levelBounds.center;
        maskObject.transform.localScale = new Vector3(
            levelBounds.width / maskSprite.bounds.size.x, levelBounds.height / maskSprite.bounds.size.y, 1f);
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
        float depth = Vector3.Dot(gridOrigin.position - gameCamera.transform.position, gameCamera.transform.forward);
        Vector3 lower = gridOrigin.InverseTransformPoint(gameCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth)));
        Vector3 upper = gridOrigin.InverseTransformPoint(gameCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depth)));
        return Rect.MinMaxRect(Mathf.Min(lower.x, upper.x), Mathf.Min(lower.y, upper.y),
            Mathf.Max(lower.x, upper.x), Mathf.Max(lower.y, upper.y));
    }

    private static Rect ExpandBounds(Rect bounds, float margin)
    {
        return Rect.MinMaxRect(bounds.xMin - margin, bounds.yMin - margin, bounds.xMax + margin, bounds.yMax + margin);
    }

    private static Vector2 PointOnPerimeter(Rect bounds, float position)
    {
        float distance = position * 2f * (bounds.width + bounds.height);
        if (distance < bounds.width) return new Vector2(bounds.xMin + distance, bounds.yMin);
        distance -= bounds.width;
        if (distance < bounds.height) return new Vector2(bounds.xMax, bounds.yMin + distance);
        distance -= bounds.height;
        if (distance < bounds.width) return new Vector2(bounds.xMax - distance, bounds.yMax);
        distance -= bounds.width;
        return new Vector2(bounds.xMin, bounds.yMax - distance);
    }

    private static float DistanceToEdge(Rect bounds, Vector2 origin, Vector2 direction)
    {
        float horizontal = Mathf.Abs(direction.x) < 0.000001f ? float.PositiveInfinity
            : ((direction.x > 0f ? bounds.xMax : bounds.xMin) - origin.x) / direction.x;
        float vertical = Mathf.Abs(direction.y) < 0.000001f ? float.PositiveInfinity
            : ((direction.y > 0f ? bounds.yMax : bounds.yMin) - origin.y) / direction.y;
        return Mathf.Min(horizontal, vertical);
    }

    private void OnEnable()
    {
        if (visibilityMask != null) visibilityMask.enabled = true;
    }

    private void OnDisable()
    {
        if (activeBonus != null) Destroy(activeBonus.gameObject);
        activeBonus = null;
        hadActiveBonus = false;
        spawnElapsed = 0f;
        if (visibilityMask != null) visibilityMask.enabled = false;
    }

    private void OnDestroy()
    {
        if (visibilityMask != null) Destroy(visibilityMask.gameObject);
        if (maskSprite != null) Destroy(maskSprite);
    }
}
