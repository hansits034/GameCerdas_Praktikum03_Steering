using UnityEngine;

[RequireComponent(typeof(SteeringAgent))]
public class SteeringAnimator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Pilih model 3D (child) yang memiliki komponen Animator")]
    [SerializeField] private Animator modelAnimator;

    [Header("Animation Settings")]
    [Tooltip("Nama parameter float di Animator (contoh: Vert, Speed, dll)")]
    [SerializeField] private string speedParameterName = "Vert";

    private SteeringAgent agent;

    private void Awake()
    {
        agent = GetComponent<SteeringAgent>();
    }

    private void Update()
    {
        if (agent == null || modelAnimator == null) return;

        float currentSpeed = agent.Velocity.magnitude;

        modelAnimator.SetFloat(speedParameterName, currentSpeed);
    }
}