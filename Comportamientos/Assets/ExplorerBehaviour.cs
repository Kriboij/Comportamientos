using System;
using BehaviourAPI.UnityToolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using BehaviourAPI.Core;
using Random = UnityEngine.Random;

enum ExplorerStates 
{
        Exploring,
        Painting,
        LookingAt,
        Escaping,
        InteractingDoor,
        Hiding,
        Fainted,
}

public class ExplorerBehaviour : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int health = 50;

    [Header("Exploring")]
    [SerializeField] private List<Transform> explorePositions;
    private int currentPositionIndex = 0;
    private Coroutine exploringCorutine = null;

    [Header("Painting")] 
    [SerializeField] private float paintingTime;
    private Transform paintingPosition;
    private Coroutine paintCorutine = null;
    
    [Header("Faint")] 
    [SerializeField] private float faintTime;
    private Coroutine faintCorutine;

    [Header("General Variables")]
    [SerializeField] private ExplorerStates state = ExplorerStates.Exploring;
    
    [Header("Thinking bubble")]
    [SerializeField] ThinkingCloudBehaviour thinkingCloudBehaviour;
    
    private Coroutine exploreCorutine;
    private Transform doorPosition;
    public NavMeshAgent agent;

    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }


    public void Explore()
    {
        state = ExplorerStates.Exploring;
        thinkingCloudBehaviour.UpdateCloud(0);

        if (exploreCorutine != null)
        {
            StopCoroutine(exploreCorutine);
            exploreCorutine = null;
        }
        exploreCorutine = StartCoroutine(ExploreCorutine());
    }
    
    public void Paint(Transform paint)
    {
        paintingPosition = paint;
        state = ExplorerStates.Painting;
        thinkingCloudBehaviour.UpdateCloud(1);

        if (paintCorutine != null)
        {
            StopCoroutine(paintCorutine);
            exploreCorutine = null;
        }
        exploreCorutine = StartCoroutine(PaintCorutine());
    }
    
    public void Look(Transform objective)
    {
        state = ExplorerStates.LookingAt;
        transform.LookAt(objective);
    }

    public void Escape()
    {
        state = ExplorerStates.Escaping;
    }
    
    public void Hide()
    {
        state = ExplorerStates.Hiding;
    }
    
    public void Faint()
    {
        state = ExplorerStates.Fainted;
        thinkingCloudBehaviour.UpdateCloud(1);

        if (faintCorutine != null)
        {
            StopCoroutine(faintCorutine);
            exploreCorutine = null;
        }
        faintCorutine = StartCoroutine(FaintCorutine());
    }

    public void InteractingDoor(GameObject door)
    {
        doorPosition = door.transform;
        state = ExplorerStates.InteractingDoor;
        thinkingCloudBehaviour.UpdateCloud(1);

        if (paintCorutine != null)
        {
            StopCoroutine(paintCorutine);
            exploreCorutine = null;
        }
        agent.SetDestination(doorPosition.position);
        //doorPosition.gameObject.GetComponent<DoorContoller>().Open();
    }
    
    private IEnumerator PaintCorutine()
    {
        agent.SetDestination(paintingPosition.position);
        yield return new WaitForSeconds(paintingTime);
    }
    
    private IEnumerator ExploreCorutine()
    {
        agent.SetDestination(explorePositions[(int) Random.Range(0f, explorePositions.Count)].position);
        yield return new WaitUntil(() => { return IsPathComplete(); });
    }

    private IEnumerator FaintCorutine()
    {
        agent.enabled = false;
        yield return new WaitForSeconds(faintTime);
        agent.enabled = true;
    }
    
    bool IsPathComplete()
    {
        return (!agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance &&
                (!agent.hasPath || agent.velocity.sqrMagnitude == 0f));
    }
    

    private void OnTriggerEnter(Collider other)
    {
       
    }

    private void OnTriggerExit(Collider other)
    {
       
    }



    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + new Vector3(0,1,0), new Vector3(15,1,15));
    }
}

