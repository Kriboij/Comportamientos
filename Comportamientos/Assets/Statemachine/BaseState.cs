using UnityEngine;

public abstract class BaseState: IState
{
    protected readonly GameObject personaje;
    
    protected BaseState(GameObject personaje)
    {
        this.personaje = personaje;
    }
    
    public virtual void OnEnter()
    {
        //noop
    }

    public virtual void Update()
    {
        //noop
    }

    public virtual void FixedUpdate()
    {
        //noop
    }

    public virtual void OnExit()
    {
        //noop
    }
}