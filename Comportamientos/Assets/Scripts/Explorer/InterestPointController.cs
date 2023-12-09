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

public class InterestPointController : MonoBehaviour
{
    private bool observed = false;

    public bool IsObnserved()
    {
        return observed;
    }
    
    public void Observe()
    {
        observed = true;
    }
}