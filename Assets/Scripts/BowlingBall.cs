using UnityEngine;
using System.Collections;

public class BowlingBall : MonoBehaviour
{
    private Rigidbody rb;
    public float force = 1000f;
    public float moveSpeed = 5f;
    public float jumpForce = 300f;
    public float fallMultiplier = 2.5f;
    private bool isGrounded = true;
    private Vector3 startPosition;
    private Vector3 smoothedInput;
    public float smoothingFactor = 0.1f;
    private bool isGameOver = false;

    private AudioSource audioSource;
    public AudioClip rollingSound;
    [Range(0f, 1f)]
    public float rollingVolume = 0.1f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = new Vector3(9.150002f, 23.44f, -27.51f);
        
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = rollingSound;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = rollingVolume;
        
        ResetBall();
    }

    void Update()
    {
        if (isGameOver)
        {
            audioSource.Stop();
            return;
        }

        HandleRollingSound();
        HandleMovement();
        HandleJump();

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetBall();
            var gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.RestartGame();
            }
        }
    }

    private void HandleRollingSound()
    {
        if (rb.linearVelocity.magnitude > 0.1f && isGrounded)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
            audioSource.volume = (Mathf.Clamp01(rb.linearVelocity.magnitude / moveSpeed) * rollingVolume);
        }
        else
        {
            audioSource.Stop();
        }
    }

    private void HandleMovement()
    {
        Vector3 rawInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        smoothedInput = Vector3.Lerp(smoothedInput, rawInput, Time.deltaTime / smoothingFactor);
        rb.linearVelocity = new Vector3(smoothedInput.x * moveSpeed, rb.linearVelocity.y, smoothedInput.z * moveSpeed);
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
    }

    public void ResetBall()
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        isGrounded = true;
        smoothedInput = Vector3.zero;

        StartCoroutine(ReactivatePhysics());
    }

    private IEnumerator ReactivatePhysics()
    {
        yield return new WaitForSeconds(0.1f);
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    public void SetGameOver(bool gameOver)
    {
        isGameOver = gameOver;
    }

    public void RestartGame()
    {
        isGameOver = false;
        ResetBall();
    }
}
