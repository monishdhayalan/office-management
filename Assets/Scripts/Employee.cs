using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum EmployeeType
{
    Intern,
    Junior,
    Mid,
    Senior
}

public enum WorkAnimations
{
    Dance1,
    Dance2,
    MAX
}

public class Employee : MonoBehaviour
{
    private NavMeshAgent agent;
    public EmployeeType employeeType;
    public Animator animator;
    
    private Table currentTable;
    private bool isWorking = false;

    // Simulation params
    private float workInterval = 2.0f;
    private int moneyPerTick = 10;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // Define stats based on type
        switch(employeeType)
        {
            case EmployeeType.Intern: moneyPerTick = 5; workInterval = 4f; break;
            case EmployeeType.Junior: moneyPerTick = 15; workInterval = 3f; break;
            case EmployeeType.Mid: moneyPerTick = 30; workInterval = 2f; break;
            case EmployeeType.Senior: moneyPerTick = 60; workInterval = 1f; break;
        }

        // Start looking for a table
        StartCoroutine(FindAndGoToTable());
    }

    IEnumerator FindAndGoToTable()
    {
        while (currentTable == null)
        {
            // Simple logic: Find first free table
            if (Table.FreeTables.Count > 0)
            {
                // Find closest? Or just first.
                // For simplicity, pick first.
                Table candidate = Table.FreeTables[0];
                
                if (candidate.AssignEmployee(this))
                {
                    //StartCoroutine(SetAnimatorBoolCoroutine("IsWalking", true));
                    animator.SetBool("IsWalking", true);
                    currentTable = candidate;
                    agent.SetDestination(currentTable.GetInteractionPosition());
                }
            }

            if (currentTable == null)
            {
                // Wait and retry
                yield return new WaitForSeconds(1f);
            }
        }

        // Wait until we reach destination
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }

        transform.forward = currentTable.GetInteractionPosition() - transform.position;
        animator.SetBool("IsWalking", false);
        // Start working
        animator.SetInteger("Work", Random.Range(0, (int)WorkAnimations.MAX));
        isWorking = true;
        StartCoroutine(WorkRoutine());
    }

    IEnumerator WorkRoutine()
    {
        while (isWorking)
        {
            yield return new WaitForSeconds(workInterval);
            GameManager.Instance.AddMoney(moneyPerTick);
            // Optional: Play work animation or effect
        }
    }

    public void UnassignTable()
    {
        isWorking = false;
        currentTable = null;
        StopAllCoroutines();
        // Go back to finding a table
        StartCoroutine(FindAndGoToTable());
    }
    
    
    public IEnumerator SetAnimatorBoolCoroutine(string name, bool value)
    {
        yield return null; // For some unknown reason this is crucial to making this work in my case. YMMV

        animator.SetBool(name, value);

        yield return null;
    }
}
