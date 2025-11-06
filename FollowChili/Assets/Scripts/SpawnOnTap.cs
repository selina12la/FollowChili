using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SpawnOnTap : MonoBehaviour
{
    public ARRaycastManager raycastManager;

    public GameObject catPrefab;
    public GameObject toyPrefab;
    public GameObject foodPrefab;

    private GameObject spawnedCat;
    private GameObject spawnedToy;
    private GameObject spawnedFood;

    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        if (raycastManager == null)
            raycastManager = FindFirstObjectByType<ARRaycastManager>();
    }

    public void SpawnCat()
    {
        if (spawnedCat != null)
        {
            Debug.Log("Katze existiert schon.");

            if (spawnedToy != null)
            {
                var followScript = spawnedCat.GetComponent<CatFollowToy>();
                if (followScript != null)
                    followScript.SetTarget(spawnedToy.transform);
            }

            return;
        }

        SpawnObject(catPrefab, false);
    }

    public void SpawnFood()
    {
        if (spawnedFood != null)
        {
            Debug.Log("Food existiert schon.");

            if (spawnedCat != null)
            {
                var followScript = spawnedCat.GetComponent<CatFollowFood>();
                if (followScript != null)
                    followScript.SetTarget(spawnedFood.transform);
            }

            return;
        }

        SpawnObject(foodPrefab, false);
    }

    public void SpawnToy()
    {
        if (spawnedToy != null)
        {
            Debug.Log("Toy existiert schon.");

            if (spawnedCat != null)
            {
                var followScript = spawnedCat.GetComponent<CatFollowToy>();
                if (followScript != null)
                    followScript.SetTarget(spawnedToy.transform);
            }

            return;
        }

        SpawnObject(toyPrefab, true);
    }

    private void SpawnObject(GameObject prefab, bool isToy)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Kein Prefab referenziert!");
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;
            GameObject newObj = Instantiate(prefab, pose.position, pose.rotation);

            if (isToy)
            {
                spawnedToy = newObj;

                if (spawnedCat != null)
                {
                    var followToy = spawnedCat.GetComponent<CatFollowToy>();
                    if (followToy != null)
                        followToy.SetTarget(spawnedToy.transform);
                }
            }
            else
            {
                if (prefab == catPrefab)
                {
                    spawnedCat = newObj;

                    if (spawnedFood != null)
                    {
                        var followFood = spawnedCat.GetComponent<CatFollowFood>();
                        if (followFood != null)
                            followFood.SetTarget(spawnedFood.transform);
                    }
                    else if (spawnedToy != null)
                    {
                        var followToy = spawnedCat.GetComponent<CatFollowToy>();
                        if (followToy != null)
                            followToy.SetTarget(spawnedToy.transform);
                    }
                }
                else if (prefab == foodPrefab)
                {
                    spawnedFood = newObj;

                    if (spawnedCat != null)
                    {
                        var followFood = spawnedCat.GetComponent<CatFollowFood>();
                        if (followFood != null)
                            followFood.SetTarget(spawnedFood.transform);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Kein Plane-Hit gefunden!");
        }

       
    }
    public void CallCat()
    {
        if (spawnedCat == null)
        {
            Debug.Log("Keine Katze gespawnt.");
            return;
        }

        // Kamera-Transform
        Transform cam = Camera.main.transform;

        // Versuche zuerst Food-Follow, sonst Toy-Follow
        var followFood = spawnedCat.GetComponent<CatFollowFood>();
        var followToy = spawnedCat.GetComponent<CatFollowToy>();

        if (followFood != null)
            followFood.CallCatTo(cam);
        if (followToy != null)
            followToy.CallCatTo(cam);

        Debug.Log("Katze wird zur Kamera gerufen.");
    }
}