using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvestigableObject : MonoBehaviour
{
    public bool recentlyInvestigated = false;
    public float investigationCooldownSeconds = 10f;

    [SerializeField]
    public int investigateTime = 2;

    public Transform investigatePosition;

    // Start is called before the first frame update
    void Start()
    {
        if(investigatePosition==null) investigatePosition = transform;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool ShouldInvestigate() 
    {
        return !recentlyInvestigated;
    }

    public void HasBeenInvestigated() 
    {
        recentlyInvestigated = true;

        StartCoroutine(InvestigationCooldown());

        IEnumerator InvestigationCooldown() 
        {
            yield return new WaitForSeconds(investigationCooldownSeconds);
            recentlyInvestigated = false;
        }
    }
}
