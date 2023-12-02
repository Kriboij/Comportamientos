using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvestigableObject : MonoBehaviour
{
    public bool recentlyInvestigated = false;
    public float investigationCooldownSeconds = 10f;

    public float curiosity = 1;
    public float investigateThreshold = 50;

    [SerializeField]
    public int investigateTime = 2;

    public Transform investigatePosition;

    private Coroutine cooldown =null;

    // Start is called before the first frame update
    void Start()
    {
        if(investigatePosition==null) investigatePosition = transform;
    }


    public bool ShouldInvestigate(int paranoia) 
    {
        float investigateLevel = curiosity * paranoia;
        if (!recentlyInvestigated || investigateLevel > investigateThreshold) 
        {
            return true;
        }
        return false;
    }

    public void HasBeenInvestigated() 
    {
        recentlyInvestigated = true;

        if (cooldown != null) 
        {
            StopCoroutine(cooldown);
            cooldown = null;
        }

        cooldown = StartCoroutine(InvestigationCooldown());

        IEnumerator InvestigationCooldown() 
        {
            yield return new WaitForSeconds(investigationCooldownSeconds);
            recentlyInvestigated = false;
        }
    }
}
