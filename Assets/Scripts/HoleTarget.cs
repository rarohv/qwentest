using UnityEngine;
using System.Collections;

public class HoleTarget : MonoBehaviour
{
    [SerializeField] private float holeRadius = 0.3f;
    [SerializeField] private float captureSpeed = 0.5f;

    private GolfBall golfBall;
    private GolfManager golfManager;
    private LevelManager levelManager;
    private AudioManager audioManager;
    private Transform flagPole;
    private Transform flagCloth;
    private bool ballCaptured;

    void Start()
    {
        CreateHoleVisuals();
        AutoLinkReferences();
    }

    void CreateHoleVisuals()
    {
        GameObject holeCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        holeCylinder.name = "HoleCup";
        holeCylinder.transform.parent = transform;
        holeCylinder.transform.localPosition = Vector3.zero;
        holeCylinder.transform.localScale = new Vector3(holeRadius * 2f, 0.05f, holeRadius * 2f);

        Renderer holeRenderer = holeCylinder.GetComponent<Renderer>();
        Material holeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        holeMat.color = new Color(0.05f, 0.05f, 0.05f);
        holeRenderer.material = holeMat;

        Collider holeCollider = holeCylinder.GetComponent<Collider>();
        if (holeCollider != null) Destroy(holeCollider);

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "FlagPole";
        pole.transform.parent = transform;
        pole.transform.localPosition = new Vector3(holeRadius, 1f, 0f);
        pole.transform.localScale = new Vector3(0.02f, 1f, 0.02f);

        Renderer poleRenderer = pole.GetComponent<Renderer>();
        Material poleMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        poleMat.color = new Color(0.8f, 0.8f, 0.8f);
        poleRenderer.material = poleMat;

        Collider poleCollider = pole.GetComponent<Collider>();
        if (poleCollider != null) Destroy(poleCollider);

        flagPole = pole.transform;

        GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flag.name = "FlagCloth";
        flag.transform.parent = pole.transform;
        flag.transform.localPosition = new Vector3(0.3f, 0.7f, 0f);
        flag.transform.localScale = new Vector3(0.6f, 0.3f, 0.01f);

        Renderer flagRenderer = flag.GetComponent<Renderer>();
        Material flagMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        flagMat.color = Color.red;
        flagRenderer.material = flagMat;

        Collider flagCollider = flag.GetComponent<Collider>();
        if (flagCollider != null) Destroy(flagCollider);

        flagCloth = flag.transform;

        GameObject teeBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        teeBox.name = "TeeBox";
        teeBox.transform.parent = transform;
        teeBox.transform.localPosition = new Vector3(0f, -0.02f, -2f);
        teeBox.transform.localScale = new Vector3(1f, 0.04f, 0.8f);

        Renderer teeRenderer = teeBox.GetComponent<Renderer>();
        Material teeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        teeMat.color = new Color(0.6f, 0.6f, 0.6f);
        teeRenderer.material = teeMat;
    }

    void AutoLinkReferences()
    {
        GameObject ballObj = GameObject.FindGameObjectWithTag("GolfBall");
        if (ballObj != null)
            golfBall = ballObj.GetComponent<GolfBall>();

        golfManager = FindObjectOfType<GolfManager>();
        levelManager = FindObjectOfType<LevelManager>();
        audioManager = FindObjectOfType<AudioManager>();
    }

    void Update()
    {
        if (golfBall == null)
        {
            GameObject ballObj = GameObject.FindGameObjectWithTag("GolfBall");
            if (ballObj != null)
                golfBall = ballObj.GetComponent<GolfBall>();
        }

        if (golfBall == null || ballCaptured) return;
        if (golfBall.IsMoving()) return;

        CheckBallInHole();
        AnimateFlag();
    }

    void CheckBallInHole()
    {
        float distXZ = Vector3.ProjectOnPlane(golfBall.transform.position - transform.position, Vector3.up).magnitude;
        float distY = golfBall.transform.position.y - transform.position.y;

        if (distXZ < holeRadius && distY < 0.3f && distY > -0.2f)
        {
            CaptureBall();
        }
    }

    void CaptureBall()
    {
        ballCaptured = true;
        Rigidbody ballRb = golfBall.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            ballRb.velocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            ballRb.isKinematic = true;
        }

        StartCoroutine(CaptureAnimation());
    }

    IEnumerator CaptureAnimation()
    {
        Vector3 startPos = golfBall.transform.position;
        Vector3 endPos = transform.position + Vector3.up * 0.02f;
        float t = 0f;

        while (t < 1f)
        {
            t += captureSpeed * Time.deltaTime;
            golfBall.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        if (audioManager != null)
            audioManager.PlayHoleSound();

        if (golfManager != null)
        {
            levelManager.RecordHoleStrokes(golfManager.StrokeCount);

            if (levelManager.IsLastHole)
            {
                golfManager.ShowHoleComplete(true);
            }
            else
            {
                golfManager.ShowHoleComplete(false);
            }
        }

        yield return new WaitForSeconds(3f);

        if (!levelManager.IsLastHole)
        {
            levelManager.AdvanceToNextHole();
            golfManager.LoadNextHole();
        }
    }

    void AnimateFlag()
    {
        if (flagCloth == null) return;
        Vector3 pos = flagCloth.localPosition;
        pos.x = 0.3f + Mathf.Sin(Time.time * 3f) * 0.03f;
        flagCloth.localPosition = pos;
    }
}
