using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.AI;

public class WallController : MonoBehaviour
{
    private bool painted = false;

    public bool IsPainted()
    {
        return painted;
    }
    
    public void Paint()
    {
        painted = true;
    }
}