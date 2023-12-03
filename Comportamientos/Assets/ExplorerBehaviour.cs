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
using UnityEngine;
using UnityEngine.AI;
using Vector3 = System.Numerics.Vector3;

enum ExplorerStates 
{
        Exploring,
        Painting,
        Watching,
        AdvanceTo,
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
    private Vector3 position;
    
    private FSM _fsm;
    
    private UtilitySystem _us;
    private float _distance;

    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _vision = GetComponentInChildren<Vision>();
        _detection = GetComponentInChildren<Detection>();
        
        _fsm = new FSM();
        _us = new UtilitySystem();
        

        #region CREACION SISTEMA DE UTILIDAD

        VariableFactor anxiety = _us.CreateVariable(() => _distance, 0f, 1f);
        VariableFactor fear = _us.CreateVariable(() => _distance, 0f, 1f);

        ExponentialCurveFactor fearCurve = _us.CreateCurve<ExponentialCurveFactor>(fear);
        fearCurve.Exponent = 2;
        fearCurve.DespX = -0.2f;

        SigmoidCurveFactor anxietyCurve = _us.CreateCurve<SigmoidCurveFactor>(anxiety);
        anxietyCurve.GrownRate = -0.3f;
        anxietyCurve.Midpoint = 0.5f;
        
        FunctionalAction faintAction = new FunctionalAction(StartFainting, Fainting, null);
        UtilityAction faintUtilityAction = _us.CreateAction(fearCurve, faintAction);
        
        FunctionalAction anxietyAction = new FunctionalAction(StartEscaping, Escaping, null);
        UtilityAction anxietyUtilityAction = _us.CreateAction(fearCurve, faintAction);

        #endregion
        
        
        #region CREACION ESTADOS

        FunctionalAction exploringAction = new FunctionalAction(StartExploring, Exploring, null); //Estado
        State exploring = _fsm.CreateState(exploringAction);

        FunctionalAction watchingAction = new FunctionalAction(StartWatching, Watching, null); //Estado
        State watching = _fsm.CreateState(watchingAction);
        
        FunctionalAction advancingAction = new FunctionalAction(StartAdvancing, Advancing, null); //Estado
        State advancing = _fsm.CreateState(watchingAction);
        
        FunctionalAction paintingAction = new FunctionalAction(StartPainting, Painting, null); //Estado
        State painting = _fsm.CreateState(paintingAction);
        
        #endregion 

        
        #region CREACION PERCECPCIONES ESTADOS
        
        ConditionPerception detectObjective = new ConditionPerception(null, IsDetectingObjective, null);
        
        ConditionPerception watchObjective = new ConditionPerception(null, IsWatchingObjective, null);
        
        ConditionPerception onObjective = new ConditionPerception(null, IsOnEmpty, null);
        
        ConditionPerception onWall = new ConditionPerception(null, IsOnWall, null);
        
        ConditionPerception emptyWall = new ConditionPerception(null, IsEmptyWall, null);
        
        ConditionPerception paintedWall = new ConditionPerception(null, IsPaintedWall, null);
        
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
        
        #endregion

        
        _fsm.SetEntryState(exploring);
        _fsm.Start();
        _us.Start();
    }

    void Update()
    {
        _distance = CalculateDistance();
        _us.Update();
        _fsm.Update();
    }

    private float CalculateDistance()
    {
        float[] distances = new float[4];
        
        var policePos = FindObjectOfType<PoliceBehaviour>().transform.position;
        var criminalPos = FindObjectOfType<CriminalBehaviour>().transform.position;
        //var beastPos = FindObjectOfType<BeastBehaviour>().transform.position;
        //var ghostPos = FindObjectOfType<GhostBehaviour>().transform.position;
        
        Vector3 policeVector = new Vector3(policePos.x, policePos.y, policePos.z);
        Vector3 criminalVector = new Vector3(criminalPos.x, criminalPos.y, criminalPos.z);
        //Vector3 beastVector = new Vector3(beastPos.x, beastPos.y, beastPos.z);
        //Vector3 ghostVector = new Vector3(ghostPos.x, ghostPos.y, ghostPos.z);
        
        distances[0] = Vector3.Distance (position, policeVector);
        distances[1] = Vector3.Distance (position, criminalVector);
        //distances[2] = Vector3.Distance (position, beastVector);
        //distances[3] = Vector3.Distance (position, ghostVector);

        return Mathf.Max(distances);
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


    #region METODOS MAQUINA ESTADOS IMPLEMENTACION
    void StartExploring()
    {
        thinkingCloudBehaviour.UpdateCloud(0);
        agent.isStopped = false;
    }

    public Status Exploring()
    {
        if (IsPathComplete())
        {
            ChangePatrolPoint(1);
        }
        return Status.Running;
    }
    
    void StartPainting()
    {
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
        thinkingCloudBehaviour.UpdateCloud(0);
        agent.isStopped = true;
    }

    public Status Watching()
    {
        Debug.Log(rotation);
        transform.Rotate(0, rotation * Time.deltaTime, 0);
        return Status.Running;
    }
    
    void StartAdvancing()
    {
        thinkingCloudBehaviour.UpdateCloud(0);
        agent.isStopped = false;
    }

    public Status Advancing()
    {
        agent.SetDestination(objective.position);
        return Status.Running;
    }
    
    #endregion
    

    #region METODOS SISTEMA DE UTILIDAD
    void StartFainting()
    {
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
        Destroy(this.gameObject);
    }
    
    void StartEscaping()
    {
        thinkingCloudBehaviour.UpdateCloud(0);
        agent.isStopped = false;
    }

    public Status Escaping()
    {
        agent.SetDestination(escapePoint.position);
        return Status.Running;
    }
    
    #endregion IMPLEMENTACION
    
    
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
            if (a.GetComponent<WallController>()) // ||a.GetComponent<BeastBehaviour>() || a.GetComponent<WallController>()
            {
                objective = a;
                return true;
            }
        }
        Debug.Log("No lo veo");
        return false;
    }
    
    bool IsDetectingObjective()
    {
        foreach(var a in _detection.DetectableTriggers)
        {
            if (a.GetComponent<WallController>())
            {
                Debug.Log("Detecto");
                return true;
            }
        }
        Debug.Log("No Detecto");
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
    
    #endregion
    
    
}

