using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Habitacion : MonoBehaviour
{
    public Collider hab;
    private bool moss = false;

    public bool hasEntered(Collider other)
    {
        if (other.isTrigger)
        {
            return true;
            
        }
        return false;
    }
    
    
    
}
