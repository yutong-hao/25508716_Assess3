using UnityEngine;
using TMPro;

[DefaultExecutionOrder(100)]
public class deliveryc : MonoBehaviour
{
    [SerializeField] private PacStudentController pc;
    [SerializeField] private hudc hud;
    [SerializeField] private SpriteRenderer[] pickups;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private RectTransform homeMark;
    [SerializeField] private TMP_Text carryTxt;
    [SerializeField] private TMP_Text deliveredTxt;
    [SerializeField] private TMP_Text statusTxt;
    [SerializeField] private SpriteRenderer carryCorn;
    [SerializeField] private AudioClip collectSfx;
    [SerializeField, Min(1)] private int cap = 3;
    [SerializeField, Min(1)] private int dropTarget = 9;
    [SerializeField, Min(0.1f)] private float refillWait = 6f;

    public int Carried { get; private set; }
    public int Delivered { get; private set; }
    private float[] refillTs;
    private int prevLives;
    private int prevTp;
    private Vector3 prevPos;
    private AudioSource pickupAud;
    private float noticeT;
    private Camera cam;

    private void Start()
    {
        refillTs = new float[pickups.Length];
        prevLives = hud.Lives;
        prevTp = pc.TpVer;
        prevPos = pc.transform.position;
        pickupAud = gameObject.AddComponent<AudioSource>();
        pickupAud.playOnAwake = false;
        pickupAud.spatialBlend = 0f;
        cam = Camera.main;
        Refresh();
        ShowNotice("COLLECT\n& RETURN", 0f);
    }

    private void LateUpdate()
    {
        Vector2 anchor = cam.WorldToViewportPoint(dropPoint.position);
        homeMark.anchorMin = homeMark.anchorMax = anchor;
        homeMark.anchoredPosition = Vector2.zero;
    }

    private void Update()
    {
        AdvanceDelivery(Time.deltaTime);
    }

    private void AdvanceDelivery(float dt)
    {
        Vector3 pos = pc.transform.position;
        Vector3 from = prevTp == pc.TpVer ? prevPos : pos;
        prevPos = pos;
        prevTp = pc.TpVer;
        if (hud.Lives < prevLives)
        {
            int lost = Carried;
            Carried = 0;
            Refresh();
            if (lost > 0) ShowNotice("CARGO LOST", 3f);
        }
        prevLives = hud.Lives;
        if (!pc.CanAct) return;
        noticeT = Mathf.Max(0f, noticeT - dt);
        for (int index = 0; index < pickups.Length; index++)
        {
            if (!pickups[index].gameObject.activeSelf)
            {
                refillTs[index] -= dt;
                if (refillTs[index] <= 0f) pickups[index].gameObject.SetActive(true);
            }
        }
        float radius = Vector3.Distance(pc.CellToWorld(Vector2Int.zero), pc.CellToWorld(Vector2Int.right)) * 0.3f;
        if (Carried > 0 && Touches(from, pos, dropPoint.position, radius))
        {
            int amount = Mathf.Min(Carried, dropTarget - Delivered);
            Delivered += amount;
            Carried = 0;
            hud.AddScore(amount * 100);
            PlayCollect();
            Refresh();
            ShowNotice("DELIVERED +" + amount, 2f);
            if (Delivered >= dropTarget)
            {
                hud.CompleteDelivery();
                return;
            }
        }
        for (int index = 0; index < pickups.Length && Carried < cap; index++)
        {
            SpriteRenderer pickup = pickups[index];
            if (!pickup.gameObject.activeSelf || !Touches(from, pos, pickup.transform.position, radius)) continue;
            pickup.gameObject.SetActive(false);
            refillTs[index] = refillWait;
            Carried++;
            PlayCollect();
            Refresh();
            ShowNotice(Carried == cap ? "BAG FULL!" : "POPCORN +1", 1.5f);
        }
        if (noticeT <= 0f)
            statusTxt.text = Carried == cap ? "BAG FULL!\nRETURN HOME" : Carried > 0 ? "RETURN HOME\nTO BANK CARGO" : "COLLECT\nPOPCORN";
    }

    private static bool Touches(Vector3 start, Vector3 end, Vector3 point, float radius)
    {
        Vector2 motion = end - start;
        Vector2 offset = point - start;
        float frac = motion.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(offset, motion) / motion.sqrMagnitude) : 0f;
        return (offset - motion * frac).sqrMagnitude <= radius * radius;
    }

    private void Refresh()
    {
        carryTxt.text = Carried + " / " + cap;
        deliveredTxt.text = Delivered + " / " + dropTarget;
        carryCorn.enabled = Carried > 0;
    }

    private void ShowNotice(string message, float dur)
    {
        statusTxt.text = message;
        noticeT = dur;
    }

    private void PlayCollect()
    {
        if (collectSfx != null) pickupAud.PlayOneShot(collectSfx);
    }
}
