using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform bowlingBall;
    private Vector3 offset;

    void Start()
    {
        if (bowlingBall == null)
        {
            bowlingBall = FindAnyObjectByType<BowlingBall>()?.transform;
        }
        offset = new Vector3(0, 5, -7);
    }

    void LateUpdate()
    {
        if (bowlingBall != null)
        {
            Vector3 targetPosition = bowlingBall.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f);
            transform.LookAt(bowlingBall.position + Vector3.forward * 10);
        }
    }
} 