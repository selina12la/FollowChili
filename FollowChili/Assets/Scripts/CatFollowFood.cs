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
    private bool isConsuming = false;

    private Animator animator;
    private bool isWalkingAnim = false;
    private bool hasPlayedSit = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        SetWalking(false);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        hasPlayedSit = false;
        isConsuming = false;
    }

    void Update()
    {
        if (isConsuming)
            return; 

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
            if (dist <= stopDistance + 0.01f)
            {
                StartCoroutine(ConsumeFood());
                return;
            }
            
            if (!hasPlayedSit && animator != null)
            {
                if (HasParam(animator, "sitOnce"))
                    animator.SetTrigger("sitOnce");
                hasPlayedSit = true;
            }
        }
    }

    private IEnumerator ConsumeFood()
    {
        isConsuming = true;
        SetWalking(false);

        yield return new WaitForSeconds(eatDuration);

        if (target != null)
        {
            GameObject foodObj = target.gameObject;
            target = null;
            if (foodObj != null)
                Destroy(foodObj);
        }

        hasPlayedSit = false;
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

    bool HasParam(Animator anim, string name)
    {
        foreach (var p in anim.parameters)
            if (p.name == name)
                return true;
        return false;
    }

    public void CallCatTo(Transform callTarget)
    {
        target = callTarget;
        hasPlayedSit = false;
        isConsuming = false;
    }

    public void ClearTarget()
    {
        target = null;
        hasPlayedSit = false;
        isConsuming = false;
        SetWalking(false);
    }
}
