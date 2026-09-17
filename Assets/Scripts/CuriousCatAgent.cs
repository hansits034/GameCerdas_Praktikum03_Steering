using UnityEngine;

public class CuriousCatAgent : MonoBehaviour
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
    private float stopRadius = 4f;

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
    private CuriousCatSensor sensor;

    [SerializeField]
    private float avoidanceWeight = 2.5f;

    private Vector3 velocity;

    private Vector3 wanderDirection;

    private float wanderTimer;

    public Vector3 Velocity => velocity;

    [Header("Panic / Flee")]
    [SerializeField]
    private float fleeSpeed = 10f;
    [SerializeField]
    private float panicRadius = 1.5f;
    [SerializeField]
    private float calmDownRadius = 10f;
    [SerializeField]
    private bool isPanicked = false;
    
    [Header("Pursue")]
    [SerializeField]
    private Vector3 lastKnownPlayerPosition;
    [SerializeField]
    private Vector3 lastKnownPlayerVelocity;
    [SerializeField]
    private float lastSeenTime;
    [SerializeField]
    private float pursueRadius = 2f;
    [SerializeField]
    private float MaxPredictionTime = 2f;
    [SerializeField]
    private bool finishedPursue = true;



    [SerializeField]
    private Renderer _renderer;
    
    private void Start()
    {
        // lastKnownPlayerPosition = target.position;
        _renderer = GetComponent<Renderer>();
        wanderDirection = transform.forward;
        wanderTimer = wanderChangeInterval;
    }

    private void Update()
    {
        Vector3 desiredVelocity;

        if (useTarget && target != null)
        {
            if(sensor.DetectPlayer(target.position)) {
                if (((target.position - transform.position).magnitude <= panicRadius ) || isPanicked)
                {
                    Debug.Log("Fleeing from player");
                    desiredVelocity = CalculateFlee();
                }
                else
                {
                    Debug.Log("Going to player");
                    desiredVelocity = CalculateArrive();
                }
            } else if(finishedPursue) {
                Debug.Log("Wandering");
                desiredVelocity = CalculateWander();
            } else {
                Debug.Log("Pursuing player");
                desiredVelocity = CalculatePursue();
            }
        }
        else {
            // If no target is available, wander around
            Debug.Log("Wandering");
            desiredVelocity = CalculateWander();
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

        finishedPursue = false;

        lastKnownPlayerPosition = target.position;
        lastKnownPlayerVelocity = target.GetComponent<SimplePlayerController>().Velocity;
        lastSeenTime = Time.time;

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

        float desiredSpeed = fleeSpeed;

        // 73. Level 2 - Flee
        if(distance <= calmDownRadius)
        {
            isPanicked = true;
            _renderer.material.color = Color.blue;
            return -toTarget.normalized * desiredSpeed;
        }
        Debug.Log("Calm down");

        isPanicked = false;
        return CalculateWander();
    }

    private Vector3 CalculatePursue()
    {
        float elapse = Mathf.Min(Time.time - lastSeenTime, MaxPredictionTime);
        Vector3 predictedPosition =
            lastKnownPlayerPosition + lastKnownPlayerVelocity * elapse;

        Vector3 toTarget =
            predictedPosition - transform.position;

        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        if (distance <= pursueRadius)
        {
            finishedPursue = true;
            return CalculateWander();
        }

        _renderer.material.color = Color.pink;
        float desiredSpeed = maxSpeed;

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