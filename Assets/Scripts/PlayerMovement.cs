using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Transform[] points;
    public float speed = 2f;

    private int currentPoint = 0;
    private Animator animator;


    void Start()
    {
        animator = GetComponent<Animator>();
    }


    void Update()
    {
        Transform target = points[currentPoint];

        Vector2 direction = target.position - transform.position;


        if (direction.x > 0.01f)
        {
            animator.Play("right");
        }
        else if (direction.x < -0.01f)
        {
            animator.Play("left");
        }
        else if (direction.y > 0.01f)
        {
            animator.Play("back");
        }
        else if (direction.y < -0.01f)
        {
            animator.Play("player-walk");
        }

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );



    float distance = Vector2.Distance(
    transform.position,
    target.position
);

if (distance < 0.05f)
        {
            transform.position = target.position;

            currentPoint = currentPoint + 1;

            if (currentPoint >= points.Length)
            {
                currentPoint = 0;
            }
        }
    }
}