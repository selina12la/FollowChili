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

    // Referenzen auf gespawnte Objekte
    private GameObject spawnedCat;
    private GameObject spawnedToy;
    private GameObject spawnedFood;

    // Raycast Cache
    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        if (raycastManager == null)
            raycastManager = FindFirstObjectByType<ARRaycastManager>();
    }

    // -------------------------------
    // Publice Button-Methoden
    // -------------------------------

    public void SpawnCat()
    {
        if (spawnedCat != null)
        {
            Debug.Log("Katze existiert schon.");
            // Auf bestehendes Ziel schalten (Food bevorzugt)
            if (spawnedFood != null)      SwitchCatFollowToFood();
            else if (spawnedToy != null)  SwitchCatFollowToToy();
            return;
        }

        SpawnObject(catPrefab, isToy: false);
    }

    public void SpawnFood()
    {
        // Exklusivität: erst Toy entfernen
        if (spawnedToy != null)
        {
            Destroy(spawnedToy);
            spawnedToy = null;
        }

        if (spawnedFood != null)
        {
            Debug.Log("Food existiert schon.");
            if (spawnedCat != null) SwitchCatFollowToFood();
            return;
        }

        SpawnObject(foodPrefab, isToy: false);
        if (spawnedCat != null) SwitchCatFollowToFood();
    }

    public void SpawnToy()
    {
        // Exklusivität: erst Food entfernen
        if (spawnedFood != null)
        {
            Destroy(spawnedFood);
            spawnedFood = null;
        }

        if (spawnedToy != null)
        {
            Debug.Log("Toy existiert schon.");
            if (spawnedCat != null) SwitchCatFollowToToy();
            return;
        }

        SpawnObject(toyPrefab, isToy: true);
        if (spawnedCat != null) SwitchCatFollowToToy();
    }

    public void CallCat()
    {
        if (spawnedCat == null)
        {
            Debug.Log("Keine Katze gespawnt.");
            return;
        }

        Transform cam = Camera.main.transform;

        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var followToy  = spawnedCat.GetComponent<CatFollowToy>();

        // Nur EIN Follow aktiv nutzen – hier Food bevorzugt
        if (followFood != null)
        {
            if (followToy != null) { TryClear(followToy); followToy.enabled = false; }
            followFood.enabled = true;
            followFood.CallCatTo(cam);
        }
        else if (followToy != null)
        {
            followToy.enabled = true;
            followToy.CallCatTo(cam);
        }

        // Optional: Wander/Idle deaktivieren
        var wander = spawnedCat.GetComponent<CatMovement>();
        if (wander) wander.enabled = false;

        Debug.Log("Katze wird zur Kamera gerufen.");
    }

    // -------------------------------
    // Interne Helfer
    // -------------------------------

    private void SpawnObject(GameObject prefab, bool isToy)
    {
        if (prefab == null)
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

        if (isToy)
        {
            spawnedToy = newObj;
            if (spawnedCat != null) SwitchCatFollowToToy();
            return;
        }

        // Nicht-Toy: kann Cat oder Food sein
        if (prefab == catPrefab)
        {
            spawnedCat = newObj;

            // Direkt auf existierendes Ziel schalten (Food bevorzugt)
            if (spawnedFood != null)      SwitchCatFollowToFood();
            else if (spawnedToy != null)  SwitchCatFollowToToy();
        }
        else if (prefab == foodPrefab)
        {
            spawnedFood = newObj;
            if (spawnedCat != null) SwitchCatFollowToFood();
        }
    }

    private void SwitchCatFollowToFood()
    {
        if (spawnedCat == null || spawnedFood == null) return;

        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var followToy  = spawnedCat.GetComponent<CatFollowToy>();

        // Toy-Follow sauber deaktivieren
        if (followToy != null)
        {
            TryClear(followToy);
            followToy.enabled = false;
        }

        if (followFood != null)
        {
            followFood.enabled = true;
            followFood.SetTarget(spawnedFood.transform);
        }

        // Wander aus
        var wander = spawnedCat.GetComponent<CatMovement>();
        if (wander) wander.enabled = false;
    }

    private void SwitchCatFollowToToy()
    {
        if (spawnedCat == null || spawnedToy == null) return;

        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var followToy  = spawnedCat.GetComponent<CatFollowToy>();

        if (followFood != null)
        {
            TryClear(followFood);
            followFood.enabled = false;
        }

        if (followToy != null)
        {
            followToy.enabled = true;
            followToy.SetTarget(spawnedToy.transform);
        }

        var wander = spawnedCat.GetComponent<CatMovement>();
        if (wander) wander.enabled = false;
    }

    private void TryClear(object follow)
    {
        // Erwartet, dass CatFollowFood/Toy eine ClearTarget()-Methode besitzen
        // (siehe vorherige Nachricht).
        var m = follow.GetType().GetMethod("ClearTarget");
        if (m != null) m.Invoke(follow, null);
    }
}
