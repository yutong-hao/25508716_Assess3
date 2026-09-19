using UnityEngine;

public class PM : MonoBehaviour
{
    public float sp = 4f;

    private Rigidbody2D rb;
    private Animator animator;

    Vector2 mo;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        mo.x = Input.GetAxisRaw("Horizontal");
        mo.y = Input.GetAxisRaw("Vertical");

        animator.SetFloat("CatSpeed", mo.sqrMagnitude);
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + mo.normalized * sp * Time.fixedDeltaTime);
    }
}
