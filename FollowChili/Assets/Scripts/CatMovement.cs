using UnityEngine;
using System.Collections;

public class CatMovement : MonoBehaviour
{
    [Header("Random Walk")]
    public float moveSpeed = 0.5f;
    public float waitTime = 1.0f;
    public float moveRange = 1.5f;
    public float rotationSpeed = 5f;
    public float startDelay = 0.1f; 

    [Header("Grounding")]
    public float planeYOverride = float.NaN; 

    private Vector3 areaCenter;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private Animator animator;
    private bool routineRunning = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        if (!routineRunning) StartCoroutine(MoveRoutine());
    }

    void OnDisable()
    {
        routineRunning = false;
        SetWalking(false);
        StopAllCoroutines();
    }

    public void RestartAfterDelay()
    {
        StopAllCoroutines();
        routineRunning = false;
        StartCoroutine(MoveRoutine());
    }

    void Start()
    {
        if (areaCenter == Vector3.zero) areaCenter = transform.position;
    }

    public void SetAreaCenter(Vector3 center)
    {
        areaCenter = center;
    }

    IEnumerator MoveRoutine()
    {
        routineRunning = true;

        yield return new WaitForSeconds(startDelay);

        while (enabled)
        {
            if (!isMoving)
            {
                Vector2 randomCircle = Random.insideUnitCircle * moveRange;
                float y = float.IsNaN(planeYOverride) ? transform.position.y : planeYOverride;
                targetPosition = new Vector3(areaCenter.x + randomCircle.x, y, areaCenter.z + randomCircle.y);
                isMoving = true;
                SetWalking(true);
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            Vector3 dir = targetPosition - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion tRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, tRot, rotationSpeed * Time.deltaTime);
            }

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isMoving = false;
                SetWalking(false);
                yield return new WaitForSeconds(waitTime);
            }

            yield return null;
        }

        routineRunning = false;
    }

    private void SetWalking(bool walking)
    {
        if (animator) animator.SetBool("isWalking", walking);
    }
}
