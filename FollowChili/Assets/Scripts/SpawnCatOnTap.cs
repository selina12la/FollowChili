using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
 
public class SpawnCatOnTap : MonoBehaviour
{
    public GameObject objectToSpawn;
    public ARRaycastManager raycastManager;
    private GameObject spawnedObject;
 
    void Start()
    {
        if (raycastManager == null)
            raycastManager = FindFirstObjectByType<ARRaycastManager>();
    }
 
    public void SpawnAtCenter()
    {
        // Prüfen, ob schon ein Objekt existiert
        if (spawnedObject != null)
        {
            Debug.Log("Ein Objekt wurde bereits gespawnt!");
            return; // Abbrechen, kein neues Objekt spawnen
        }
 
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        var hits = new List<ARRaycastHit>();
 
        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            spawnedObject = Instantiate(objectToSpawn, hitPose.position, hitPose.rotation);
        }
    }
}