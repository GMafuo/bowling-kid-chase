using UnityEngine;
using System.Collections;

public class Pin : MonoBehaviour
{
    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private AudioSource audioSource;
    public AudioClip hitSound;
    private bool isHit = false;
    private Vector3 startPosition;
    private Quaternion startRotation;
    public float blinkSpeed = 0.1f;
    public float destroyDelay = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<BowlingBall>() != null && !isHit)
        {
            isHit = true;
            
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound, 0.3f);
            }
            
            var gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.PinHit();
            }
            StartCoroutine(BlinkAndDestroy());
        }
    }

    private IEnumerator BlinkAndDestroy()
    {
        float elapsedTime = 0f;
        float currentBlinkSpeed = blinkSpeed;
        
        while (elapsedTime < destroyDelay)
        {
            currentBlinkSpeed = blinkSpeed * (1f - (elapsedTime / destroyDelay) * 0.8f);
            meshRenderer.enabled = !meshRenderer.enabled;
            yield return new WaitForSeconds(currentBlinkSpeed);
            elapsedTime += currentBlinkSpeed;
        }

        meshRenderer.enabled = true;
        gameObject.SetActive(false);
    }

    public void ResetPin()
    {
        isHit = false;
        meshRenderer.enabled = true;
        gameObject.SetActive(true);
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = startPosition;
            transform.rotation = startRotation;
            StartCoroutine(ReactivatePhysics());
        }
    }

    private IEnumerator ReactivatePhysics()
    {
        yield return new WaitForSeconds(0.1f);
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }
} 