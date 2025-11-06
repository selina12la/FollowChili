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

    // Aktive Instanzen
    private GameObject spawnedCat;
    private GameObject spawnedToy;
    private GameObject spawnedFood;

    // Raycast Cache
    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    [Header("Fetch / Bring back")]
    public float catPickupDistance = 0.25f;   // Distanz, ab der die Katze das Toy „aufhebt“
    public float catDropDistance   = 0.25f;   // Distanz, ab der vor der Kamera gedroppt wird
    public float toyDropForward    = 0.45f;   // Abstand vor der Kamera
    private Coroutine fetchRoutine;

    void Start()
    {
        if (raycastManager == null)
            raycastManager = FindFirstObjectByType<ARRaycastManager>();
    }

    // -------------------------------
    // Buttons
    // -------------------------------

    public void SpawnCat()
    {
        if (spawnedCat != null)
        {
            Debug.Log("Katze existiert schon.");
            if (spawnedFood)      SwitchCatFollowToFood();
            else if (spawnedToy)  SwitchCatFollowToToy();
            return;
        }

        SpawnObject(catPrefab, isToy:false);
    }

    public void SpawnFood()
    {
        // Exklusivität: Toy zuerst entfernen
        if (spawnedToy)
        {
            Destroy(spawnedToy);
            spawnedToy = null;
        }

        if (spawnedFood)
        {
            Debug.Log("Food existiert schon.");
            if (spawnedCat) SwitchCatFollowToFood();
            return;
        }

        SpawnObject(foodPrefab, isToy:false);
        if (spawnedCat) SwitchCatFollowToFood();
    }

    public void SpawnToy()
    {
        // Exklusivität: Food zuerst entfernen
        if (spawnedFood)
        {
            Destroy(spawnedFood);
            spawnedFood = null;
        }

        if (spawnedToy)
        {
            Debug.Log("Toy existiert schon.");
            if (spawnedCat) SwitchCatFollowToToy();
            return;
        }

        SpawnObject(toyPrefab, isToy:true);
        if (spawnedCat) SwitchCatFollowToToy();
    }

    public void CallCat()
    {
        if (!spawnedCat)
        {
            Debug.Log("Keine Katze gespawnt.");
            return;
        }

        Transform cam = Camera.main.transform;
        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var followToy  = spawnedCat.GetComponent<CatFollowToy>();

        // EIN Follow aktiv – Food bevorzugt
        if (followFood)
        {
            if (followToy) { TryClear(followToy); followToy.enabled = false; }
            followFood.enabled = true;
            followFood.CallCatTo(cam);
        }
        else if (followToy)
        {
            followToy.enabled = true;
            followToy.CallCatTo(cam);
        }

        var wander = spawnedCat.GetComponent<CatMovement>();
        if (wander) wander.enabled = false;

        Debug.Log("Katze wird zur Kamera gerufen.");
    }

    // -------------------------------
    // Spawning
    // -------------------------------

    private void SpawnObject(GameObject prefab, bool isToy)
    {
        if (!prefab)
        {
            Debug.LogWarning("Kein Prefab referenziert!");
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        if (!raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            Debug.LogWarning("Kein Plane-Hit gefunden!");
            return;
        }

        Pose pose = hits[0].pose;
        GameObject newObj = Instantiate(prefab, pose.position, pose.rotation);

        // Höhe/Pivot ausgleichen (damit nichts im Boden steckt)
        float halfHeight = GetHalfHeight(newObj);
        newObj.transform.position = pose.position + pose.up * halfHeight;

        if (isToy)
        {
            if (spawnedToy) Destroy(spawnedToy);
            spawnedToy = newObj;

            EnsureToyInteractable(spawnedToy);

            if (spawnedCat) SwitchCatFollowToToy();

            if (fetchRoutine != null) StopCoroutine(fetchRoutine);
            fetchRoutine = StartCoroutine(FetchMonitorRoutine());
            return;
        }

        if (prefab == catPrefab)
        {
            spawnedCat = newObj;

            if (spawnedToy) EnsureToyInteractable(spawnedToy);

            if (spawnedFood)      SwitchCatFollowToFood();
            else if (spawnedToy)  SwitchCatFollowToToy();
        }
        else if (prefab == foodPrefab)
        {
            spawnedFood = newObj;
            if (spawnedCat) SwitchCatFollowToFood();
        }
    }

    // -------------------------------
    // Follow-Umschalter (exklusiv)
    // -------------------------------

    private void SwitchCatFollowToFood()
    {
        if (!spawnedCat || !spawnedFood) return;

        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var followToy  = spawnedCat.GetComponent<CatFollowToy>();

        if (followToy)
        {
            TryClear(followToy);
            followToy.enabled = false;
        }

        if (followFood)
        {
            followFood.enabled = true;
            followFood.SetTarget(spawnedFood.transform);
        }

        var wander = spawnedCat.GetComponent<CatMovement>();
        if (wander) wander.enabled = false;
    }

    private void SwitchCatFollowToToy()
    {
        if (!spawnedCat || !spawnedToy) return;

        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var followToy  = spawnedCat.GetComponent<CatFollowToy>();

        if (followFood)
        {
            TryClear(followFood);
            followFood.enabled = false;
        }

        if (followToy)
        {
            followToy.enabled = true;
            followToy.SetTarget(spawnedToy.transform);
        }

        var wander = spawnedCat.GetComponent<CatMovement>();
        if (wander) wander.enabled = false;
    }

    // -------------------------------
    // Toy-Interaktion & Fetch
    // -------------------------------

    private void EnsureToyInteractable(GameObject toy)
    {
        var ti = toy.GetComponent<ToyInteractable>();
        if (!ti) ti = toy.AddComponent<ToyInteractable>();

        ti.arRaycastManager = raycastManager;
        ti.OnReleased -= OnToyReleased; // doppelte Abos vermeiden
        ti.OnReleased += OnToyReleased;
    }

    private void OnToyReleased(Vector3 worldPos)
    {
        // Beim Loslassen: Katze holt das Toy
        if (spawnedCat && spawnedToy)
        {
            SwitchCatFollowToToy(); // setzt Target = Toy
            if (fetchRoutine != null) StopCoroutine(fetchRoutine);
            fetchRoutine = StartCoroutine(FetchMonitorRoutine());
        }
    }

    private IEnumerator FetchMonitorRoutine()
    {
        if (!spawnedCat || !spawnedToy) yield break;

        var cat = spawnedCat.transform;
        bool pickedUp = false;

        // HoldPoint suchen (optional)
        Transform holdPoint = spawnedCat.transform.Find("HoldPoint");
        if (!holdPoint) holdPoint = spawnedCat.transform;

        while (spawnedCat && spawnedToy)
        {
            float d = Vector3.Distance(cat.position, spawnedToy.transform.position);

            // 1) Aufheben
            if (!pickedUp && d <= catPickupDistance)
            {
                spawnedToy.transform.SetParent(holdPoint, worldPositionStays:false);
                spawnedToy.transform.localPosition = Vector3.zero;
                spawnedToy.transform.localRotation = Quaternion.identity;
                pickedUp = true;

                // Jetzt zur Kamera laufen
                Transform cam = Camera.main.transform;
                var followToy = spawnedCat.GetComponent<CatFollowToy>();
                if (followToy) followToy.CallCatTo(cam);
            }

            // 2) Vor Kamera ablegen
            if (pickedUp)
            {
                Transform cam = Camera.main.transform;
                float dToCam = Vector3.Distance(cat.position, cam.position);
                if (dToCam <= catDropDistance)
                {
                    spawnedToy.transform.SetParent(null, worldPositionStays:true);
                    Vector3 dropPos = cam.position + cam.forward * toyDropForward;

                    // auf AR-Ebene ablegen (leicht anheben, dann absenken)
                    spawnedToy.transform.position = dropPos;
                    spawnedToy.transform.rotation = Quaternion.LookRotation(
                        Vector3.ProjectOnPlane(cam.forward, Vector3.up), Vector3.up);

                    // Follow wieder aufheben/idle
                    var followToy = spawnedCat.GetComponent<CatFollowToy>();
                    if (followToy) { followToy.ClearTarget(); followToy.enabled = false; }

                    var wander = spawnedCat.GetComponent<CatMovement>();
                    if (wander) wander.enabled = true;

                    yield break;
                }
            }

            yield return null;
        }
    }

    // -------------------------------
    // Utils
    // -------------------------------

    private float GetHalfHeight(GameObject go)
    {
        var rend = go.GetComponentInChildren<Renderer>();
        if (rend) return rend.bounds.extents.y;

        var col = go.GetComponentInChildren<Collider>();
        if (col) return col.bounds.extents.y;

        return 0.05f; // Fallback
    }

    private void TryClear(object follow)
    {
        var m = follow.GetType().GetMethod("ClearTarget");
        if (m != null) m.Invoke(follow, null);
    }
}
