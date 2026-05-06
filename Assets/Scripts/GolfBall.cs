using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class GolfBall : MonoBehaviour
{
    [SerializeField] private float maxVelocity = 50f;
    [SerializeField] private float stopThreshold = 0.05f;

    private Rigidbody rb;
    private SphereCollider sc;

    public Vector3 LastHitPosition { get; private set; }
    public bool IsInWater { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sc = GetComponent<SphereCollider>();
        LastHitPosition = transform.position;
        ConfigureRigidbody();
        ConfigureCollider();
    }

    void ConfigureRigidbody()
    {
        rb.mass = 0.045f;
        rb.drag = 0.3f;
        rb.angularDrag = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void ConfigureCollider()
    {
        PhysicMaterial mat = new PhysicMaterial("GolfBallPhysic")
        {
            bounciness = 0.5f,
            dynamicFriction = 0.35f,
            staticFriction = 0.35f,
            frictionCombine = PhysicMaterialCombine.Average,
            bounceCombine = PhysicMaterialCombine.Average
        };
        sc.material = mat;
        sc.radius = 0.5f;
    }

    public void HitBall(Vector3 direction, float power)
    {
        SaveHitPosition();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(direction.normalized * power, ForceMode.Impulse);
    }

    public void SaveHitPosition()
    {
        LastHitPosition = transform.position;
    }

    public bool IsMoving()
    {
        return rb.velocity.magnitude > stopThreshold;
    }

    public Vector3 GetVelocity()
    {
        return rb.velocity;
    }

    public void ResetToNewPosition(Vector3 pos)
    {
        LastHitPosition = pos;
        IsInWater = false;
        transform.position = pos + Vector3.up * 0.15f;
        ResetVelocity();
    }

    public void ResetVelocity()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void TriggerWaterReset()
    {
        if (!IsInWater)
        {
            IsInWater = true;
            StartCoroutine(WaterResetRoutine());
        }
    }

    IEnumerator WaterResetRoutine()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        yield return new WaitForSeconds(1f);

        transform.position = LastHitPosition + Vector3.up * 0.5f;
        ResetVelocity();
        rb.isKinematic = false;
        IsInWater = false;
    }
}
