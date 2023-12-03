using System;
using BehaviourAPI.UnityToolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.StateMachines;

enum beastStates 
{
    Hunt,
    Chase,
    Flee,
    Rest,
    Combat
}

public class beastBehaviour : MonoBehaviour
{
    FSM fsm;
    
    [Header("Health")] [SerializeField] 
    private Boolean fullHealth = true;
    private int health = 100;
    public int regen = 20;

    [Header("Hunt")]
    [SerializeField]
    private List<Transform> huntPositions;
    private int currentHuntIndex = 0;
    private Coroutine huntCoroutine = null;


    [Header("Thinking bubble")]
    [SerializeField]
    ThinkingCloudBehaviour thinkingCloudBehaviour;

    public NavMeshAgent agent;

    [SerializeField]
    private beastStates state = beastStates.Hunt;

    
    
    
    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        fsm = new FSM();

        //estado cazar
        FunctionalAction huntingAction = new FunctionalAction(StartHunting, Hunt(), null);
        State hunting = fsm.CreateState(huntingAction);
        
        ConditionalPerception checkPolice = new ConditionalPerception()
        //primer lugar estado inicial, segundo lugar otro estado y tercer lugar condicion yu al final el status flag (por ahora en todas es running)
        
        
    }



    public void Hunt()
    {
        // Establece el estado actual como "Hunt" (caza).
        state = beastStates.Hunt;
    
        // Actualiza la nube de pensamiento con un valor específico (0 en este caso).
        thinkingCloudBehaviour.UpdateCloud(0);

        // Si ya hay una corrutina de caza en ejecución, la detiene.
        if (huntCoroutine != null)
        {
            StopCoroutine(huntCoroutine);
            huntCoroutine = null;
        }

        // Inicia una nueva corrutina de caza y guarda una referencia a ella en huntCoroutine.
        huntCoroutine = StartCoroutine(PatrolCorutine());

        // La corrutina de patrullaje.
        IEnumerator PatrolCorutine()
        {
            // Este bucle se ejecuta continuamente.
            while (true)
            {
                // Establece la posición de destino del agente (personaje o entidad) hacia la posición actual de caza.
                agent.SetDestination(huntPositions[currentHuntIndex].position);

                // Espera hasta que el agente complete su ruta hacia la posición de caza.
                yield return new WaitUntil(IsPathComplete);

                // Incrementa el índice de caza actual y asegura que esté dentro de los límites del tamaño de la lista.
                currentHuntIndex++;
                currentHuntIndex %= huntPositions.Count;
            }
        }
    }

    bool IsPathComplete()
    {
        return (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance &&
            (!agent.hasPath || agent.velocity.sqrMagnitude == 0f));
    }


    public void Chase()
    {
        state = beastStates.Chase;
        //actualizar el thinking cloud a persiguiendo
        //thinkingCloudBehaviour.UpdateCloud();
    }

    //metodo en el que se queda quieto y regenera vida
    public void Rest()
    {
        state = beastStates.Rest;
        while (fullHealth == false)
        {
            health += regen;
        }
    }

    public void Combat()
    {
        state = beastStates.Combat;
        
    }

    //metodo en el que huye de un personaje 
    public void Flee()
    {
        state = beastStates.Flee;
        // El personaje se va a una habitación
        //agent.SetDestination(Habitacion.position);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + new Vector3(0,1,0), new Vector3(15,1,15));
    }
}
