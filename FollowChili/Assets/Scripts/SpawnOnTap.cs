using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SpawnOnTap : MonoBehaviour
{
    [Header("AR")]
    public ARRaycastManager raycastManager;

    [Header("Prefabs")]
    public GameObject catPrefab;
    public GameObject toyPrefab;
    public GameObject foodPrefab;

    private GameObject spawnedCat;
    private GameObject spawnedToy;
    private GameObject spawnedFood;

    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    [Header("Toy / Return")]
    public float catPickupDistance = 0.25f;
    public float catDropDistance = 0.25f;
    public float toyDropForward = 0.45f;

    [Header("Call Cat")]
    public float callCatDistance = 1.2f;

    private Coroutine pickupRoutine;
    private Coroutine returnWithToyRoutine;

    private Transform callTargetTransform;

    void Start()
    {
        if (raycastManager == null)
            raycastManager = FindFirstObjectByType<ARRaycastManager>();

        var go = new GameObject("CatCallTarget");
        go.hideFlags = HideFlags.HideInHierarchy;
        callTargetTransform = go.transform;
    }

    public void SpawnCat()
    {
        if (spawnedCat != null)
        {
            if (spawnedFood) SwitchCatFollowToFood();
            else if (spawnedToy) SwitchCatFollowToToy();
            else EnableWander();
            return;
        }

        SpawnObject(catPrefab, false);
    }

    public void SpawnFood()
    {
        if (spawnedToy)
        {
            Destroy(spawnedToy);
            spawnedToy = null;
        }

        if (spawnedFood != null)
        {
            if (spawnedFood == null) spawnedFood = null;
            else
            {
                if (spawnedCat) SwitchCatFollowToFood();
                return;
            }
        }

        SpawnObject(foodPrefab, false);
        if (spawnedCat) SwitchCatFollowToFood();
    }

    public void SpawnToy()
    {
        if (spawnedFood)
        {
            Destroy(spawnedFood);
            spawnedFood = null;
        }

        if (spawnedToy != null)
        {
            if (spawnedToy == null) spawnedToy = null;
            else
            {
                if (spawnedCat) SwitchCatFollowToToy();
                return;
            }
        }

        SpawnObject(toyPrefab, true);
        if (spawnedCat) SwitchCatFollowToToy();
    }

    public void CallCat()
    {
        if (!spawnedCat) return;

        if (IsCatHoldingToy())
        {
            if (returnWithToyRoutine != null) StopCoroutine(returnWithToyRoutine);
            returnWithToyRoutine = StartCoroutine(ReturnToyToCameraRoutine());
            return;
        }

        Transform cam = Camera.main.transform;
        EnsureCatComponents(spawnedCat);

        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var followToy = spawnedCat.GetComponent<CatFollowToy>();
        var wander = spawnedCat.GetComponent<CatMovement>();

        if (wander) wander.enabled = false;

        Vector3 forwardFlat = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
        if (forwardFlat.sqrMagnitude < 0.0001f) forwardFlat = cam.forward;
        forwardFlat.y = 0f;
        forwardFlat.Normalize();

        float y = spawnedCat.transform.position.y;
        Vector3 basePos = cam.position;
        basePos.y = y;
        callTargetTransform.position = basePos + forwardFlat * callCatDistance;
        callTargetTransform.rotation = Quaternion.LookRotation(-forwardFlat, Vector3.up);

        if (followFood)
        {
            followFood.enabled = true;
            followFood.CallCatTo(callTargetTransform);
        }
        else if (followToy)
        {
            followToy.enabled = true;
            followToy.CallCatTo(callTargetTransform);
        }
    }

    private void SpawnObject(GameObject prefab, bool isToy)
    {
        if (!prefab) return;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        if (!raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
            return;

        Pose pose = hits[0].pose;

        GameObject newObj = Instantiate(prefab, pose.position, pose.rotation);

        float offset = GetPivotCorrection(newObj);
        newObj.transform.position += Vector3.up * offset;

        if (prefab == catPrefab)
        {
            spawnedCat = newObj;
            EnsureCatComponents(spawnedCat);

            EnableWander();

            if (spawnedFood) SwitchCatFollowToFood();
            else if (spawnedToy) SwitchCatFollowToToy();

            var walk = spawnedCat.GetComponent<CatMovement>();
            if (walk) walk.planeYOverride = pose.position.y;
        }
        else if (isToy)
        {
            if (spawnedToy) Destroy(spawnedToy);
            spawnedToy = newObj;

            var interact = spawnedToy.GetComponent<ToyInteractable>();
            if (interact != null)
            {
                interact.OnReleased -= HandleToyReleased;
                interact.OnReleased += HandleToyReleased;
            }

            if (spawnedCat) SwitchCatFollowToToy();

            if (pickupRoutine != null) StopCoroutine(pickupRoutine);
            pickupRoutine = StartCoroutine(PickupToyAndWanderRoutine());
        }
        else if (prefab == foodPrefab)
        {
            spawnedFood = newObj;
            if (spawnedCat) SwitchCatFollowToFood();
        }
    }

    private void HandleToyReleased(Vector3 pos)
    {
        if (!spawnedCat || !spawnedToy) return;
        if (IsCatHoldingToy()) return;

        SwitchCatFollowToToy();

        if (pickupRoutine != null) StopCoroutine(pickupRoutine);
        pickupRoutine = StartCoroutine(PickupToyAndWanderRoutine());
    }

    private void SwitchCatFollowToFood()
    {
        if (!spawnedCat || !spawnedFood) return;

        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var followToy = spawnedCat.GetComponent<CatFollowToy>();
        var wander = spawnedCat.GetComponent<CatMovement>();

        if (followToy && followToy.enabled)
        {
            followToy.ClearTarget();
            followToy.enabled = false;
        }

        if (wander) wander.enabled = false;

        if (followFood)
        {
            followFood.enabled = true;
            followFood.SetTarget(spawnedFood.transform);
        }
    }

    private void SwitchCatFollowToToy()
    {
        if (!spawnedCat || !spawnedToy) return;

        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var followToy = spawnedCat.GetComponent<CatFollowToy>();
        var wander = spawnedCat.GetComponent<CatMovement>();

        if (followFood && followFood.enabled)
        {
            followFood.ClearTarget();
            followFood.enabled = false;
        }

        if (wander) wander.enabled = false;

        if (followToy)
        {
            followToy.enabled = true;
            followToy.SetTarget(spawnedToy.transform);
        }
    }

    private void EnableWander()
    {
        if (!spawnedCat) return;

        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var followToy = spawnedCat.GetComponent<CatFollowToy>();
        var wander = spawnedCat.GetComponent<CatMovement>();

        if (followFood)
        {
            followFood.ClearTarget();
            followFood.enabled = false;
        }

        if (followToy)
        {
            followToy.ClearTarget();
            followToy.enabled = false;
        }

        if (wander)
        {
            wander.enabled = true;
            wander.RestartAfterDelay();
        }
    }

    private IEnumerator PickupToyAndWanderRoutine()
    {
        if (!spawnedCat || !spawnedToy) yield break;

        var cat = spawnedCat.transform;
        bool pickedUp = false;

        Transform holdPoint = spawnedCat.transform.Find("HoldPoint");
        if (!holdPoint) holdPoint = spawnedCat.transform;

        while (spawnedCat && spawnedToy)
        {
            float d = Vector3.Distance(cat.position, spawnedToy.transform.position);

            if (!pickedUp && d <= catPickupDistance)
            {
                spawnedToy.transform.SetParent(holdPoint, false);
                spawnedToy.transform.localPosition = Vector3.zero;
                spawnedToy.transform.localRotation = Quaternion.identity;
                pickedUp = true;

                var followToy = spawnedCat.GetComponent<CatFollowToy>();
                if (followToy)
                {
                    followToy.ClearTarget();
                    followToy.enabled = false;
                }

                var wander = spawnedCat.GetComponent<CatMovement>();
                if (wander)
                {
                    wander.enabled = true;
                }

                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator ReturnToyToCameraRoutine()
    {
        if (!spawnedCat || !spawnedToy) yield break;

        var followToy = spawnedCat.GetComponent<CatFollowToy>();
        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var wander = spawnedCat.GetComponent<CatMovement>();

        if (wander) wander.enabled = false;
        if (followFood)
        {
            followFood.ClearTarget();
            followFood.enabled = false;
        }

        if (followToy) followToy.enabled = true;

        Transform cam = Camera.main.transform;
        followToy.CallCatTo(cam);

        while (spawnedCat && spawnedToy)
        {
            float dToCam = Vector3.Distance(spawnedCat.transform.position, cam.position);
            if (dToCam <= catDropDistance)
            {
                spawnedToy.transform.SetParent(null, true);
                Vector3 dropPos = cam.position + Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized * toyDropForward;
                spawnedToy.transform.position = dropPos;
                spawnedToy.transform.rotation = Quaternion.LookRotation(
                    Vector3.ProjectOnPlane(cam.forward, Vector3.up), Vector3.up);

                if (followToy)
                {
                    followToy.ClearTarget();
                    followToy.enabled = false;
                }

                if (wander)
                {
                    wander.enabled = true;
                    wander.RestartAfterDelay();
                }

                yield break;
            }

            yield return null;
        }
    }

    private void EnsureCatComponents(GameObject cat)
    {
        if (!cat.GetComponent<CatFollowFood>()) cat.AddComponent<CatFollowFood>();
        if (!cat.GetComponent<CatFollowToy>()) cat.AddComponent<CatFollowToy>();
        if (!cat.GetComponent<CatMovement>()) cat.AddComponent<CatMovement>();
        if (!cat.GetComponent<Animator>())
            Debug.LogWarning("Katze Prefab hat keinen Animator! (isWalking Bool wird dann ignoriert)");
    }

    private bool IsCatHoldingToy()
    {
        if (!spawnedCat || !spawnedToy) return false;
        return spawnedToy.transform.IsChildOf(spawnedCat.transform);
    }

    private float GetPivotCorrection(GameObject go)
    {
        var rend = go.GetComponentInChildren<Renderer>();
        if (rend)
        {
            float lowestPoint = rend.bounds.min.y;
            float currentY = go.transform.position.y;
            return currentY - lowestPoint;
        }

        var col = go.GetComponentInChildren<Collider>();
        if (col)
        {
            float lowestPoint = col.bounds.min.y;
            float currentY = go.transform.position.y;
            return currentY - lowestPoint;
        }

        return 0f;
    }
}
