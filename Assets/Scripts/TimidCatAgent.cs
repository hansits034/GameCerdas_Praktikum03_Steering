using UnityEngine;

public class TimidCatAgent : MonoBehaviour
{
    [Header("Target")]

    [SerializeField]
    private Transform target;

    [Header("Movement")]

    [SerializeField]
    private float maxSpeed = 4f;

    [SerializeField]
    private float maxAcceleration = 8f;

    [SerializeField]
    private float turnSpeed = 8f;

    [Header("Wander")]

    [SerializeField]
    private float wanderSpeed = 2.5f;

    [SerializeField]
    private float wanderChangeInterval = 1.5f;

    [SerializeField]
    private float wanderAngleChange = 45f;

    [Header("Flee")]

    [SerializeField]
    private float fleeRadius = 3f;

    [SerializeField]
    private float fleeSpeed = 6f;

    [Header("Separation")]

    [SerializeField]
    private LayerMask agentMask;

    [SerializeField]
    private float separationRadius = 1.5f;

    [SerializeField]
    private float separationWeight = 1.5f;

    [Header("Obstacle Avoidance")]

    [SerializeField]
    private CatSensor sensor;

    [SerializeField]
    private float avoidanceWeight = 2.5f;

    [Header("Debug")]

    [SerializeField]
    private Renderer _renderer;
    [SerializeField]

    private Vector3 velocity;

    private Vector3 wanderDirection;

    private float wanderTimer;

    public Vector3 Velocity => velocity;


    private void Start()
    {
        _renderer = GetComponent<Renderer>();
        wanderDirection = transform.forward;
        wanderTimer = wanderChangeInterval;
    }

    private void Update()
    {
        Vector3 desiredVelocity;

        
        bool tooCloseToTarget = (target.position - transform.position).magnitude < fleeRadius && sensor.DetectPlayer(target.position);

        if (tooCloseToTarget)
        {
            desiredVelocity = CalculateFlee();
        }
        else
        {
            desiredVelocity = CalculateWander();
        }

        Vector3 separationVelocity = CalculateSeparation();
        if (separationVelocity.sqrMagnitude > 0.001f)
        {
            desiredVelocity += separationVelocity * separationWeight;
        }

        desiredVelocity =
            ApplyObstacleAvoidance(desiredVelocity);

        velocity =
            Vector3.MoveTowards(
                velocity,
                desiredVelocity,
                maxAcceleration * Time.deltaTime
            );

        // Flee is allowed to exceed maxSpeed so the animal
        // can actually outrun something chasing it.
        velocity =
            Vector3.ClampMagnitude(
                velocity,
                Mathf.Max(maxSpeed, fleeSpeed)
            );

        ApplyMovement();

        UpdateRotation();
    }

    private Vector3 CalculateWander()
    {
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            float randomAngle =
                Random.Range(
                    -wanderAngleChange,
                    wanderAngleChange
                );

            wanderDirection =
                Quaternion.Euler(
                    0f,
                    randomAngle,
                    0f
                ) * transform.forward;

            wanderDirection.y = 0f;
            wanderDirection.Normalize();

            wanderTimer = wanderChangeInterval;

            if (_renderer != null)
            {
                _renderer.material.color = Color.blue;
            }
        }

        return wanderDirection * wanderSpeed;
    }

    private Vector3 CalculateFlee()
    {
        Vector3 toTarget =
            target.position - transform.position;

        toTarget.y = 0f;

        if (_renderer != null)
        {
            _renderer.material.color = Color.red;
        }

        return -toTarget.normalized * fleeSpeed;
    }

    private Vector3 ApplyObstacleAvoidance(
        Vector3 desiredVelocity)
    {
        if (sensor == null)
        {
            return desiredVelocity;
        }

        Vector3 checkDirection =
            desiredVelocity.sqrMagnitude > 0.001f
                ? desiredVelocity.normalized
                : transform.forward;

        Vector3 avoidanceDirection =
            sensor.GetAvoidanceDirection(
                checkDirection
            );

        if (avoidanceDirection.sqrMagnitude > 0.001f)
        {
            Vector3 combinedDirection =
                checkDirection +
                avoidanceDirection *
                avoidanceWeight;

            combinedDirection.y = 0f;

            if (combinedDirection.sqrMagnitude > 0.001f)
            {
                combinedDirection.Normalize();
            }

            float desiredSpeed =
                Mathf.Max(
                    desiredVelocity.magnitude,
                    wanderSpeed
                );

            return combinedDirection * desiredSpeed;
        }

        return desiredVelocity;
    }

    private void ApplyMovement()
    {
        transform.position +=
            velocity * Time.deltaTime;
    }

    private void UpdateRotation()
    {
        Vector3 horizontalVelocity = velocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                horizontalVelocity.normalized
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            fleeRadius
        );

        if (target != null)
        {
            Gizmos.DrawLine(
                transform.position,
                target.position
            );
        }
    }

    private Vector3 CalculateSeparation()
    {
        Collider[] neighbors =
            Physics.OverlapSphere(
                transform.position,
                separationRadius,
                agentMask
            );

        Vector3 separation =
            Vector3.zero;

        int count = 0;

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.transform == transform)
            {
                continue;
            }

            Vector3 away =
                transform.position -
                neighbor.transform.position;

            away.y = 0f;

            float sqrDistance =
                away.sqrMagnitude;

            if (sqrDistance > 0.001f)
            {
                separation +=
                    away.normalized /
                    Mathf.Max(sqrDistance, 0.01f);

                count++;
            }
        }

        if (count > 0)
        {
            separation /= count;
        }

        return separation;
    }
}
