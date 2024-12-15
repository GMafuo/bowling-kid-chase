using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NPCController : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;
    private BowlingBall bowlingBall;
    private AudioSource audioSource;
    public AudioClip kickSound;
    
    // Paramètres de comportement
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;
    public float chaseRadius = 10f;
    public float wanderRadius = 10f;
    public float kickDistance = 1.2f;
    public float stoppingDistance = 0.8f;
    public float animationSpeedMultiplier = 0.5f;
    public float velocityThreshold = 0.05f;
    public float detectionRadius = 5f;
    public float chaseChance = 0.5f;

    // Paramètres de mouvement
    public float normalSpeed = 0.5f;
    public float maxChaseSpeed = 1.5f;
    public float accelerationRate = 0.1f;
    public float rotationSpeed = 120f;
    public float angularSpeed = 120f;
    private float currentSpeed;

    // États
    private bool isMoving = false;
    private bool hasKickedBall = false;
    private bool isChasing = false;
    private bool gameWon = false;
    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        InitializeComponents();
        ConfigureNavMeshAgent();
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.1f);
        SetRandomDestination();
        isMoving = true;
    }
    
    private void InitializeComponents()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        bowlingBall = FindAnyObjectByType<BowlingBall>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void ConfigureNavMeshAgent()
    {
        agent.speed = normalSpeed;
        agent.angularSpeed = 360f;
        agent.acceleration = 3f;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
        agent.radius = 0.3f;
        agent.height = 2f;
    }

    void Update()
    {
        if (gameWon) return;
        HandleBallChasing();
        UpdateAnimation();
    }

    private void HandleBallChasing()
    {
        if (bowlingBall == null || hasKickedBall) return;

        float distanceToBall = Vector3.Distance(transform.position, bowlingBall.transform.position);

        if (distanceToBall < detectionRadius && !isChasing && Random.value < chaseChance)
        {
            isChasing = true;
            currentSpeed = normalSpeed;
        }

        if (isChasing && distanceToBall < chaseRadius)
        {
            ChaseBall(distanceToBall);
        }
        else if (!agent.pathPending && agent.remainingDistance < 0.5f && isMoving)
        {
            StopChasing();
        }
    }

    private void ChaseBall(float distanceToBall)
    {
        if (distanceToBall <= kickDistance)
        {
            KickBall();
            return;
        }

        UpdateChaseMovement(distanceToBall);
    }

    private void UpdateChaseMovement(float distanceToBall)
    {
        Vector3 directionToBall = (bowlingBall.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToBall);
        
        float distanceMultiplier = Mathf.Clamp01((distanceToBall - stoppingDistance) / 1.5f);
        float speedMultiplier = Mathf.Lerp(1f, 0.1f, angle / 45f) * distanceMultiplier;
        
        if (angle > 30f) speedMultiplier *= 0.5f;
        
        currentSpeed = Mathf.Min(currentSpeed + accelerationRate * Time.deltaTime, maxChaseSpeed);
        agent.speed = currentSpeed * speedMultiplier;
        agent.SetDestination(bowlingBall.transform.position);
        
        transform.forward = Vector3.Lerp(transform.forward, directionToBall, Time.deltaTime * 10f);
        isMoving = true;
    }

    private void StopChasing()
    {
        isMoving = false;
        isChasing = false;
        StartCoroutine(WaitAndMove());
    }

    private void UpdateAnimation()
    {
        float velocityMagnitude = agent.velocity.magnitude;
        animator.SetFloat("Speed", velocityMagnitude < velocityThreshold ? 0 : 
            Mathf.Clamp01(velocityMagnitude / agent.speed) * animationSpeedMultiplier);
    }

    private void KickBall()
    {
        if (hasKickedBall) return;

        hasKickedBall = true;
        agent.isStopped = true;
        
        if (audioSource != null && kickSound != null)
        {
            audioSource.PlayOneShot(kickSound);
        }
        
        animator.SetTrigger("Kick");
        StartCoroutine(StartDanceAfterKick());
        
        var gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.Lose();
        }
    }

    private IEnumerator StartDanceAfterKick()
    {
        yield return new WaitForSeconds(1f);
        
        float elapsedTime = 0;
        float duration = 0.5f;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0, -169f, 0);
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transform.rotation = Quaternion.Lerp(startRot, targetRot, elapsedTime / duration);
            yield return null;
        }
        
        transform.rotation = targetRot;
        animator.SetTrigger("Dance");
    }

    public void RestartBehavior()
    {
        gameWon = false;
        hasKickedBall = false;
        isChasing = false;
        isMoving = false;
        agent.isStopped = false;

        agent.enabled = false;
        transform.position = startPosition;
        transform.rotation = startRotation;
        agent.enabled = true;

        animator.Rebind();
        animator.Update(0f);
        animator.SetFloat("Speed", 0);
        animator.ResetTrigger("Kick");
        animator.ResetTrigger("Dance");

        StartCoroutine(DelayedRestart());
    }

    private IEnumerator DelayedRestart()
    {
        yield return new WaitForSeconds(0.1f);
        SetRandomDestination();
    }

    private void SetRandomDestination()
    {
        if (!agent.isOnNavMesh)
        {
            return;
        }

        int maxAttempts = 30;
        int attempts = 0;
        
        while (attempts < maxAttempts)
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;
            NavMeshHit hit;
            
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(hit.position);
                    isMoving = true;
                    return;
                }
            }
            attempts++;
        }
    }

    public void OnGameWon()
    {
        gameWon = true;
        agent.isStopped = true;
        isChasing = false;
        
        if (animator != null)
        {
            animator.SetFloat("Speed", 0);
            animator.SetTrigger("Sad");
        }
    }

    private IEnumerator WaitAndMove()
    {
        yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
        SetRandomDestination();
    }
}
