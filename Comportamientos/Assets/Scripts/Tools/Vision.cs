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
                if(hit.collider.gameObject == visionTrigger.gameObject)
                {   
                    if (!VisibleTriggers.Contains(visionTrigger.Body))
                    {
                        VisibleTriggers.Add(visionTrigger.Body);
                    }
                }    
                
            }
            else
            {
                
                VisibleTriggers.Remove(visionTrigger.Body);
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

    void CleanTriggers()
    {
        for (int i = 0; i < VisibleTriggers.Count; i++)
        {
            if (VisibleTriggers[i] == null)
            {
                VisibleTriggers.RemoveAt(i);
                i--;
            }
        }
    }

    //COMPROBACIONES

    public bool IsWatchingPoliceman()
    {
        CleanTriggers();

        foreach (var trigger in VisibleTriggers)
        {
            var police = trigger.GetComponent<PoliceBehaviour>();
            if (police != null)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsWatchingCriminal()
    {

        CleanTriggers();

        foreach (var trigger in VisibleTriggers)
        {
            var criminal = trigger.GetComponent<CriminalBehaviour>();
            if (criminal != null)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsWatchingHuman()
    {


        if (IsWatchingPoliceman())
        {
            return true;
        }
        if (IsWatchingCriminal())
        {
            return true;
        }
        if (IsWatchingExplorer())
        {
            return true;
        }
        return false;
    }

    public bool IsWatchingGhost()
    {
        CleanTriggers();

        foreach (var trigger in VisibleTriggers)
        {
            var ghost = trigger.GetComponent<GhostBehaviour>();
            if (ghost != null)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsWatchingExplorer()
    {

        CleanTriggers();

        foreach (var trigger in VisibleTriggers)
        {
            var explorer = trigger.GetComponent<ExplorerBehaviour>();
            if (explorer != null)
            {
                return true;
            }
        }
        return false;
    }
}
