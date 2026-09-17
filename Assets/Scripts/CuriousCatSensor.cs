using UnityEngine;

public class CuriousCatSensor : MonoBehaviour
{
    [Header("Obstacle Sensor")]

    [SerializeField]
    private float obstacleDistance = 3f;

    [SerializeField]
    private float avoidanceSensorRadius = 0.5f;

    [SerializeField]
    private float sensorHeight = 0.5f;

    [SerializeField]
    private LayerMask obstacleMask;
    [SerializeField]
    private LayerMask playerMask;

    [Header("Avoidance")]

    [SerializeField]
    private float forwardBias = 0.5f;

    private bool obstacleDetected;
    private RaycastHit lastHit;

    public bool ObstacleDetected => obstacleDetected;

    public RaycastHit LastHit => lastHit;

    [Header("Player Sensor")]
    [SerializeField]
    private float playerDistance = 5f;

    public Vector3 GetAvoidanceDirection(
        Vector3 movementDirection)
    {
        obstacleDetected = false;

        if (movementDirection.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        movementDirection.Normalize();

        Vector3 origin =
            transform.position +
            Vector3.up * sensorHeight;

        if (Physics.SphereCast(
            origin,
            avoidanceSensorRadius,
            movementDirection,
            out lastHit,
            obstacleDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore))
        {
            obstacleDetected = true;

            Vector3 avoidDirection =
                Vector3.ProjectOnPlane(
                    lastHit.normal,
                    Vector3.up
                );

            avoidDirection.y = 0f;

            if (avoidDirection.sqrMagnitude > 0.001f)
            {
                avoidDirection.Normalize();
            }

            avoidDirection +=
                movementDirection * forwardBias;

            return avoidDirection.normalized;
        }

        return Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin =
            transform.position +
            Vector3.up * sensorHeight;

        Vector3 direction =
            transform.forward;

        Gizmos.DrawWireSphere(
            origin,
            avoidanceSensorRadius
        );

        Gizmos.DrawLine(
            origin,
            origin + direction * obstacleDistance
        );

        Gizmos.DrawWireSphere(
            origin + direction * obstacleDistance,
            avoidanceSensorRadius
        );
    }

    public bool DetectPlayer(Vector3 targetPosition)
    {
        Vector3 origin =
            transform.position +
            Vector3.up * sensorHeight;

        return Physics.Raycast(
            origin,
            targetPosition - origin,
            out lastHit,
            playerDistance,
            playerMask,
            QueryTriggerInteraction.Ignore);
    }
}