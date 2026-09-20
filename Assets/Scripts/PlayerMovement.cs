using UnityEngine;

public class PacStudentMovement : MonoBehaviour
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
        if (points.Length == 0)
            return;


        Transform target = points[currentPoint];


        // calculate movement direction
        Vector2 direction = target.position - transform.position;


        // play correct animation direction
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


        // linear movement with frame-rate independence
        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );


        // reach next point
        if (Vector2.Distance(transform.position, target.position) < 0.05f)
        {
            transform.position = target.position;

            currentPoint++;

            if (currentPoint >= points.Length)
            {
                currentPoint = 0;
            }
        }
    }
}