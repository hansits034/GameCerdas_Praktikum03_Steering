using UnityEngine;

public class SteeringAgentChallenge : MonoBehaviour
{
    [Header("Target")]

    [SerializeField]
    private Transform target;

    [SerializeField]
    private bool useTarget = true;

    [Header("Movement")]

    [SerializeField]
    private float maxSpeed = 4f;

    [SerializeField]
    private float maxAcceleration = 8f;

    [SerializeField]
    private float turnSpeed = 8f;

    [Header("Arrive")]

    [SerializeField]
    private float slowRadius = 4f;

    [SerializeField]
    private float stopRadius = 1.5f;

    [Header("Wander")]

    [SerializeField]
    private float wanderSpeed = 2.5f;

    [SerializeField]
    private float wanderChangeInterval = 1.5f;

    [SerializeField]
    private float wanderAngleChange = 45f;

    // Step 56 //
    [Header("Separation")]

    [SerializeField]
    private LayerMask agentMask;

    [SerializeField]
    private float separationRadius = 1.5f;

    [SerializeField]
    private float separationWeight = 1.5f;
    // .. //

    [Header("Obstacle Avoidance")]

    [SerializeField]
    private SteeringSensor sensor;

    [SerializeField]
    private float avoidanceWeight = 2.5f;

    private Vector3 velocity;

    private Vector3 wanderDirection;

    private float wanderTimer;

    public Vector3 Velocity => velocity;

    [SerializeField]
    private Renderer _renderer;
    [SerializeField]
    private float panicRadius = 2f;
    [SerializeField]
    private Vector3 lastKnownPlayerPosition;

    [Header("Pursue")]
    [SerializeField]
    private float predictionTime = 2.5f;
    [SerializeField]
    private Vector3 lastTargetPosition;

    private void Start()
    {
        _renderer = GetComponent<Renderer>();
        wanderDirection = transform.forward;
        wanderTimer = wanderChangeInterval;
    }
    private void Update()
    {
        Vector3 desiredVelocity;

        if (useTarget && target != null)
        {
            desiredVelocity = CalculateArrive();
        }
        else if(!useTarget && target != null)
        {
            desiredVelocity = CalculatePursue(); 
        }
        else
        {
            // desiredVelocity = CalculateWander();
            desiredVelocity = CalculateFlee();
        }

        // Step 56 //
        Vector3 separationVelocity = CalculateSeparation();
        if (separationVelocity.sqrMagnitude > 0.001f)
        {
            desiredVelocity += separationVelocity * separationWeight;
        }
        // .. //

        desiredVelocity =
            ApplyObstacleAvoidance(desiredVelocity);

        velocity =
            Vector3.MoveTowards(
                velocity,
                desiredVelocity,
                maxAcceleration * Time.deltaTime
            );

        velocity =
            Vector3.ClampMagnitude(
                velocity,
                maxSpeed
            );

        ApplyMovement();

        UpdateRotation();
    }

    private Vector3 CalculateArrive()
    {
        Vector3 toTarget =
            target.position - transform.position;

        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        if (distance <= stopRadius)
        {
            return Vector3.zero;
        }

        float desiredSpeed = maxSpeed;

        // 73. Level 1
        _renderer.material.color = Color.red;
        if (distance < slowRadius)
        {
            float range =
                Mathf.Max(
                    slowRadius - stopRadius,
                    0.001f
                );

            float normalizedDistance =
                (distance - stopRadius) / range;

            desiredSpeed =
                maxSpeed *
                Mathf.Clamp01(normalizedDistance);
        }

        return toTarget.normalized * desiredSpeed;
    }

    private Vector3 CalculatePursue()
    {
        Vector3 targetVelocity = (target.position - lastTargetPosition) / Time.deltaTime;
        Vector3 predictedPosition =
            target.position +
            targetVelocity * predictionTime;

        Vector3 toTarget =
            predictedPosition - transform.position;

        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        if (distance <= stopRadius)
        {
            return Vector3.zero;
        }

        float desiredSpeed = maxSpeed;

        // 73. Level 1
        _renderer.material.color = Color.pink;
        if (distance < slowRadius)
        {
            float range =
                Mathf.Max(
                    slowRadius - stopRadius,
                    0.001f
                );

            float normalizedDistance =
                (distance - stopRadius) / range;

            desiredSpeed =
                maxSpeed *
                Mathf.Clamp01(normalizedDistance);
        }

        lastTargetPosition = target.position;
        return toTarget.normalized * desiredSpeed;
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
            // 73. Level 1
            _renderer.material.color = Color.blue;
        }

        return wanderDirection * wanderSpeed;
    }
    
    private Vector3 CalculateFlee()
    {
        Vector3 toTarget =
            target.position - transform.position;

        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        float desiredSpeed = maxSpeed;

        // 73. Level 2 - Flee
        if(distance <= panicRadius)
        {
            _renderer.material.color = Color.darkOrange;
            return -toTarget.normalized * desiredSpeed;
        }

        return CalculateWander();
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
            
            // 73. Level 1
            _renderer.material.color = Color.yellow;
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
            stopRadius
        );

        Gizmos.DrawWireSphere(
            transform.position,
            slowRadius
        );

        if (target != null)
        {
            Gizmos.DrawLine(
                transform.position,
                target.position
            );
        }
    }

    // Step 56 //
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
    // .. //
}