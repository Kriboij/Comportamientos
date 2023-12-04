using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vision : MonoBehaviour
{

    public List<Transform> VisibleTriggers;

    [SerializeField] LayerMask sceneMask;

    private void Awake()
    {
        VisibleTriggers = new List<Transform>();
    }

    private void OnTriggerStay(Collider other)
    {
        VisionTrigger visionTrigger = other.GetComponent<VisionTrigger>();

        if (visionTrigger != null)
        {
            Vector3 direction = visionTrigger.transform.position - transform.position;
            float raycastRange = direction.magnitude;
            if (Physics.Raycast(transform.position, direction.normalized, out var hit, raycastRange,sceneMask))
            {
                Debug.Log("Remove");
                VisibleTriggers.Remove(visionTrigger.Body);
            }
            else
            {
                Debug.Log("Add");
                if (!VisibleTriggers.Contains(visionTrigger.Body))
                {
                    VisibleTriggers.Add(visionTrigger.Body);
                }
            }


        }
    }

    private void OnTriggerExit(Collider other)
    {
        VisionTrigger visionTrigger = other.GetComponent<VisionTrigger>();


        if (visionTrigger != null)
        {
            VisibleTriggers.Remove(visionTrigger.Body);
        }
    }
}
