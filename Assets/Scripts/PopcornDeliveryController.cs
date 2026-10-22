using UnityEngine;
using TMPro;

[DefaultExecutionOrder(100)]
public class PopcornDeliveryController : MonoBehaviour
{
    [SerializeField] private PacStudentController player;
    [SerializeField] private InGameHUDController hud;
    [SerializeField] private SpriteRenderer[] pickups;
    [SerializeField] private Transform deliveryPoint;
    [SerializeField] private RectTransform homeMarker;
    [SerializeField] private TMP_Text carryText;
    [SerializeField] private TMP_Text deliveredText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private SpriteRenderer carriedPopcorn;
    [SerializeField] private AudioClip collectClip;
    [SerializeField, Min(1)] private int capacity = 3;
    [SerializeField, Min(1)] private int deliveryTarget = 9;
    [SerializeField, Min(0.1f)] private float refillDelay = 6f;

    public int Carried { get; private set; }
    public int Delivered { get; private set; }
    private float[] refillTimers;
    private int previousLives;
    private int previousTeleport;
    private Vector3 previousPosition;
    private AudioSource pickupAudio;
    private float noticeTime;
    private Camera gameCamera;

    private void Start()
    {
        refillTimers = new float[pickups.Length];
        previousLives = hud.Lives;
        previousTeleport = player.TeleportVersion;
        previousPosition = player.transform.position;
        pickupAudio = gameObject.AddComponent<AudioSource>();
        pickupAudio.playOnAwake = false;
        pickupAudio.spatialBlend = 0f;
        gameCamera = Camera.main;
        Refresh();
        ShowNotice("COLLECT\n& RETURN", 0f);
    }

    private void LateUpdate()
    {
        Vector2 anchor = gameCamera.WorldToViewportPoint(deliveryPoint.position);
        homeMarker.anchorMin = homeMarker.anchorMax = anchor;
        homeMarker.anchoredPosition = Vector2.zero;
    }

    private void Update()
    {
        AdvanceDelivery(Time.deltaTime);
    }

    private void AdvanceDelivery(float deltaTime)
    {
        Vector3 position = player.transform.position;
        Vector3 from = previousTeleport == player.TeleportVersion ? previousPosition : position;
        previousPosition = position;
        previousTeleport = player.TeleportVersion;
        if (hud.Lives < previousLives)
        {
            int lost = Carried;
            Carried = 0;
            Refresh();
            if (lost > 0) ShowNotice("CARGO LOST", 3f);
        }
        previousLives = hud.Lives;
        if (!player.CanInteract) return;
        noticeTime = Mathf.Max(0f, noticeTime - deltaTime);
        for (int index = 0; index < pickups.Length; index++)
        {
            if (!pickups[index].gameObject.activeSelf)
            {
                refillTimers[index] -= deltaTime;
                if (refillTimers[index] <= 0f) pickups[index].gameObject.SetActive(true);
            }
        }
        float radius = Vector3.Distance(player.CellToWorld(Vector2Int.zero), player.CellToWorld(Vector2Int.right)) * 0.3f;
        if (Carried > 0 && Touches(from, position, deliveryPoint.position, radius))
        {
            int amount = Mathf.Min(Carried, deliveryTarget - Delivered);
            Delivered += amount;
            Carried = 0;
            hud.AddScore(amount * 100);
            PlayCollect();
            Refresh();
            ShowNotice("DELIVERED +" + amount, 2f);
            if (Delivered >= deliveryTarget)
            {
                hud.CompleteDelivery();
                return;
            }
        }
        for (int index = 0; index < pickups.Length && Carried < capacity; index++)
        {
            SpriteRenderer pickup = pickups[index];
            if (!pickup.gameObject.activeSelf || !Touches(from, position, pickup.transform.position, radius)) continue;
            pickup.gameObject.SetActive(false);
            refillTimers[index] = refillDelay;
            Carried++;
            PlayCollect();
            Refresh();
            ShowNotice(Carried == capacity ? "BAG FULL!" : "POPCORN +1", 1.5f);
        }
        if (noticeTime <= 0f)
            statusText.text = Carried == capacity ? "BAG FULL!\nRETURN HOME" : Carried > 0 ? "RETURN HOME\nTO BANK CARGO" : "COLLECT\nPOPCORN";
    }

    private static bool Touches(Vector3 start, Vector3 end, Vector3 point, float radius)
    {
        Vector2 motion = end - start;
        Vector2 offset = point - start;
        float fraction = motion.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(offset, motion) / motion.sqrMagnitude) : 0f;
        return (offset - motion * fraction).sqrMagnitude <= radius * radius;
    }

    private void Refresh()
    {
        carryText.text = Carried + " / " + capacity;
        deliveredText.text = Delivered + " / " + deliveryTarget;
        carriedPopcorn.enabled = Carried > 0;
    }

    private void ShowNotice(string message, float duration)
    {
        statusText.text = message;
        noticeTime = duration;
    }

    private void PlayCollect()
    {
        if (collectClip != null) pickupAudio.PlayOneShot(collectClip);
    }
}
