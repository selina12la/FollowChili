using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SpawnOnTap : MonoBehaviour
{
    public ARRaycastManager raycastManager;

    public GameObject catPrefab;
    public GameObject toyPrefab;

    private GameObject spawnedCat;
    private GameObject spawnedToy;

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
                    var followScript = spawnedCat.GetComponent<CatFollowToy>();
                    if (followScript != null)
                        followScript.SetTarget(spawnedToy.transform);
                }
            }
            else
            {
                spawnedCat = newObj;

                if (spawnedToy != null)
                {
                    var followScript = spawnedCat.GetComponent<CatFollowToy>();
                    if (followScript != null)
                        followScript.SetTarget(spawnedToy.transform);
                }
            }
        }
        else
        {
            Debug.LogWarning("Kein Plane-Hit gefunden!");
        }
    }
}
