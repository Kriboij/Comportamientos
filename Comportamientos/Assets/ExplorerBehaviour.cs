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
    private List<Transform> explorePositions;
    private int currentExploreIndex = 0;
    
    [Header("Painting")] 
    
    [Header("Faint")] 

    [Header("General Variables")]
    
    [Header("Thinking bubble")]
    [SerializeField]
    ThinkingCloudBehaviour thinkingCloudBehaviour;
    
    public NavMeshAgent agent;
    private Animator animator;
    private Vision vision;
    FSM fsm;
    private Transform objective;
    private Vector3 wallPosition;
    private float paintTime;
    private float paintingTime;

    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        vision = GetComponentInChildren<Vision>();
            
        fsm = new FSM();

        FunctionalAction exploringAction = new FunctionalAction(StartExplore, Exploring, null); //Estado
        State exploring = fsm.CreateState(exploringAction);

        FunctionalAction watchingAction = new FunctionalAction(StartWatching, Watching, null); //Estado
        State watching = fsm.CreateState(watchingAction);
        
        FunctionalAction advancingAction = new FunctionalAction(StartAdvancing, Advancing, null); //Estado
        State advancing = fsm.CreateState(watchingAction);
        
        FunctionalAction paintingAction = new FunctionalAction(StartPainting, Painting, null); //Estado
        State painting = fsm.CreateState(paintingAction);
        
        ConditionPerception detectObjective = new ConditionPerception(null, IsDetectingObjective, null);
        ConditionPerception watchObjective = new ConditionPerception(null, IsWatchingObjective, null);
        ConditionPerception onObjective = new ConditionPerception(null, IsOnEmpty, null);
        ConditionPerception onWall = new ConditionPerception(null, IsOnWall, null);
        ConditionPerception emptyWall = new ConditionPerception(null, IsEmptyWall, null);
        ConditionPerception paintedWall = new ConditionPerception(null, IsPaintedWall, null);
        AndPerception canPaint = new AndPerception(emptyWall, onWall);
        AndPerception stopPaint = new AndPerception(paintedWall, onWall);
        AndPerception stopAdvance = new AndPerception(onObjective);
        fsm.CreateTransition(exploring, watching, detectObjective, statusFlags: StatusFlags.Running);
        fsm.CreateTransition(watching, advancing, watchObjective, statusFlags: StatusFlags.Running);
        fsm.CreateTransition(advancing, painting, canPaint, statusFlags: StatusFlags.Running); // 1.Dde donde partimos, 2. Estado al que pasamos, 3. Condicion 1, 4. Condicion 2
        fsm.CreateTransition(painting, exploring, stopPaint, statusFlags: StatusFlags.Running);
        fsm.CreateTransition(advancing, exploring, stopAdvance, statusFlags: StatusFlags.Running);

        fsm.SetEntryState(exploring);
        fsm.Start();
        
        
        /*
        FunctionalAction escapingAction = new FunctionalAction(StartEscaping, Escaping, null); //Estado
        State escaping = fsm.CreateState(escapingAction);
        
        FunctionalAction hidingAction = new FunctionalAction(StartExplore, Exploring, null); //Estado
        State hiding = fsm.CreateState(hidingAction);

        FunctionalAction faintingAction = new FunctionalAction(StartPainting, Painting, null); //Estado
        State fainting = fsm.CreateState(faintingAction);
         */
    }

    void Update()
    {
        fsm.Update();
    }

    void StartExplore()
    {
        thinkingCloudBehaviour.UpdateCloud(0);
        agent.isStopped = false;
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
        thinkingCloudBehaviour.UpdateCloud(1);
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
        thinkingCloudBehaviour.UpdateCloud(2);
        agent.isStopped = true;
    }

    public Status Watching()
    {
        transform.Rotate(0, 1, 0);
        return Status.Running;
    }
    
    void StartAdvancing()
    {
        thinkingCloudBehaviour.UpdateCloud(3);
        agent.isStopped = false;
    }

    public Status Advancing()
    {
        agent.SetDestination(objective.position);
        return Status.Running;
    }
    
    /*
    
    void StartEscaping()
    {
        
    }

    public Status Escaping()
    {
    }
    
    void StartHiding()
    {
        
    }

    public Status Hiding()
    {
    }
    
    void StartFainting()
    {
        
    }

    public Status Fainting()
    {
    }
    */
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
            if (a.GetComponent<PoliceBehaviour>()) // ||a.GetComponent<BeastBehaviour>() || a.GetComponent<WallController>()
            {
                objective = a;
                return true;
            }
        }

        return false;
    }
    
    bool IsDetectingObjective()
    {
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
        if(explorePositions.Count == 0)
        {
            return;
        }
        agent.SetDestination(explorePositions[currentExploreIndex].position);
        currentExploreIndex += change;
        currentExploreIndex %= explorePositions.Count;
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

