using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DangerObject : MonoBehaviour
{
   
    public float GetDistance()
    {
        return Vector3.Distance(FindObjectOfType<ExplorerBehaviour>().transform.position, transform.position);
    }
}
