using UnityEngine;

public class Scaner : MonoBehaviour
{
    [Header("Scanner Settings")]
    public Vector3 scanSize = new Vector3(0.5f, 0.1f, 0.1f);
    public float scanDistance = 3f;
    public LayerMask scanLayerMask = -1;

    private Camera mainCamera;
    private float fixedX;

    void Start()
    {
        mainCamera = Camera.main;
        fixedX = transform.position.x; // Автоматически берем текущий X
    }

    private void Update()
    {
        ScanItem();
        FollowCursorYZ();
    }
    
    private void ScanItem()
{
    Collider[] hitColliders = Physics.OverlapBox(
        transform.position + transform.forward * scanDistance * 0.5f,
        scanSize * 0.5f,
        transform.rotation,
        scanLayerMask
    );

    foreach (Collider collider in hitColliders)
    {
        if (collider.isTrigger && collider.CompareTag("Code"))
        {
            Item item = GameManager.Instance.currentItem.GetComponent<Item>();
            if (item != null)
            {
                GameManager.Instance.ShowScanResult();
                gameObject.SetActive(false);
                return;
            }
        }
    }
}
    
    private void FollowCursorYZ()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = mainCamera.WorldToScreenPoint(transform.position).z;
        
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        
        transform.position = new Vector3(fixedX, worldPos.y, worldPos.z);
    }
    
private void OnDrawGizmosSelected()
{
    Gizmos.color = Color.blue;
    
    // Сохраняем текущую матрицу
    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
    
    // Рисуем BoxCast правильно
    Vector3 boxCenter = Vector3.forward * scanDistance * 0.5f;
    Gizmos.DrawWireCube(boxCenter, scanSize);
    
    // Рисуем луч от начала до конца BoxCast
    Gizmos.DrawRay(Vector3.zero, Vector3.forward * scanDistance);
    
    // Дополнительно: рисуем начальную позицию BoxCast
    Gizmos.color = Color.red;
    Gizmos.DrawWireCube(Vector3.zero, scanSize);
}
}