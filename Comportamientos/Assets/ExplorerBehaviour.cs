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
    
    [Header("Faint")] 

    [Header("General Variables")]
    
    [Header("Thinking bubble")]
    [SerializeField]
    ThinkingCloudBehaviour thinkingCloudBehaviour;
    
    public NavMeshAgent agent;
    private Animator animator;
    private Vision vision;
    private Detection detection;
    private FSM fsm;
    private UtilitySystem us;
    [SerializeField] private Transform objective;
    private Vector3 wallPosition;
    private float paintTime;
    private float paintingTime;
    private int rotation = 1;
    private float distance;

    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        vision = GetComponentInChildren<Vision>();
        detection = GetComponentInChildren<Detection>();
        
        fsm = new FSM();
        us = new UtilitySystem();

        #region SISTEMA DE UTILIDAD

        VariableFactor anxiety = us.CreateVariable(() => distance, 0f, 1f);
        VariableFactor fear = us.CreateVariable(() => distance, 0f, 1f);

        ExponentialCurveFactor fearCurve = us.CreateCurve<ExponentialCurveFactor>(fear);
        fearCurve.Exponent = 2;
        fearCurve.DespX = -0.2f;

        SigmoidCurveFactor anxietyCurve = us.CreateCurve<SigmoidCurveFactor>(anxiety);
        anxietyCurve.GrownRate = -0.3f;
        anxietyCurve.Midpoint = 0.5f;

        #endregion
        
        
        #region ESTADOS
        
        FunctionalAction faintAction = new FunctionalAction(StartFainting, Fainting, null);
        UtilityAction faintUtilityAction = us.CreateAction(fearCurve, faintAction);
        
        FunctionalAction anxietyAction = new FunctionalAction(StartEscaping, Escaping, null);
        UtilityAction anxietyUtilityAction = us.CreateAction(fearCurve, faintAction);

        FunctionalAction exploringAction = new FunctionalAction(StartExploring, Exploring, null); //Estado
        State exploring = fsm.CreateState(exploringAction);

        FunctionalAction watchingAction = new FunctionalAction(StartWatching, Watching, null); //Estado
        State watching = fsm.CreateState(watchingAction);
        
        FunctionalAction advancingAction = new FunctionalAction(StartAdvancing, Advancing, null); //Estado
        State advancing = fsm.CreateState(watchingAction);
        
        FunctionalAction paintingAction = new FunctionalAction(StartPainting, Painting, null); //Estado
        State painting = fsm.CreateState(paintingAction);
        
        #endregion 

        
        #region PERCECPCIONES ESTADOS
        
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

        
        #region TRANSICIONES ESTADOS
        
        fsm.CreateTransition(exploring, watching, detectObjective, statusFlags: StatusFlags.Running);
        
        fsm.CreateTransition(watching, advancing, watchObjective, statusFlags: StatusFlags.Running);
        
        fsm.CreateTransition(advancing, painting, canPaint, statusFlags: StatusFlags.Running);
        
        fsm.CreateTransition(painting, exploring, stopPaint, statusFlags: StatusFlags.Running);
        
        fsm.CreateTransition(advancing, exploring, stopAdvance, statusFlags: StatusFlags.Running);
        
        #endregion

        
        fsm.SetEntryState(exploring);
        fsm.Start();
        us.Start();
    }

    void Update()
    {
        us.Update();
        fsm.Update();
    }

    void StartExploring()
    {
        thinkingCloudBehaviour.UpdateCloud(0);
        //agent.isStopped = false;
    }

    public Status Exploring()
    {
        if (isPathComplete())
        {
            ChangePatrolPoint(1);
        }
        return Status.Running;
    }
    
    void StartPainting()
    {
        //thinkingCloudBehaviour.UpdateCloud(1);
        agent.isStopped = true;
    }

    public Status Painting()
    {
        var wallController = objective.GetComponent<WallController>();
        if (wallController != null)
        {
            wallController.Paint();
        }
        return Status.Running;
    }
    
    void StartWatching()
    {
        //thinkingCloudBehaviour.UpdateCloud(2);
        agent.isStopped = true;
    }

    public Status Watching()
    {
        Debug.Log(rotation);
        transform.Rotate(0, rotation, 0);
        return Status.Running;
    }
    
    void StartAdvancing()
    {
        //thinkingCloudBehaviour.UpdateCloud(3);
        Debug.Log("Empiezo a avanzar");
        agent.isStopped = false;
    }

    public Status Advancing()
    {
        agent.SetDestination(objective.position);
        return Status.Running;
    }
    
    void StartFainting()
    {
    }

    public Status Fainting()
    {
        return Status.Running;
    }
    
    void StartEscaping()
    {
    }

    public Status Escaping()
    {
        return Status.Running;
    }
    
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
        foreach(var a in vision.VisibleTriggers)
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
        foreach(var a in detection.DetectableTriggers)
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
        return isPathComplete() && !objective.GetComponent<WallController>();
    }
    
    bool IsOnWall()
    {
        return isPathComplete() && objective.GetComponent<WallController>();
    }
    
    void ChangePatrolPoint(int change)
    {
        Debug.Log("Estoy cambiando");
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

    bool isPathComplete()
    {
        return (!agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance &&
                (!agent.hasPath || agent.velocity.sqrMagnitude == 0f));
    }
    
    
}

