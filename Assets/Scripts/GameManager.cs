using UnityEngine;

public class PacStudentMovement : MonoBehaviour
{
    public Transform[] points;
    public float speed = 2f;

    private int currentPoint = 0;

    void Update()
    {
        if(points.Length == 0)
            return;

        Transform target = points[currentPoint];

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        if(Vector2.Distance(transform.position, target.position) < 0.05f)
        {
            currentPoint++;

            if(currentPoint >= points.Length)
            {
                currentPoint = 0;
            }
        }
    }
}