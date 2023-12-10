using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DangerObject : MonoBehaviour
{
    private float _distanceFromExplorer;

    // Update is called once per frame
    void Update()
    {
        _distanceFromExplorer =
            Vector3.Distance(FindObjectOfType<ExplorerBehaviour>().transform.position, transform.position);

    }

    public float GetDistance()
    {
        return _distanceFromExplorer;
    }
}
