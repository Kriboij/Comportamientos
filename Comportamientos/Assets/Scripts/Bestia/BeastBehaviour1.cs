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


public class BeastBehaviour1 : MonoBehaviour
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
    private Transform prey;

    [Header("Rest")] 
    [SerializeField] 
    private List<Transform> RestPositions;
    
    
    [Header("Thinking bubble")]
    [SerializeField]
    ThinkingCloudBehaviour thinkingCloudBehaviour;
    
    //behaviours
    private PoliceBehaviour police;
    private ExplorerBehaviour explorer;
    private GhostBehaviour ghost;

    public NavMeshAgent agent;
    
    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        fsm = new FSM();
    }

    private void Update()
    {
        //cuando no haya lo d abajo
        Debug.Log("Path pending: " + agent.pathPending);
        
        
        Debug.Log("Remaining distance: " + agent.remainingDistance);
        Debug.Log("Stopping distance: " + agent.stoppingDistance);
        
        //y 
        Debug.Log("Velocidad del agente: " + agent.velocity.sqrMagnitude);
        Debug.Log("HasPath: " + agent.hasPath);
        fsm.Update();
    }
    
    #region 1. HuntState

    void StartHunting()
    {
        
    }
    public void Hunt()
    {
        agent.isStopped = false;
        agent.SetDestination(huntPositions[0].position);
        if (IsPathComplete())
        {
            Debug.Log("Caminou completao, siguiendo ruta");
            ChangeHuntPoint(1);
        }
    }

    #endregion
    
    #region 2. RestState

    void StartResting()
    {
        thinkingCloudBehaviour.UpdateCloud(4);
        agent.isStopped = true;
    }
    public void Rest()
    {
        Debug.Log("ZZZZ");
        if(EvaluarMoho()){
            while (fullHealth == false)
            {
                health += regen;
            }
        }
    }
    #endregion
    
    #region 3. FleeState

    void StartFleeing()
    {
        thinkingCloudBehaviour.UpdateCloud(2);
        agent.isStopped = false;
    } 
    
    public void Flee()
    {
        Debug.Log("HUYENDO");
        if (IsPathComplete())
        {
            agent.SetDestination(ClosestPosition(RestPositions).position);
        }

    }
    #endregion
    
    #region 4. CombatState

    void StartCombating()
    {
        thinkingCloudBehaviour.UpdateCloud(5);
        agent.isStopped = false;
        
    }
    public void Combat()
    {
        Debug.Log("COMBATIENDO");
        PoliceBehaviour police = null;
        ExplorerBehaviour explorer = null;
    
        foreach (var trigger in vision.VisibleTriggers)
        {
            if (trigger.CompareTag("Police"))
            {
                police = trigger.GetComponent<PoliceBehaviour>();
            }
            else if (trigger.CompareTag("Explorer"))
            {
                explorer = trigger.GetComponent<ExplorerBehaviour>();
            }
        }

        if (explorer != null)
        {
            agent.SetDestination(explorer.transform.position);
            if (IsPathComplete())
            {
                //Hace daño
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
        
    }

    
    public bool CheckDeadPolice()
    {
       return police.currentHealth == 0;
    }

    public bool CheckDeadExplorer()
    {
        return explorer.currentHealth == 0;
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

    public bool IsWatchingPrey()
    {
        prey = null;
        explorer = null;
        police = null;
        
        Debug.Log(vision.VisibleTriggers);
        foreach (var trigger in vision.VisibleTriggers)
        {
            if (trigger != null)
            {
                if (prey == trigger.GetComponent<PoliceBehaviour>())
                {
                    prey = trigger;
                    police = trigger.GetComponent<PoliceBehaviour>();
                }
                else if (prey == trigger.GetComponent<ExplorerBehaviour>())
                {
                    prey = trigger;
                    explorer = trigger.GetComponent<ExplorerBehaviour>();
                }
                
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

    public bool IsWatchingGhost()
    {
        ghost = null;
        foreach (var trigger in vision.VisibleTriggers)
        {
            ghost = trigger.GetComponent<GhostBehaviour>();
            if (ghost != null)
            {
                return true;
            }
        }

        return false;
    }
    #endregion

    private bool EvaluarMoho()
    {
        Debug.Log("Evaluando moho");
        bool moho = false;

        foreach (var trigger in vision.VisibleTriggers)
        {
            if (trigger.CompareTag("moho"))
            {
                moho = true;
            }
        }

        return moho;
    }


    public void checkPrey()
    {
        
        Debug.Log("Checkeando presa ;)");
        foreach (var trigger in vision.VisibleTriggers)
        {
            if (trigger.GetComponent<PoliceBehaviour>() != null || trigger.GetComponent<ExplorerBehaviour>() != null)
            {
                prey = trigger;
            }
        }
    }
    
    void ChangeHuntPoint(int change)
    {
        
        Debug.Log("Contando zonas d cCAZA");
        if (huntPositions.Count == 0)
        {
            return;
        }
        Debug.Log("Cambiando");
        agent.SetDestination(huntPositions[currentHuntIndex].position);
        currentHuntIndex += change;
        currentHuntIndex %= huntPositions.Count;
        if (currentHuntIndex < 0)
        {
            currentHuntIndex = huntPositions.Count - 1;
        }
    }


    public bool CheckHealth()
    {
        return health == 100;
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
    
    public bool IsPathComplete()
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
