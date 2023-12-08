using System;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using BehaviourAPI.UtilitySystems;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using State = BehaviourAPI.StateMachines.State;

enum ExplorerStates 
{
        Exploring,
        Painting,
        Watching,
        Advancing,
        Escaping,
        Hiding,
        Fainted,
}

public class ExplorerBehaviour : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int health = 50;

    [Header("Exploring")]
    [SerializeField] private List<Transform> explorePositions;
    [SerializeField] private int currentExploreIndex = 0;
    
    [Header("Painting")] 
    [SerializeField] private float paintingTime;
    
    [Header("Faint")] 
    [SerializeField] private float faintingTime;
    
    [Header("Watching")] 
    [SerializeField] private int rotation;
    [Header("General Variables")]
    
    [Header("Escaping")] 
    [SerializeField] private Transform escapePoint;
    
    [Header("Thinking bubble")]
    [SerializeField] ThinkingCloudBehaviour thinkingCloudBehaviour;
    
    [SerializeField] private Transform objective;
    
    public NavMeshAgent agent;
    private Animator _animator;
    private Vision _vision;
    private Detection _detection;
    
    private FSM _fsm;
    
    private UtilitySystem _us;
    private float _distance;
    private ExplorerStates _states;
    private Transform exploreObjective;

    private VariableFactor anxiety;
    private VariableFactor fear;
    private bool _stoppedFaint;

    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _vision = GetComponentInChildren<Vision>();
        _detection = GetComponentInChildren<Detection>();
        
        _fsm = new FSM();
        
        #region CREACION ESTADOS

        FunctionalAction exploringAction = new FunctionalAction(StartExploring, Exploring, null); //Estado Explorar
        State exploring = _fsm.CreateState(exploringAction);

        FunctionalAction watchingAction = new FunctionalAction(StartWatching, Watching, null); //Estado Mirar
        State watching = _fsm.CreateState(watchingAction);
        
        FunctionalAction advancingAction = new FunctionalAction(StartAdvancing, Advancing, null); //Estado Avanzar
        State advancing = _fsm.CreateState(advancingAction);
        
        FunctionalAction paintingAction = new FunctionalAction(StartPainting, Painting, null); //Estado Pintar
        State painting = _fsm.CreateState(paintingAction);

        FunctionalAction escapingAction = new FunctionalAction(StartEscaping, Escaping, null); //Estado Escapar
        State escaping = _fsm.CreateState(escapingAction);
        
        FunctionalAction faintingAction = new FunctionalAction(StartFainting, Fainting, null); //Estado Desmayarse
        State fainting = _fsm.CreateState(faintingAction);
        
        #endregion 

        
        #region CREACION PERCECPCIONES ESTADOS

        ConditionPerception escapeDistance = new ConditionPerception(null, IsAtEscapeDistance, null);
        
        ConditionPerception faintDistance = new ConditionPerception(null, IsAtFaintDistance, null);
        
        ConditionPerception detectObjective = new ConditionPerception(null, IsDetectingObjective, null);
        
        ConditionPerception watchObjective = new ConditionPerception(null, IsWatchingObjective, null);
        
        ConditionPerception onObjective = new ConditionPerception(null, IsOnEmpty, null);
        
        ConditionPerception onWall = new ConditionPerception(null, IsOnWall, null);
        
        ConditionPerception emptyWall = new ConditionPerception(null, IsEmptyWall, null);
        
        ConditionPerception paintedWall = new ConditionPerception(null, IsPaintedWall, null);
        
        ConditionPerception stopEscaping = new ConditionPerception(null, StopEscape, null);
        
        ConditionPerception stopFainting = new ConditionPerception(null, StopFaint, null);
        
        AndPerception canPaint = new AndPerception(emptyWall, onWall);
        
        AndPerception stopPaint = new AndPerception(paintedWall, onWall);
        
        OrPerception stopAdvance = new OrPerception(onObjective, stopPaint);

        #endregion

        
        #region CREACION TRANSICIONES ESTADOS
        
        _fsm.CreateTransition(exploring, watching, detectObjective, statusFlags: StatusFlags.Running);
        
        _fsm.CreateTransition(watching, advancing, watchObjective, statusFlags: StatusFlags.Running);
        
        _fsm.CreateTransition(advancing, painting, canPaint, statusFlags: StatusFlags.Running);
        
        _fsm.CreateTransition(painting, exploring, stopPaint, statusFlags: StatusFlags.Running);
        
        _fsm.CreateTransition(advancing, exploring, stopAdvance, statusFlags: StatusFlags.Running);
        
        _fsm.CreateTransition(exploring, escaping, escapeDistance, statusFlags: StatusFlags.Running);
        
        _fsm.CreateTransition(watching, escaping, escapeDistance, statusFlags: StatusFlags.Running);
        
        _fsm.CreateTransition(advancing, escaping, escapeDistance, statusFlags: StatusFlags.Running);
        
        _fsm.CreateTransition(painting, escaping, escapeDistance, statusFlags: StatusFlags.Running);
        
        _fsm.CreateTransition(exploring, fainting, faintDistance, statusFlags: StatusFlags.Running);
        
        _fsm.CreateTransition(watching, fainting, faintDistance, statusFlags: StatusFlags.Running);
        
        _fsm.CreateTransition(advancing, fainting, faintDistance, statusFlags: StatusFlags.Running);
        
        _fsm.CreateTransition(painting, fainting, faintDistance, statusFlags: StatusFlags.Running);
        
        _fsm.CreateTransition(escaping, fainting, faintDistance, statusFlags: StatusFlags.Running);

        _fsm.CreateTransition(escaping, exploring, stopEscaping, statusFlags: StatusFlags.Running);
        
        _fsm.CreateTransition(fainting, exploring, stopFainting, statusFlags: StatusFlags.Running);
        #endregion

        
        _fsm.SetEntryState(exploring);
        _fsm.Start();
    }

    void Update()
    {
        _fsm.Update();
    }
    
    void ChangePatrolPoint(int change)
    {
        if(explorePositions.Count == 0)
        {
            return;
        }
        currentExploreIndex += change;
        currentExploreIndex %= explorePositions.Count;
        agent.SetDestination(explorePositions[currentExploreIndex].position);
        if(currentExploreIndex < 0)
        {
            currentExploreIndex = explorePositions.Count - 1;
        }
    }

    void CalculateRotation(Transform a)
    {
        Vector3 posA = transform.position;
        Vector3 posB = a.transform.position;
        Vector3 A = posB - posA;
        Vector3 B = transform.forward;
        float angle = Vector3.SignedAngle(A, B, Vector3.up);
        if (angle > 0)
        {
            rotation = -Mathf.Abs(rotation);
        }
        else
        {
            rotation = Mathf.Abs(rotation);
        }
    }

    #region METODOS MAQUINA ESTADOS IMPLEMENTACION
    void StartExploring()
    {
        thinkingCloudBehaviour.UpdateCloud(0);
        Debug.Log("Estoy explorando");
        agent.isStopped = false;
    }

    public Status Exploring()
    {
        if (IsPathComplete())
        {
            if (_states != ExplorerStates.Exploring)
            {
                ChangePatrolPoint(0);
            }
            else
            {
                ChangePatrolPoint(1);
            }
        }
        _states = ExplorerStates.Exploring;
        return Status.Running;
    }
    
    void StartPainting()
    {
        _states = ExplorerStates.Painting;
        Debug.Log("Estoy pintando");
        thinkingCloudBehaviour.UpdateCloud(0);
        agent.isStopped = true;
    }

    public Status Painting()
    {
        var wallController = objective.GetComponent<WallController>();
        if (wallController != null)
        {
            DOVirtual.DelayedCall(paintingTime, () => wallController.Paint());
        }
        return Status.Running;
    }
    
    void StartWatching()
    {
        _states = ExplorerStates.Watching;
        Debug.Log("Estoy buscando el objetivo de interes");
        thinkingCloudBehaviour.UpdateCloud(0);
        agent.isStopped = true;
        CalculateRotation(objective);
    }

    public Status Watching()
    {
        transform.Rotate(0, rotation * Time.deltaTime, 0);
        return Status.Running;
    }
    
    void StartAdvancing()
    {
        _states = ExplorerStates.Advancing;
        Debug.Log("Estoy yendo hacia el objetivo de interes");
        thinkingCloudBehaviour.UpdateCloud(0);
        agent.isStopped = false;
    }

    public Status Advancing()
    {
        agent.SetDestination(objective.position);
        return Status.Running;
    }
    
    void StartFainting()
    {
        _states = ExplorerStates.Fainted;
        Debug.Log("Me desmayo");
        thinkingCloudBehaviour.UpdateCloud(0);
        agent.isStopped = true;
    }

    public Status Fainting()
    {
        StartCoroutine(Faint());
        return Status.Running;
    }

    private IEnumerator Faint()
    {
        yield return new WaitForSeconds(faintingTime);
        _stoppedFaint = true;
        agent.isStopped = false;
    }
    
    void StartEscaping()
    {
        _states = ExplorerStates.Escaping;
        Debug.Log("Empiezo a escapar");
        thinkingCloudBehaviour.UpdateCloud(0);
        agent.isStopped = false;
    }

    public Status Escaping()
    {
        agent.SetDestination(escapePoint.position);
        return Status.Running;
    }
    #endregion
    
    #region COMPROBACIONES
    
    bool IsEmptyWall()
    {
        var wallController = objective.GetComponent<WallController>();
        if(wallController!=null)
        {
            if (!wallController.IsPainted())
            {
                return true;
            }
        }

        return false;
    }
    
    bool IsPaintedWall()
    {
        var wallController = objective.GetComponent<WallController>();
        if (wallController!=null)
        {
            if (wallController.IsPainted())
            {
                return true;
            }
        }

        return false;
    }
    
    bool IsWatchingObjective()
    {
        foreach(var a in _vision.VisibleTriggers)
        {
            if (a == objective) // ||a.GetComponent<BeastBehaviour>() || a.GetComponent<WallController>()
            {
                Debug.Log("He visualizado lo que he");
                return true;
            }
        }
        return false;
    }
    
    bool IsDetectingObjective()
    {
        foreach(var a in _detection.DetectableTriggers)
        {
            if (a.GetComponent<WallController>())
            {
                if (!a.GetComponent<WallController>().IsPainted())
                {
                    objective = a;
                    return true;
                }
            }
        }
        return false;
    }

    bool IsOnEmpty()
    {
        return IsPathComplete() && !objective.GetComponent<WallController>();
    }
    
    bool IsOnWall()
    {
        return IsPathComplete() && objective.GetComponent<WallController>();
    }
    
    bool IsPathComplete()
    {
        return (!agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance &&
                (!agent.hasPath || agent.velocity.sqrMagnitude == 0f));
    }

    bool IsAtFaintDistance()
    {
        var dangerObjects = FindObjectsOfType<DangerObject>();
        foreach (var a in dangerObjects)
        {
            if (a.GetDistance() < 2)
            {
                if (_vision.VisibleTriggers.Contains(a.transform))
                {
                    return true;
                }
            }
        }
        return false;
    }

    bool StopFaint()
    {
        var dangerObjects = FindObjectsOfType<DangerObject>();
        foreach (var a in dangerObjects)
        {
            if (a.GetDistance() < 5)
            {
                if (_vision.VisibleTriggers.Contains(a.transform))
                {
                    return false;
                }
            }
        }

        if (_stoppedFaint)
        {
            _stoppedFaint = false;
            return true;
        }
        return false;
    }

    bool IsAtEscapeDistance()
    {
        var dangerObjects = FindObjectsOfType<DangerObject>();
        foreach (var a in dangerObjects)
        {
            if (a.GetDistance() < 10)
            {
                if (_vision.VisibleTriggers.Contains(a.transform))
                {
                    return true;
                }
            }
        }
        return false;

    }
    
    bool StopEscape()
    {
        var dangerObjects = FindObjectsOfType<DangerObject>();
        foreach (var a in dangerObjects)
        {
            if (a.GetDistance() > 15)
            {
                if (_vision.VisibleTriggers.Contains(a.transform))
                {
                    return true;
                }
            }
        }
        return IsPathComplete();
    }
    
    #endregion
    
    
}

