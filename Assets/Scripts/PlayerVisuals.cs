using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    [SerializeField] private Vector3 shirtColor = new Vector3(0.18f, 0.32f, 0.55f);
    [SerializeField] private Vector3 pantsColor = new Vector3(0.55f, 0.45f, 0.30f);
    [SerializeField] private Vector3 skinColor = new Vector3(0.85f, 0.65f, 0.50f);
    [SerializeField] private Vector3 hairColor = new Vector3(0.15f, 0.10f, 0.08f);
    [SerializeField] private Vector3 shoeColor = new Vector3(0.20f, 0.15f, 0.10f);
    [SerializeField] private Vector3 shaftColor = new Vector3(0.62f, 0.62f, 0.66f);
    [SerializeField] private Vector3 clubHeadColor = new Vector3(0.30f, 0.20f, 0.10f);

    [SerializeField] private float backswingAngle = -110f;
    [SerializeField] private float swingDownAngle = 35f;
    [SerializeField] private float chargeRiseSpeed = 4f;
    [SerializeField] private float swingDownSpeed = 18f;
    [SerializeField] private float restoreSpeed = 6f;

    private Transform shoulderPivot;
    private Transform clubArm;
    private Transform leftArm;
    private Transform rightArm;
    private float currentClubAngle;
    private float targetClubAngle;
    private bool swinging;
    private float swingTimer;

    public enum SwingState { Idle, Charging, Swinging }
    private SwingState state = SwingState.Idle;

    void Awake()
    {
        BuildVisuals();
    }

    void BuildVisuals()
    {
        Material shirtMat = MakeMaterial("PlayerShirt", shirtColor);
        Material pantsMat = MakeMaterial("PlayerPants", pantsColor);
        Material skinMat = MakeMaterial("PlayerSkin", skinColor);
        Material hairMat = MakeMaterial("PlayerHair", hairColor);
        Material shoeMat = MakeMaterial("PlayerShoes", shoeColor);
        Material shaftMat = MakeMaterial("ClubShaft", shaftColor);
        Material clubHeadMat = MakeMaterial("ClubHead", clubHeadColor);

        Renderer rootRenderer = GetComponent<Renderer>();
        if (rootRenderer != null) rootRenderer.enabled = false;

        Collider rootCollider = GetComponent<Collider>();
        if (rootCollider != null && !(rootCollider is CharacterController))
        {
            Destroy(rootCollider);
        }

        GameObject visualRoot = new GameObject("Visuals");
        visualRoot.transform.SetParent(transform, false);
        visualRoot.transform.localPosition = Vector3.zero;

        AddPart(visualRoot.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.05f, 0f), new Vector3(0.7f, 0.55f, 0.45f), shirtMat);
        AddPart(visualRoot.transform, "Pelvis", PrimitiveType.Cube, new Vector3(0f, -0.55f, 0f), new Vector3(0.55f, 0.25f, 0.4f), pantsMat);
        AddPart(visualRoot.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.85f, 0f), new Vector3(0.55f, 0.55f, 0.55f), skinMat);
        AddPart(visualRoot.transform, "Hair", PrimitiveType.Sphere, new Vector3(0f, 1.0f, -0.05f), new Vector3(0.6f, 0.35f, 0.6f), hairMat);

        leftArm = AddPart(visualRoot.transform, "LeftArm", PrimitiveType.Cylinder, new Vector3(-0.4f, 0.15f, 0f), new Vector3(0.18f, 0.45f, 0.18f), shirtMat).transform;
        leftArm.localRotation = Quaternion.Euler(0f, 0f, 10f);

        GameObject rightShoulder = new GameObject("RightShoulder");
        rightShoulder.transform.SetParent(visualRoot.transform, false);
        rightShoulder.transform.localPosition = new Vector3(0.36f, 0.42f, 0f);
        shoulderPivot = rightShoulder.transform;

        rightArm = AddPart(rightShoulder.transform, "RightArm", PrimitiveType.Cylinder, new Vector3(0f, -0.45f, 0f), new Vector3(0.18f, 0.45f, 0.18f), shirtMat).transform;

        clubArm = new GameObject("ClubAttach").transform;
        clubArm.SetParent(rightShoulder.transform, false);
        clubArm.localPosition = new Vector3(0.0f, -0.85f, 0.05f);

        GameObject shaft = AddPart(clubArm, "ClubShaft", PrimitiveType.Cylinder, new Vector3(0f, -0.7f, 0f), new Vector3(0.05f, 0.7f, 0.05f), shaftMat);
        AddPart(shaft.transform, "ClubHead", PrimitiveType.Cube, new Vector3(0f, -1f, 0.05f), new Vector3(2.5f, 0.4f, 1.2f), clubHeadMat);

        AddPart(visualRoot.transform, "LeftLeg", PrimitiveType.Cylinder, new Vector3(-0.16f, -1.1f, 0f), new Vector3(0.2f, 0.5f, 0.2f), pantsMat);
        AddPart(visualRoot.transform, "RightLeg", PrimitiveType.Cylinder, new Vector3(0.16f, -1.1f, 0f), new Vector3(0.2f, 0.5f, 0.2f), pantsMat);

        AddPart(visualRoot.transform, "LeftShoe", PrimitiveType.Cube, new Vector3(-0.16f, -1.65f, 0.08f), new Vector3(0.22f, 0.12f, 0.4f), shoeMat);
        AddPart(visualRoot.transform, "RightShoe", PrimitiveType.Cube, new Vector3(0.16f, -1.65f, 0.08f), new Vector3(0.22f, 0.12f, 0.4f), shoeMat);

        currentClubAngle = 0f;
        targetClubAngle = 0f;
        ApplyClubAngle();
    }

    GameObject AddPart(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = localScale;

        Collider col = part.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer rend = part.GetComponent<Renderer>();
        if (rend != null) rend.sharedMaterial = mat;

        return part;
    }

    Material MakeMaterial(string name, Vector3 rgb)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material m = new Material(shader);
        m.name = name;
        m.color = new Color(rgb.x, rgb.y, rgb.z);
        return m;
    }

    void Update()
    {
        if (shoulderPivot == null) return;

        float dt = Time.deltaTime;

        switch (state)
        {
            case SwingState.Idle:
                targetClubAngle = 0f;
                currentClubAngle = Mathf.Lerp(currentClubAngle, targetClubAngle, restoreSpeed * dt);
                break;

            case SwingState.Charging:
                targetClubAngle = backswingAngle;
                currentClubAngle = Mathf.Lerp(currentClubAngle, targetClubAngle, chargeRiseSpeed * dt);
                break;

            case SwingState.Swinging:
                targetClubAngle = swingDownAngle;
                currentClubAngle = Mathf.Lerp(currentClubAngle, targetClubAngle, swingDownSpeed * dt);
                swingTimer += dt;
                if (swingTimer > 0.6f)
                {
                    state = SwingState.Idle;
                    swingTimer = 0f;
                }
                break;
        }

        ApplyClubAngle();
    }

    void ApplyClubAngle()
    {
        if (shoulderPivot == null) return;
        shoulderPivot.localRotation = Quaternion.Euler(currentClubAngle, 0f, 0f);
    }

    public void SetCharging(bool charging)
    {
        if (charging)
        {
            state = SwingState.Charging;
            swingTimer = 0f;
        }
        else if (state == SwingState.Charging)
        {
            state = SwingState.Idle;
        }
    }

    public void TriggerSwing()
    {
        state = SwingState.Swinging;
        swingTimer = 0f;
    }

    public void ShowClub(bool show)
    {
        if (clubArm == null) return;
        clubArm.gameObject.SetActive(show);
    }
}
