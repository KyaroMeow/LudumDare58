using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Interaction Settings")]
    public Transform holdPosition;
    public LayerMask interactableLayer;
    public GameObject HUD;
    public UVLighter uVLighter;
    public GameObject scaner;
    public ParticleSystem particle;
    
    [Header("Inspect Settings")]
    public float inspectRotationSpeed = 20f;
    public float rotationSpeedLimit = 500f;
    
    private Item heldItem;
    
    private Camera playerCamera;
    private Vector3 originalItemPosition;
    private Quaternion originalItemRotation;
    private Transform originalItemParent;
    private Vector2 lastMousePosition;
    private PlayerView playerView;
    private bool isItemRotating = false;


    private void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        playerView = GetComponent<PlayerView>();
    }

    void Update()
    {
        HandleInput();
        HandleInspect();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            DropItem();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (heldItem == null)
            {
                TryPickupItem();
            }
        }
    }
    
    private void HandleInspect()
    {
        if (heldItem != null)
        {
            
            
            if (Input.GetMouseButton(0))
            {
                Vector2 currentMousePosition = Input.mousePosition;
                Vector2 mouseDelta = currentMousePosition - lastMousePosition;

                float rotationSpeed = mouseDelta.magnitude / Time.deltaTime;

                if (rotationSpeed > rotationSpeedLimit && isItemRotating == true)
                {
                    DestroyItem(); 
                }

                heldItem.transform.Rotate(playerCamera.transform.up, -mouseDelta.x * inspectRotationSpeed * Time.deltaTime, Space.World);
                heldItem.transform.Rotate(playerCamera.transform.right, mouseDelta.y * inspectRotationSpeed * Time.deltaTime, Space.World);
                isItemRotating = true;
                lastMousePosition = currentMousePosition;
            }
            else
            {
                lastMousePosition = Input.mousePosition;
            }
        }
    }
    
    private void TryPickupItem()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 10f, interactableLayer))
        {
            Item item = hit.collider.GetComponent<Item>();
            if (item != null)
            {
                PickupItem(item);
            }
        }
    }
    private void DestroyItem()
    {
        particle.Play();
        GameManager.Instance.WrongSort();
    }
    private void PickupItem(Item item)
    {
        playerView.canRotate = false;
        HUD.SetActive(true);
        heldItem = item;
        
        // Save item position
        originalItemPosition = item.transform.position;
        originalItemRotation = item.transform.rotation;
        originalItemParent = item.transform.parent;
        
        // Move item to hold position
        item.transform.SetParent(holdPosition);
        item.transform.localPosition = Vector3.zero;
        
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    public void DropItem()
    {
        if (heldItem != null)
        {
            heldItem.transform.SetParent(originalItemParent);
            heldItem.transform.position = originalItemPosition;
            heldItem.transform.rotation = originalItemRotation;

            Rigidbody rb = heldItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            heldItem = null;
        }
        isItemRotating = false;
        playerView.canRotate = true;
        scaner.SetActive(false);
        HUD.SetActive(false);
        uVLighter.ToggleLighterOff();
    }
    

}