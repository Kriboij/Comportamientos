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

public class CriminalBehaviour : MonoBehaviour
{

    [SerializeField] Vision vision;
    FSM fsm;

    PoliceBehaviour police;

    [SerializeField] float fleeTime;

    [Header("Health")]
    [SerializeField]
    private int health = 100;

    [Header("Patrol")]
    [SerializeField]
    private List<Transform> patrolPositions;
    private int currentPatrolIndex = 0;
    

    [Header("Thinking bubble")]
    [SerializeField]
    ThinkingCloudBehaviour thinkingCloudBehaviour;

    private NavMeshAgent agent;
    private Animator animator;


    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        fsm = new FSM();

        FunctionalAction patrollingAction = new FunctionalAction(StartPatrolling, Patrolling, null); //Estado
        State patrolling = fsm.CreateState(patrollingAction);

        FunctionalAction watchPoliceAction = new FunctionalAction(StartWatchPolice, WatchPolice, StopWatchPolice); //Estado
        ProbabilisticState watchPolice = fsm.CreateProbabilisticState(watchPoliceAction);

        ConditionPerception checkPolice = new ConditionPerception(null, vision.IsWatchingPoliceman, null); //Trancisión para la conexión
        fsm.CreateTransition(patrolling, watchPolice, checkPolice, statusFlags: StatusFlags.Running); // 1.Dde donde partimos, 2. Estado al que pasamos, 3. Condicion 1, 4. Condicion 2

        //Estados probabilisticos
        FunctionalAction fleeAction = new FunctionalAction(StartFlee, Fleeing, null); //Estado
        State flee = fsm.CreateState(fleeAction);

        //Estados probabilisticos
        FunctionalAction bribeAction = new FunctionalAction(StartBribing, Bribing, null); //Estado
        State bribe = fsm.CreateState(bribeAction);

        TimerPerception fleeTimer = new TimerPerception(fleeTime);
        TimerPerception bribeTimer = new TimerPerception(fleeTime);

        var fleeTransition = fsm.CreateTransition(watchPolice, flee, fleeTimer, statusFlags: StatusFlags.Running); //Transicion Ver Policia y Huir
        var bribeTransition = fsm.CreateTransition(watchPolice, bribe, bribeTimer, statusFlags: StatusFlags.Running); //Transicion Ver Policia y Sobornar

        watchPolice.SetProbability(fleeTransition, 0.75f);
        watchPolice.SetProbability(bribeTransition, 0.25f);


        ConditionPerception noCheckPolice = new ConditionPerception(null,()=> { return !vision.IsWatchingPoliceman(); }, null);
        AndPerception timeAndNoWatch = new AndPerception(noCheckPolice, fleeTimer);

        fsm.CreateTransition(flee, patrolling, timeAndNoWatch, statusFlags: StatusFlags.Running); //Transicion huir y patrullar

        fsm.SetEntryState(patrolling);
        fsm.Start();
    }

    private void Update()
    {
        fsm.Update();
    }

    #region 1. PATROL STATE
    void StartPatrolling()
    {
        thinkingCloudBehaviour.UpdateCloud(0);
        agent.isStopped = false;
    }

    public Status Patrolling()
    {
        if (isPathComplete())
        {
            ChangePatrolPoint(1);
        }
        return Status.Running;

    }
    #endregion


    #region 2. WATCH STATE
    void StartWatchPolice()
    {
        thinkingCloudBehaviour.UpdateCloud(1);
        agent.isStopped = true;
    }

    void StopWatchPolice()
    {
        police = null;
        foreach(var trigger in vision.VisibleTriggers)
        {
            police = trigger.GetComponent<PoliceBehaviour>();
            if(police != null)
            {
                return;
            }
        }
    }

    public Status WatchPolice()
    {
        return Status.Running;
    }

    
    #endregion


    #region 3. FLEE STATE

    void StartFlee()
    {
        thinkingCloudBehaviour.UpdateCloud(2);
        agent.isStopped = false;
        ChangePatrolPoint(-1);
    }

    public Status Fleeing()
    {
        Debug.Log("HUYENDO");
        if(isPathComplete())
        {
            ChangePatrolPoint(-1);
        }
        return Status.Running;
    }
    #endregion

    #region 4. SOBORNAR

    void StartBribing()
    {
        thinkingCloudBehaviour.UpdateCloud(0);
        agent.isStopped = false;
    }

    public Status Bribing()
    {
        Debug.Log("SOBORNANDO");
        agent.SetDestination(police.transform.position);
        return Status.Running;
    }

    #endregion
    void ChangePatrolPoint(int change)
    {
        if(patrolPositions.Count == 0)
        {
            return;
        }
        agent.SetDestination(patrolPositions[currentPatrolIndex].position);
        currentPatrolIndex += change;
        currentPatrolIndex %= patrolPositions.Count;
        if(currentPatrolIndex < 0)
        {
            currentPatrolIndex = patrolPositions.Count - 1;
        }
    }

    bool isPathComplete()
    {
        return (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance &&
            (!agent.hasPath || agent.velocity.sqrMagnitude == 0f));
    }

}
