using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;


public class BeastBehaviour : MonoBehaviour
{
    FSM fsm;
    Vision vision;
    
    
    [Header("Health")] 
    [SerializeField] 
    private bool fullHealth = true;
    private int health = 100;
    public int regen = 20;

    [Header("Timer Perception")] 
    [SerializeField] 
    private float fleeTime;
    private float combatTime;
    private float restTime;
    
    [Header("Hunt")]
    [SerializeField]
    private List<Transform> huntPositions;
    private int currentHuntIndex = 0;

    [Header("Rest")] 
    [SerializeField] 
    private List<Transform> RestPositions;
    
    
    [Header("Thinking bubble")]
    [SerializeField]
    ThinkingCloudBehaviour thinkingCloudBehaviour;

    public NavMeshAgent agent;
    
    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        fsm = new FSM();

        //estado cazar (empieza con este)
        FunctionalAction huntingAction = new FunctionalAction(StartHunting, Hunt, null);
        State hunting = fsm.CreateState(huntingAction);
        
        //Estado de ver presa (y percepcion de ella)
        FunctionalAction watchPreyAction = new FunctionalAction(StartWatchPrey, WatchPrey, null);
        ConditionPerception checkPrey = new ConditionPerception(null, isWatchingPrey, null);
        State watchPrey = fsm.CreateState(watchPreyAction);
        fsm.CreateTransition(hunting, watchPrey, checkPrey, statusFlags: StatusFlags.Running);
        
        //Estado de ver fantasma (y percepcion de él)
        FunctionalAction watchGhostAction = new FunctionalAction(StartWatchGhost, WatchGhost, null);
        ConditionPerception checkGhost = new ConditionPerception(null, isWatchingGhost, null);
        State watchGhost = fsm.CreateState(watchGhostAction);
        fsm.CreateTransition(hunting, watchGhost, checkGhost, statusFlags: StatusFlags.Running);
        
        //Estado de descansar
        FunctionalAction restAction = new FunctionalAction(StartResting, Rest, null);
        TimerPerception restTimer = new TimerPerception(restTime);
        State resting = fsm.CreateState(restAction);
        
        //Estado de huida (transicion desde ver fantasma) FALTA LA CONDICION DE HUIDA QUE SERÍA ABATIR UN POLICIA 
        FunctionalAction fleeAction = new FunctionalAction(StartFleeing, Flee, null);
        TimerPerception fleeTimer = new TimerPerception(fleeTime);
        State fleeing = fsm.CreateState(fleeAction);
        fsm.CreateTransition(watchGhost, fleeing, fleeTimer, statusFlags: StatusFlags.Running);

        //Estado de combate (transicion desde ver presa)
        FunctionalAction combatAction = new FunctionalAction(StartCombating, Combat, null);
        TimerPerception combatTimer = new TimerPerception(combatTime);
        State combating = fsm.CreateState(combatAction);
        fsm.CreateTransition(watchPrey, combating, combatTimer, statusFlags: StatusFlags.Running);
        

        //FALTA TRANSICion entre rest y empezar a cazar de nuevo y transicion entre combate y huir (cuando mata a un policia)
        
        //Transición entre combatir y cazar de nuevo (con explorador)
        ConditionPerception checkDeadPrey = new ConditionPerception(null, CheckDeathPrey, null);
        fsm.CreateTransition(combating, hunting, checkDeadPrey, statusFlags: StatusFlags.Running);
        
        //
        
        
        fsm.SetEntryState(hunting);
        fsm.Start();
    }

    private void Update()
    {
        fsm.Update();
    }
    
    #region 1. HuntState

    void StartHunting()
    {
        agent.isStopped = false;
    }
    public Status Hunt()
    {
        if (IsPathComplete())
        {
            ChangeHuntPoint(1);
        }
        return Status.Running;
    }

    #endregion
    
    #region 2. RestState

    void StartResting()
    {
        thinkingCloudBehaviour.UpdateCloud(4);
        agent.isStopped = true;
    }
    public Status Rest()
    {
        //if(EvaluarMoho()){
            while (fullHealth == false)
            {
                health += regen;
            }
        //}
        return Status.Running;
    }
    #endregion
    
    #region 3. FleeState

    void StartFleeing()
    {
        thinkingCloudBehaviour.UpdateCloud(2);
        agent.isStopped = false;
    } 
    
    public Status Flee()
    {
        if (IsPathComplete())
        {
            agent.SetDestination(ClosestPosition(RestPositions).position);
        }

        return Status.Running;
    }
    #endregion
    
    #region 4. CombatState

    void StartCombating()
    {
        thinkingCloudBehaviour.UpdateCloud(5);
        agent.isStopped = false;
        
    }public Status Combat()
    {
        PoliceBehaviour police = null;
       //ExplorerBehaviour explorer = null;
       //Explorador puesto como police behaviour para que funcione
        PoliceBehaviour explorer = null;
    
        foreach (var trigger in vision.VisibleTriggers)
        {
            if (trigger.CompareTag("Police"))
            {
                police = trigger.GetComponent<PoliceBehaviour>();
            }
            else if (trigger.CompareTag("Explorer"))
            {
                //explorer = trigger.GetComponent<ExplorerBehaviour>();
            }
        }

        if (explorer != null)
        {
            agent.SetDestination(explorer.transform.position);
            if (IsPathComplete())
            {
                //explorer.TakeDamage(beastDamageAmount);
            }
        }
        else if (police != null)
        {
            agent.SetDestination(police.transform.position);
            if (IsPathComplete())
            {
                //police.TakeDamage(beastDamageAmount);
            }
        }
        
        return Status.Running;
    }

    bool CheckDeathExplorer()
    {
        PoliceBehaviour police = null;
        //ExplorerBehaviour explorer = null;
        //Explorador puesto como police behaviour para que funcione
        PoliceBehaviour explorer = null;
        foreach (var trigger in vision.VisibleTriggers)
        {
            if (trigger.CompareTag("Police"))
            {
                police = trigger.GetComponent<PoliceBehaviour>();
            }
            else if (trigger.CompareTag("Explorer"))
            {
                //explorer = trigger.GetComponent<ExplorerBehaviour>();
            }
        }

        if (police.Health == 0)
        {
            return true;
        }

        return false;
    }

    #endregion

    #region 5. WatchPrey

    void StartWatchPrey()
    {
        thinkingCloudBehaviour.UpdateCloud(1);
        agent.isStopped = true;
    }

    public Status WatchPrey()
    {
        return Status.Running;
    }

    bool isWatchingPrey()
    {
        //Para cada trigger en la vision comprobar si tiene un PoliceBehaviour o ExplorerBehaviour
        foreach (var trigger in vision.VisibleTriggers)
        {
            var police = trigger.GetComponent<PoliceBehaviour>();
            //var explorer = trigger.GetComponent<ExplorerBehaviour>();
            //FALTA EL  || explorer != null y descomentar lo de arriba
            if (police != null)
            {
                return true;
            }
        }
        return false;
    }
    #endregion
    
    #region 6. WatchGhost

    void StartWatchGhost()
    {
        thinkingCloudBehaviour.UpdateCloud(1);
        agent.isStopped = true;
    }

    public Status WatchGhost()
    {
        return Status.Running;
    }

    bool isWatchingGhost()
    {
        
        foreach (var trigger in vision.VisibleTriggers)
        {
            //GHOST COMO POLICEBEHAVIOUR PARA QUE FUNCIONE PERO HAY QUE CAMBIARLO A GhostBehaviour
            var ghost = trigger.GetComponent<PoliceBehaviour>();
            if (ghost != null)
            {
                return true;
            }
        }

        return false;
    }
    #endregion
    
    void ChangeHuntPoint(int change)
    {
        if (huntPositions.Count == 0)
        {
            return;
        }

        agent.SetDestination(huntPositions[currentHuntIndex].position);
        currentHuntIndex += change;
        currentHuntIndex %= huntPositions.Count;
        if (currentHuntIndex < 0)
        {
            currentHuntIndex = huntPositions.Count - 1;
        }
    }
    
    Transform ClosestPosition(List<Transform> positions)
    {
        Transform tMin = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = transform.position;
        foreach (Transform t in positions)
        {
            float dist = Vector3.Distance(t.position, currentPos);
            if (dist < minDist)
            {
                tMin = t;
                minDist = dist;
            }
        }
        return tMin;
    }
    
    bool IsPathComplete()
    {
        return (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance &&
            (!agent.hasPath || agent.velocity.sqrMagnitude == 0f));
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + new Vector3(0,1,0), new Vector3(15,1,15));
    }
}
