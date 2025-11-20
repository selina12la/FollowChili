using System.Collections;
using UnityEngine;

public class CatFollowFood : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 0.5f;
    public float rotationSpeed = 5f;
    public float startWalkDistance = 0.35f;
    public float stopDistance = 0.20f;
    public float eatDuration = 1.0f;
    public float pickUpDuration = 0.3f;
    public Transform holdPoint;
    public GameObject crumblePrefab;
    public float crumbleOffsetY = 0.02f;

    private bool isConsuming = false;
    private Animator animator;
    private bool isWalkingAnim = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        SetWalking(false);

        if (holdPoint == null)
        {
            var hp = transform.Find("HoldPoint");
            if (hp != null) holdPoint = hp;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        isConsuming = false;
    }

    void Update()
    {
        if (isConsuming) return;
        if (target == null)
        {
            SetWalking(false);
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        bool shouldWalk = isWalkingAnim ? (dist > stopDistance) : (dist > startWalkDistance);
        SetWalking(shouldWalk);

        if (shouldWalk)
        {
            transform.position += toTarget.normalized * moveSpeed * Time.deltaTime;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(toTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            if (!isConsuming && dist <= stopDistance + 0.01f)
            {
                StartCoroutine(PickupAndConsumeFood());
            }
        }
    }

    private IEnumerator PickupAndConsumeFood()
    {
        isConsuming = true;
        SetWalking(false);

        if (target == null)
        {
            isConsuming = false;
            yield break;
        }

        Transform foodTf = target;
        GameObject foodObj = foodTf.gameObject;

        float groundY = foodTf.position.y;
        var rend = foodObj.GetComponentInChildren<Renderer>();
        if (rend != null) groundY = rend.bounds.min.y;
        else
        {
            var col = foodObj.GetComponentInChildren<Collider>();
            if (col != null) groundY = col.bounds.min.y;
        }

        Vector3 groundPos = foodTf.position;
        groundPos.y = groundY;

        Transform hp = holdPoint != null ? holdPoint : transform;

        foodTf.SetParent(hp, true);

        Vector3 startPos = foodTf.position;
        Quaternion startRot = foodTf.rotation;
        Vector3 endPos = hp.position;
        Quaternion endRot = hp.rotation;

        float t = 0f;
        while (t < pickUpDuration)
        {
            t += Time.deltaTime;
            float l = Mathf.Clamp01(t / pickUpDuration);

            foodTf.position = Vector3.Lerp(startPos, endPos, l);
            foodTf.rotation = Quaternion.Slerp(startRot, endRot, l);

            yield return null;
        }

        foodTf.position = hp.position;
        foodTf.rotation = hp.rotation;

        yield return new WaitForSeconds(eatDuration);

        if (foodObj != null)
        {
            if (crumblePrefab != null)
            {
                Vector3 crumblePos = groundPos;
                crumblePos.y += crumbleOffsetY;
                Instantiate(crumblePrefab, crumblePos, Quaternion.identity);
            }

            target = null;
            Destroy(foodObj);
        }

        var wander = GetComponent<CatMovement>();
        if (wander)
        {
            wander.enabled = true;
            wander.RestartAfterDelay();
        }

        isConsuming = false;
    }

    void SetWalking(bool walk)
    {
        if (animator != null && isWalkingAnim != walk)
        {
            isWalkingAnim = walk;
            animator.SetBool("isWalking", walk);
        }
    }

    public void CallCatTo(Transform callTarget)
    {
        target = callTarget;
        isConsuming = false;
    }

    public void ClearTarget()
    {
        target = null;
        isConsuming = false;
        SetWalking(false);
    }
}
