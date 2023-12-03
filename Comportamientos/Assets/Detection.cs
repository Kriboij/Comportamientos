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
        DetectableTriggers.Add(other.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        DetectableTriggers.Remove(other.transform);
    }
}