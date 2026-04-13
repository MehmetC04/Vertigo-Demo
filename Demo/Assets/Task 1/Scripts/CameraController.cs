using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    private Camera cam;
    private Vector3 defaultPos;
    private float defaultSize;

    private Vector3 targetPos;
    private float targetSize;

    [Header("Zoom Settings")]
    [SerializeField] private float smoothSpeed = 8f; // Kayma ve yakýnlaþma hýzý
    [SerializeField] private float padding = 2f;     // Eþyanýn etrafýnda býrakýlacak boþluk (Çok yakýnsa bunu artýr)
    [SerializeField] private float minZoom = 1.5f;   // Çok küçük objelerde kameranýn girebileceði maksimum sýnýr

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        cam = GetComponent<Camera>();
        defaultPos = transform.position;
        defaultSize = cam.orthographicSize;

        targetPos = defaultPos;
        targetSize = defaultSize;
    }

    private void LateUpdate()
    {
        
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * smoothSpeed);
    }

    public void FocusOnBounds(Bounds bounds)
    {
   
        float screenRatio = (float)Screen.width / (float)Screen.height;
        float targetBoundsRatio = bounds.size.x / bounds.size.y;

        float newSize = 0f;
        if (targetBoundsRatio > screenRatio)
        {
           
            newSize = (bounds.size.x / 2f) / screenRatio;
        }
        else
        {
            
            newSize = bounds.size.y / 2f;
        }

        targetSize = Mathf.Max(newSize * padding, minZoom);

       
        targetPos = new Vector3(bounds.center.x, bounds.center.y, defaultPos.z);
    }

    public void ResetToDefault()
    {
        targetPos = defaultPos;
        targetSize = defaultSize;
    }
}