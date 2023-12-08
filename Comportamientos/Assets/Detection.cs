using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detection : MonoBehaviour
{

    public List<Transform> DetectableTriggers;

    [SerializeField] LayerMask sceneMask;

    private void Awake()
    {
        DetectableTriggers = new List<Transform>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Vector3 direction = other.transform.position - transform.position;
        float raycastRange = direction.magnitude;
        if (Physics.Raycast(transform.position, direction.normalized, out var hit, raycastRange,sceneMask))
        {
            if(hit.collider.gameObject == other.gameObject)
            {   
                if (!DetectableTriggers.Contains(other.transform))
                {
                    DetectableTriggers.Add(other.transform);
                }
            }    
                
        }
        DetectableTriggers.Add(other.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        DetectableTriggers.Remove(other.transform);
    }
}