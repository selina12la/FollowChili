using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(Collider))]
public class ToyInteractable : MonoBehaviour
{
    [Header("Physics Throw")]
    public float minThrowForce = 2f;
    public float maxThrowForce = 8f;
    public float upwardForceRatio = 0.5f;

    [Header("Refs (werden automatisch gesetzt)")]
    public ARRaycastManager arRaycastManager;
    public Camera mainCamera;

    [Header("Events")]
    public Action<Vector3> OnReleased;

    
    private bool isBeingDragged = false;
    private Vector3 grabOffset = Vector3.zero;

    
    private float halfHeight = -1f;   
    private float lastPlaneY = 0f;   
    private bool  hasPlaneY = false;

    private static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    [Header("Throw Settings")]
    [Tooltip("Ab dieser Pixel-Geschwindigkeit gilt die Geste als 'Wurf'")]
    public float throwMinScreenVelocity = 1000f;

    [Tooltip("Wie weit der Ball maximal fliegen kann (in Metern)")]
    public float throwMaxDistance = 2.0f;

    [Tooltip("Wie weit der Ball mindestens fliegt (in Metern)")]
    public float throwMinDistance = 0.3f;

    [Tooltip("Dauer des Wurfs in Sekunden")]
    public float throwDuration = 0.4f;

    [Tooltip("Maximale Höhe des Bogens über dem Boden (in Metern)")]
    public float throwArcHeight = 0.15f;

    private bool isBeingThrown = false;

    private Vector2 lastScreenPos1;
    private Vector2 lastScreenPos2;
    private float lastTime1;
    private float lastTime2;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (arRaycastManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            arRaycastManager = FindFirstObjectByType<ARRaycastManager>();
#else
            arRaycastManager = FindObjectOfType<ARRaycastManager>();
#endif
        }

        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void Update()
    {
        if (isBeingThrown) return;

#if UNITY_EDITOR
     
        if (Input.GetMouseButtonDown(0)) TryBeginGrab(Input.mousePosition);
        else if (Input.GetMouseButton(0) && isBeingDragged) ContinueDrag(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0) && isBeingDragged) EndGrab();
        
#endif
    }

 
    private void TryBeginGrab(Vector2 screenPos)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit.collider && (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)))
            {
                if (transform.parent != null)
                {
                    transform.SetParent(null, true);  
                }

                GetHalfHeight(); 

                var rb = GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                grabOffset   = transform.position - hit.point;
                isBeingDragged = true;
                
                lastScreenPos1 = screenPos;
                lastScreenPos2 = screenPos;
                lastTime1 = Time.time;
                lastTime2 = Time.time;
            }
        }
    }

    private void ContinueDrag(Vector2 screenPos)
    {
        lastScreenPos2 = lastScreenPos1;
        lastTime2      = lastTime1;

        lastScreenPos1 = screenPos;
        lastTime1      = Time.time;

        if (arRaycastManager != null && arRaycastManager.Raycast(screenPos, s_Hits, TrackableType.Planes) && s_Hits.Count > 0)
        {
            var pose = s_Hits[0].pose;
            lastPlaneY = pose.position.y;
            hasPlaneY  = true;

            Vector3 newPos = pose.position + pose.up * GetHalfHeight();
            newPos += Vector3.ProjectOnPlane(grabOffset, pose.up);
            transform.position = newPos;
            return;
        }

        if (mainCamera == null) mainCamera = Camera.main;
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        float planeY = hasPlaneY ? lastPlaneY : transform.position.y;
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 p = ray.GetPoint(enter);
            p.y = planeY + GetHalfHeight();
            p += Vector3.ProjectOnPlane(grabOffset, Vector3.up);
            transform.position = p;
        }
    }

    private void EndGrab()
    {
        isBeingDragged = false;

        float dt = Mathf.Max(0.001f, lastTime1 - lastTime2);
        Vector2 screenVelocity = (lastScreenPos1 - lastScreenPos2) / dt; 

        if (screenVelocity.magnitude > throwMinScreenVelocity)
        {
            StartThrow(screenVelocity);
            OnReleased?.Invoke(transform.position);
            return;
        }

        if (hasPlaneY)
        {
            Vector3 p = transform.position;
            p.y = lastPlaneY + GetHalfHeight();
            transform.position = p;
        }

        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        OnReleased?.Invoke(transform.position);
    }


    private void StartThrow(Vector2 screenVelocity)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        var rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight   = mainCamera.transform.right;

        Vector3 forward = Vector3.ProjectOnPlane(camForward, Vector3.up).normalized;
        Vector3 right   = Vector3.ProjectOnPlane(camRight,   Vector3.up).normalized;

        Vector2 dir2D = screenVelocity.normalized;
        Vector3 worldDir = (forward * Mathf.Clamp(dir2D.y, -1f, 1f)
                            + right   * Mathf.Clamp(dir2D.x, -1f, 1f)).normalized;

        if (worldDir.sqrMagnitude < 0.0001f)
            worldDir = forward;

        float speed = screenVelocity.magnitude;
        float t = Mathf.InverseLerp(throwMinScreenVelocity, throwMinScreenVelocity * 3f, speed);
        float force = Mathf.Lerp(minThrowForce, maxThrowForce, t);

        transform.SetParent(null, true);

        rb.isKinematic = false;
        rb.useGravity  = true;

        Vector3 velocity = worldDir * force;
        velocity.y += force * upwardForceRatio;

        rb.linearVelocity = velocity;
    }

    private System.Collections.IEnumerator ThrowRoutine(Vector3 startPos, Vector3 targetPos)
    {
        isBeingThrown = true;

        float elapsed = 0f;
        while (elapsed < throwDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / throwDuration);

            Vector3 pos = Vector3.Lerp(startPos, targetPos, t);

            float height = Mathf.Sin(t * Mathf.PI) * throwArcHeight;
            pos.y += height;

            transform.position = pos;
            yield return null;
        }

        Vector3 finalPos = targetPos;
        if (hasPlaneY)
        {
            finalPos.y = lastPlaneY + GetHalfHeight();
        }

        transform.position = finalPos;

        isBeingThrown = false;
    }

    private float GetHalfHeight()
    {
        if (halfHeight >= 0f) return halfHeight;

        var rend = GetComponentInChildren<Renderer>();
        if (rend) { halfHeight = rend.bounds.extents.y; return halfHeight; }

        var col = GetComponentInChildren<Collider>();
        if (col) { halfHeight = col.bounds.extents.y; return halfHeight; }

        halfHeight = 0.05f; 
        return halfHeight;
    }
}
