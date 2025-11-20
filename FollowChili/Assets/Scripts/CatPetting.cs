using System.Collections;
using UnityEngine;

public class CatPetting : MonoBehaviour
{
    [Tooltip("Wie lange die Streichel-Animation dauern soll.")]
    public float petDuration = 2.0f;

    [Header("Hearts")]
    [Tooltip("Prefab mit Herz-Particle / Sprite, das über dem Kopf erscheinen soll.")]
    public GameObject heartsPrefab;

    [Tooltip("Wie hoch über dem Kopf die Herzen erscheinen sollen.")]
    public float heartsHeightOffset = 0.2f;

    [Tooltip("Wie lange die Herzen sichtbar bleiben (Sekunden).")]
    public float heartsLifetime = 1.5f;

    private Animator animator;
    private bool isPetting;

    private CatFollowFood followFood;
    private CatFollowToy  followToy;
    private CatMovement   wander;

    private bool wasFollowFood;
    private bool wasFollowToy;
    private bool wasWander;

    void Awake()
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
        SpawnHearts();

        StartCoroutine(EndPetRoutine());
    }

    private void SpawnHearts()
    {
        if (heartsPrefab == null) return;

        Transform head = FindHeadBone();
        if (head == null)
            head = transform;

        
        float highestY = transform.position.y;
        var renders = GetComponentsInChildren<Renderer>();
        foreach (var r in renders)
            highestY = Mathf.Max(highestY, r.bounds.max.y);

        Vector3 spawnPos = new Vector3(
            transform.position.x,
            highestY + heartsHeightOffset,
            transform.position.z
        );

        GameObject hearts = Instantiate(heartsPrefab, spawnPos, Quaternion.identity);

        hearts.transform.SetParent(head, true);

        Destroy(hearts, heartsLifetime);
    }

    
    private Transform FindHeadBone()
    {
        var all = GetComponentsInChildren<Transform>();
        foreach (var t in all)
        {
            string n = t.name.ToLower();
            if (n.Contains("head"))
                return t;
            if (n.Contains("neck"))
                return t;
        }
        return null;
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
                wander.RestartAfterDelay();
        }

        isPetting = false;
    }
}
