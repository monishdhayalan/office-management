using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using DG.Tweening;
using Random = UnityEngine.Random;

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
    public NavMeshAgent agent;
    public EmployeeType employeeType;
    public Animator animator;
    public SkinnedMeshRenderer Mesh;
    
    private Table currentTable;
    private bool isWorking = false;

    // Simulation params
    private float workInterval = 2.0f;
    private int moneyPerTick = 10;
    private Color RandomColor;

    void Awake()
    {
        RandomColor = Random.ColorHSV(0, 1, 0, 1, 0.5f, 1);
        
        Mesh.material.color = RandomColor;
        
        // Define stats based on type
        switch(employeeType)
        {
            case EmployeeType.Intern: moneyPerTick = 5; workInterval = 4f; break;
            case EmployeeType.Junior: moneyPerTick = 15; workInterval = 3f; break;
            case EmployeeType.Mid: moneyPerTick = 30; workInterval = 2f; break;
            case EmployeeType.Senior: moneyPerTick = 60; workInterval = 1f; break;
        }

    }

    private void Start()
    {
        StartEmployee();
    }

    public void StartEmployee()
    {
        // Start looking for a table
        StartCoroutine(FindAndGoToTable());
        
    }

    IEnumerator FindAndGoToTable()
    {
        // Wait for agent to be on navmesh
        while (!agent.isOnNavMesh)
        {
            yield return null;
        }

        while (currentTable == null)
        {
            // Simple logic: Find first free table
            // Finding free table
            if (Table.FreeTables.Count > 0)
            {
                // Find closest table
                Table closestTable = null;
                float closestDist = float.MaxValue;

                foreach(var t in Table.FreeTables)
                {
                    if (t == null) continue;
                    float d = Vector3.Distance(transform.position, t.transform.position);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        closestTable = t;
                    }
                }

                if (closestTable != null && closestTable.AssignEmployee(this))
                {
                    animator.SetBool("IsWalking", true);
                    currentTable = closestTable;
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
        while (!agent.isOnNavMesh || agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
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
            
            // Play earning effect
            if (GameManager.Instance.MoneyEarningEffectPrefab != null)
            {
                // Spawn above head
                GameObject fx = Instantiate(GameManager.Instance.MoneyEarningEffectPrefab, transform.position + Vector3.up * 3, Quaternion.identity);
                Destroy(fx, 2f);
                
                GameObject textfx = Instantiate(GameManager.Instance.MoneyEarningTextPopup, transform.position + Vector3.up * 3, Quaternion.identity);
                textfx.transform.forward = Camera.main.transform.forward;
                textfx.transform.DOLocalMoveY(textfx.transform.position.y + 1.5f, 0.35f).SetEase(Ease.OutBack);
                Destroy(textfx, 0.8f);

            }

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
