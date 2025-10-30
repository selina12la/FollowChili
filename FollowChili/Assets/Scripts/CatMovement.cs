using UnityEngine;
using System.Collections;

public class CatMovement : MonoBehaviour
{
    public float moveSpeed = 0.5f;       // Geschwindigkeit des Objekts
    public float waitTime = 1.0f;        // Wartezeit an Zielpunkten
    public float moveRange = 1.5f;       // Radius um den Startpunkt, in dem sich das Objekt bewegt
    public float rotationSpeed = 5f;     // Drehgeschwindigkeit
 
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isMoving = false;
 
    void Start()
    {
        startPosition = transform.position;
        StartCoroutine(MoveRoutine());
    }
 
    IEnumerator MoveRoutine()
    {
        while (true)
        {
            if (!isMoving)
            {
                // Neues zufälliges Ziel innerhalb des moveRange auswählen
                Vector2 randomCircle = Random.insideUnitCircle * moveRange;
                targetPosition = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
                isMoving = true;
            }
 
            // Bewegung zum Ziel
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
 
            // Rotation: nach vorne schauen, wohin es sich bewegt
            Vector3 direction = targetPosition - transform.position;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
 
            // Prüfen, ob Ziel erreicht wurde
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isMoving = false;
                yield return new WaitForSeconds(waitTime); // kurz warten
            }
 
            yield return null;
        }
    }
}
