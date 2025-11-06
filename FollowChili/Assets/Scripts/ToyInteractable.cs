using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(Collider))]
public class ToyInteractable : MonoBehaviour
{
    [Header("Refs (werden automatisch gesetzt)")]
    public ARRaycastManager arRaycastManager;
    public Camera mainCamera;

    [Header("Events")]
    public Action<Vector3> OnReleased;

    // Drag-Status
    private bool isBeingDragged = false;
    private Vector3 grabOffset = Vector3.zero;

    // Boden-/Höhen-Handling
    private float halfHeight = -1f;   // halbe Objekt-Höhe (Renderer/Collider)
    private float lastPlaneY = 0f;    // letzte bekannte AR-Plane-Höhe
    private bool  hasPlaneY = false;

    private static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

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

        // Falls ein Rigidbody existiert, initial defensiv gegen "Durchfallen" konfigurieren
        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            // In AR-Szenen haben Planes meist keine Collider – deshalb nicht fallen lassen.
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        // Maus (Editor)
        if (Input.GetMouseButtonDown(0)) TryBeginGrab(Input.mousePosition);
        else if (Input.GetMouseButton(0) && isBeingDragged) ContinueDrag(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0) && isBeingDragged) EndGrab();
#else
        // Touch (Mobil)
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if      (t.phase == TouchPhase.Began)                      TryBeginGrab(t.position);
            else if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) && isBeingDragged)
                                                                      ContinueDrag(t.position);
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                                                                      EndGrab();
        }
#endif
    }

    // -------------------------
    // Dragging
    // -------------------------

    private void TryBeginGrab(Vector2 screenPos)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit.collider && (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)))
            {
                GetHalfHeight(); // Höhe cachen

                // Physik kontrollieren (wir bewegen manuell)
                var rb = GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                grabOffset = transform.position - hit.point;
                isBeingDragged = true;
            }
        }
    }

    private void ContinueDrag(Vector2 screenPos)
    {
        // 1) AR-Plane getroffen -> exakt auf Plane (plus halbe Höhe)
        if (arRaycastManager != null && arRaycastManager.Raycast(screenPos, s_Hits, TrackableType.Planes) && s_Hits.Count > 0)
        {
            var pose = s_Hits[0].pose;
            lastPlaneY = pose.position.y;
            hasPlaneY = true;

            Vector3 newPos = pose.position + pose.up * GetHalfHeight();
            // seitlichen Offset beibehalten (ohne die Höhe zu verändern)
            newPos += Vector3.ProjectOnPlane(grabOffset, pose.up);
            transform.position = newPos;
            return;
        }

        // 2) Kein AR-Hit -> Strahl auf Drag-Ebene projizieren (Höhe der letzten Plane / aktuelle Toy-Höhe)
        if (mainCamera == null) mainCamera = Camera.main;
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        float planeY = hasPlaneY ? lastPlaneY : transform.position.y;
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 p = ray.GetPoint(enter);
            // niemals unter die Ebene
            p.y = planeY + GetHalfHeight();
            p += Vector3.ProjectOnPlane(grabOffset, Vector3.up);
            transform.position = p;
        }
    }

    private void EndGrab()
    {
        isBeingDragged = false;

        // Beim Loslassen: sauber auf Ebene "snappen"
        if (hasPlaneY)
        {
            Vector3 p = transform.position;
            p.y = lastPlaneY + GetHalfHeight();
            transform.position = p;
        }

        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            // Standard in AR ohne Plane-Collider: liegen lassen
            rb.isKinematic = true;
            rb.useGravity  = false;

            // Wenn du ARPlaneMeshCollider nutzt und echte Physik willst:
            // rb.isKinematic = false;
            // rb.useGravity  = true;
        }

        OnReleased?.Invoke(transform.position);
    }

    // -------------------------
    // Helpers
    // -------------------------

    private float GetHalfHeight()
    {
        if (halfHeight >= 0f) return halfHeight;

        var rend = GetComponentInChildren<Renderer>();
        if (rend) { halfHeight = rend.bounds.extents.y; return halfHeight; }

        var col = GetComponentInChildren<Collider>();
        if (col) { halfHeight = col.bounds.extents.y; return halfHeight; }

        halfHeight = 0.05f; // Fallback
        return halfHeight;
    }
}
