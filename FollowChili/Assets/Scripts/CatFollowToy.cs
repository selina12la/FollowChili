using UnityEngine;

public class CatFollowToy : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 0.5f;
    public float rotationSpeed = 5f;

    public float startWalkDistance = 0.35f; 
    public float stopDistance      = 0.20f; 

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
    }

    void Update()
    {
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
            if (!hasPlayedSit && animator != null)
            {
                if (HasParam(animator, "sitOnce"))
                    animator.SetTrigger("sitOnce");
                hasPlayedSit = true;
            }
        }
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
            if (p.name == name) return true;
        return false;
    }
}
