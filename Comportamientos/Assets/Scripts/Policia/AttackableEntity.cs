using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class AttackableEntity : MonoBehaviour
{
    [Header("Health")]
    [SerializeField]
    protected int maxHealth = 100;
    public int currentHealth = 100;
    public bool isAlive = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public virtual void ReceiveAttack(int damage) 
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            isAlive = false;
            gameObject.GetComponent<VisionTrigger>().enabled=false;
            gameObject.GetComponent<Collider>().enabled=false;
            DOVirtual.DelayedCall(5f, () => { Destroy(gameObject); DOTween.Complete(this); });
        }
    }

}
