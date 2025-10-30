using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SpawnCatOnTap : MonoBehaviour
{
    public GameObject objectToSpawn;
    public ARRaycastManager raycastManager;

    private GameObject spawnedObject;

    private float? groundY = null;

    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        if (raycastManager == null)
            raycastManager = FindFirstObjectByType<ARRaycastManager>();
    }

    public void SpawnAtCenter()
    {
        if (spawnedObject != null)
        {
            Debug.Log("Ein Objekt wurde bereits gespawnt!");
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        var hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            ARRaycastHit lowestHit = hits[0];
            float lowestY = hits[0].pose.position.y;

            foreach (var h in hits)
            {
                if (h.pose.position.y < lowestY)
                {
                    lowestY = h.pose.position.y;
                    lowestHit = h;
                }
            }

            Vector3 pos = lowestHit.pose.position;
            pos.y = lowestY; 

            spawnedObject = Instantiate(objectToSpawn, pos, lowestHit.pose.rotation);
        }
        else
        {
            Debug.LogWarning("Kein Plane-Hit gefunden – vermutlich schaust du nicht auf eine erkannte Fläche.");
        }
    }
}