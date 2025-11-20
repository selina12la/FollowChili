using System.Collections;
using UnityEngine;

public class CatPetting : MonoBehaviour
{
    [Tooltip("Wie lange die Streichel-Animation dauern soll.")]
    public float petDuration = 2.0f;

    private Animator animator;
    private bool isPetting;

    private CatFollowFood followFood;
    private CatFollowToy followToy;
    private CatMovement wander;

    private bool wasFollowFood;
    private bool wasFollowToy;
    private bool wasWander;

    private void Awake()
    {
        animator   = GetComponent<Animator>();
        followFood = GetComponent<CatFollowFood>();
        followToy  = GetComponent<CatFollowToy>();
        wander     = GetComponent<CatMovement>();
    }

    public void Pet()
    {
        if (isPetting) return; 

        isPetting = true;

        if (followFood)
        {
            wasFollowFood = followFood.enabled;
            followFood.enabled = false;
        }

        if (followToy)
        {
            wasFollowToy = followToy.enabled;
            followToy.enabled = false;
        }

        if (wander)
        {
            wasWander = wander.enabled;
            wander.enabled = false;
        }

        if (animator)
        {
            animator.SetBool("isWalking", false);
            animator.ResetTrigger("Pet");
            animator.SetTrigger("Pet");
        }

        StartCoroutine(EndPetRoutine());
    }

    private IEnumerator EndPetRoutine()
    {
        yield return new WaitForSeconds(petDuration);

        if (followFood) followFood.enabled = wasFollowFood;
        if (followToy)  followToy.enabled  = wasFollowToy;

        if (wander)
        {
            wander.enabled = wasWander;
            if (wasWander)
            {
                wander.RestartAfterDelay();
            }
        }

        isPetting = false;
    }
}
