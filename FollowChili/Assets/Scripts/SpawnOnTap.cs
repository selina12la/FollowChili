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
    public float catPickupDistance = 0.25f;
    public float catDropDistance = 0.25f;
    public float toyDropForward = 0.45f;
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
        Debug.Log("SpawnCat Button gedrückt");
        
        if (spawnedCat != null)
        {
            Debug.Log("Katze existiert schon.");
            if (spawnedFood) SwitchCatFollowToFood();
            else if (spawnedToy) SwitchCatFollowToToy();
            return;
        }

        SpawnObject(catPrefab, isToy: false);
    }

    public void SpawnFood()
    {
        Debug.Log("SpawnFood Button gedrückt");
        
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

        SpawnObject(foodPrefab, isToy: false);
        if (spawnedCat) SwitchCatFollowToFood();
    }

    public void SpawnToy()
    {
        Debug.Log("SpawnToy Button gedrückt");
        
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

        SpawnObject(toyPrefab, isToy: true);
        if (spawnedCat) SwitchCatFollowToToy();
    }

    public void CallCat()
    {
        Debug.Log($"CallCat aufgerufen - spawnedCat: {spawnedCat != null}");
        
        if (!spawnedCat)
        {
            Debug.Log("Keine Katze gespawnt.");
            return;
        }

        Transform cam = Camera.main.transform;
        
        // Sicherstellen dass die Katze die benötigten Komponenten hat
        EnsureCatComponents(spawnedCat);
        
        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var followToy = spawnedCat.GetComponent<CatFollowToy>();

        // Beide deaktivieren zuerst
        if (followFood) 
        {
            followFood.enabled = false;
            followFood.ClearTarget();
        }
        if (followToy) 
        {
            followToy.enabled = false;
            followToy.ClearTarget();
        }

        // Eines aktivieren für Call
        if (followFood)
        {
            followFood.enabled = true;
            followFood.CallCatTo(cam);
            Debug.Log("Katze folgt Food Target zur Kamera");
        }
        else if (followToy)
        {
            followToy.enabled = true;
            followToy.CallCatTo(cam);
            Debug.Log("Katze folgt Toy Target zur Kamera");
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
        
        // Bessere Positionierung
        Vector3 spawnPosition = pose.position;
        float heightOffset = GetHalfHeight(prefab);
        spawnPosition.y += heightOffset;
        
        GameObject newObj = Instantiate(prefab, spawnPosition, pose.rotation);

        // Höhe/Pivot ausgleichen (damit nichts im Boden steckt)
        float actualHeight = GetHalfHeight(newObj);
        newObj.transform.position = spawnPosition + Vector3.up * actualHeight;

        if (isToy)
        {
            if (spawnedToy) Destroy(spawnedToy);
            spawnedToy = newObj;
            Debug.Log($"Toy gespawnt: {spawnedToy.name}");

            if (spawnedCat) 
            {
                Debug.Log("Toy gespawnt - aktiviere FollowToy");
                SwitchCatFollowToToy();
            }

            if (fetchRoutine != null) StopCoroutine(fetchRoutine);
            fetchRoutine = StartCoroutine(FetchMonitorRoutine());
            return;
        }

        if (prefab == catPrefab)
        {
            spawnedCat = newObj;
            Debug.Log($"Katze gespawnt: {spawnedCat.name}");

            // Sicherstellen dass Katze Komponenten hat
            EnsureCatComponents(spawnedCat);

            if (spawnedFood) 
            {
                Debug.Log("Katze gespawnt - aktiviere FollowFood");
                SwitchCatFollowToFood();
            }
            else if (spawnedToy) 
            {
                Debug.Log("Katze gespawnt - aktiviere FollowToy");
                SwitchCatFollowToToy();
            }
        }
        else if (prefab == foodPrefab)
        {
            spawnedFood = newObj;
            Debug.Log($"Food gespawnt: {spawnedFood.name}");
            
            if (spawnedCat) 
            {
                Debug.Log("Food gespawnt - aktiviere FollowFood");
                SwitchCatFollowToFood();
            }
        }
    }

    // -------------------------------
    // Follow-Umschalter (exklusiv)
    // -------------------------------

    private void SwitchCatFollowToFood()
    {
        Debug.Log($"SwitchCatFollowToFood - Katze: {spawnedCat != null}, Food: {spawnedFood != null}");
        
        if (!spawnedCat || !spawnedFood) 
        {
            Debug.Log($"Fehler: spawnedCat={spawnedCat != null}, spawnedFood={spawnedFood != null}");
            return;
        }

        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var followToy = spawnedCat.GetComponent<CatFollowToy>();

        if (followToy && followToy.enabled)
        {
            followToy.enabled = false;
            followToy.ClearTarget();
            Debug.Log("FollowToy deaktiviert");
        }

        if (followFood)
        {
            followFood.enabled = true;
            followFood.SetTarget(spawnedFood.transform);
            Debug.Log("FollowFood aktiviert mit Food Target");
        }
        else
        {
            Debug.LogError("CatFollowFood Component nicht gefunden!");
        }

        var wander = spawnedCat.GetComponent<CatMovement>();
        if (wander) wander.enabled = false;
    }

    private void SwitchCatFollowToToy()
    {
        Debug.Log($"SwitchCatFollowToToy - Katze: {spawnedCat != null}, Toy: {spawnedToy != null}");
        
        if (!spawnedCat || !spawnedToy) 
        {
            Debug.Log($"Fehler: spawnedCat={spawnedCat != null}, spawnedToy={spawnedToy != null}");
            return;
        }

        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var followToy = spawnedCat.GetComponent<CatFollowToy>();

        if (followFood && followFood.enabled)
        {
            followFood.enabled = false;
            followFood.ClearTarget();
            Debug.Log("FollowFood deaktiviert");
        }

        if (followToy)
        {
            followToy.enabled = true;
            followToy.SetTarget(spawnedToy.transform);
            Debug.Log("FollowToy aktiviert mit Toy Target");
        }
        else
        {
            Debug.LogError("CatFollowToy Component nicht gefunden!");
        }

        var wander = spawnedCat.GetComponent<CatMovement>();
        if (wander) wander.enabled = false;
    }

    // -------------------------------
    // Toy-Interaktion & Fetch
    // -------------------------------

    private IEnumerator FetchMonitorRoutine()
    {
        if (!spawnedCat || !spawnedToy) 
        {
            Debug.Log("FetchMonitor: Katze oder Toy fehlt");
            yield break;
        }

        var cat = spawnedCat.transform;
        bool pickedUp = false;

        // HoldPoint suchen (optional)
        Transform holdPoint = spawnedCat.transform.Find("HoldPoint");
        if (!holdPoint) 
        {
            holdPoint = spawnedCat.transform;
            Debug.Log("Kein HoldPoint gefunden, verwende Katzen-Transform");
        }

        Debug.Log("FetchMonitor gestartet");

        while (spawnedCat && spawnedToy)
        {
            float d = Vector3.Distance(cat.position, spawnedToy.transform.position);
            Debug.Log($"Distanz Katze-Toy: {d}");

            // 1) Aufheben
            if (!pickedUp && d <= catPickupDistance)
            {
                spawnedToy.transform.SetParent(holdPoint, worldPositionStays:false);
                spawnedToy.transform.localPosition = Vector3.zero;
                spawnedToy.transform.localRotation = Quaternion.identity;
                pickedUp = true;
                Debug.Log("Toy aufgehoben");

                // Jetzt zur Kamera laufen
                Transform cam = Camera.main.transform;
                var followToy = spawnedCat.GetComponent<CatFollowToy>();
                if (followToy) 
                {
                    followToy.CallCatTo(cam);
                    Debug.Log("Katze zur Kamera gerufen");
                }
            }

            // 2) Vor Kamera ablegen
            if (pickedUp)
            {
                Transform cam = Camera.main.transform;
                float dToCam = Vector3.Distance(cat.position, cam.position);
                Debug.Log($"Distanz Katze-Kamera: {dToCam}");

                if (dToCam <= catDropDistance)
                {
                    spawnedToy.transform.SetParent(null, worldPositionStays:true);
                    Vector3 dropPos = cam.position + cam.forward * toyDropForward;

                    // auf AR-Ebene ablegen
                    spawnedToy.transform.position = dropPos;
                    spawnedToy.transform.rotation = Quaternion.LookRotation(
                        Vector3.ProjectOnPlane(cam.forward, Vector3.up), Vector3.up);

                    // Follow wieder aufheben/idle
                    var followToy = spawnedCat.GetComponent<CatFollowToy>();
                    if (followToy) 
                    { 
                        followToy.ClearTarget(); 
                        followToy.enabled = false; 
                        Debug.Log("FollowToy deaktiviert nach Ablegen");
                    }

                    var wander = spawnedCat.GetComponent<CatMovement>();
                    if (wander) 
                    {
                        wander.enabled = true;
                        Debug.Log("Wander aktiviert");
                    }

                    Debug.Log("Toy abgelegt");
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.1f); // Etwas weniger frequent updaten
        }
        
        Debug.Log("FetchMonitor beendet");
    }

    // -------------------------------
    // Utils
    // -------------------------------

    private void EnsureCatComponents(GameObject cat)
    {
        // Füge die Follow-Scripts hinzu, falls nicht vorhanden
        if (!cat.GetComponent<CatFollowFood>())
        {
            cat.AddComponent<CatFollowFood>();
            Debug.Log("CatFollowFood Component hinzugefügt");
        }
        
        if (!cat.GetComponent<CatFollowToy>())
        {
            cat.AddComponent<CatFollowToy>();
            Debug.Log("CatFollowToy Component hinzugefügt");
        }
        
        // Animator sicherstellen
        if (!cat.GetComponent<Animator>())
        {
            Debug.LogWarning("Katze Prefab hat keinen Animator!");
        }
    }

    private float GetHalfHeight(GameObject go)
    {
        var rend = go.GetComponentInChildren<Renderer>();
        if (rend) 
        {
            rend.enabled = true; // Force bounds update
            return rend.bounds.extents.y;
        }

        var col = go.GetComponentInChildren<Collider>();
        if (col) 
        {
            col.enabled = true; // Force bounds update  
            return col.bounds.extents.y;
        }

        return 0.1f; // Fallback erhöhen
    }
}