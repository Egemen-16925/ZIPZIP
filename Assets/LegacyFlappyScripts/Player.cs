namespace Legacy2D
{
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(1000)]
public class Player : MonoBehaviour
{
    public GraphicRaycaster graphicRaycaster;
    private EventSystem eventSystem;
    public List<GameObject> Avatars;

    public float strength = 5f;
    public float gravity = -9.81f;
    public float tilt = 5f;
    [Tooltip("KediKutusu'nun ziplama sirasinda normal egimin ne kadarini kullanacagini belirler. 0 = donmez, 1 = tam egim.")]
    [Range(0f, 1f)] public float catBoxTiltMultiplier = 0.35f;
    public float pushAmount = 5f;
    public float pushDuration = 5f;

    public Transform birds;
    public Transform CameraParent;
    public Rigidbody2D rb;

    private Vector3 direction;

    public float Health;

    private bool canMove;
    public Transform camera;
    public Image healthBar;

    public float shakeDuration;
    public float shakePower;
    public int shakeVibrato;

    private bool onStartShield = true;
    private bool isDead;
    private bool isRecoveringFromHit;
    private int avatarIndex;
    private float flapPoseUntil;
    private float floorTop = float.NegativeInfinity;

    private Collider2D playerCollider;
    private ContactFilter2D overlapFilter;
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(16);

    [SerializeField] private List<ParticleSystem> _smokeParticles;
    [SerializeField, Min(0.1f)] private float hitboxRadius = 0.38f;
    [SerializeField, Min(0f)] private float ceilingPadding = 0.03f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        playerCollider = GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            playerCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        playerCollider.isTrigger = true;
        CircleCollider2D circle = playerCollider as CircleCollider2D;
        if (circle != null)
        {
            circle.radius = hitboxRadius;
            circle.offset = Vector2.zero;
        }

        overlapFilter = ContactFilter2D.noFilter;
        overlapFilter.useTriggers = true;
        CacheFloorHeight();
    }

    private void Start()
    {
        eventSystem = EventSystem.current;
    }

    public void OnPlay()
    {
        OpenAvatar();
        isDead = false;
        isRecoveringFromHit = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        onStartShield = true;
        CancelInvoke(nameof(CloseShield));
        Invoke(nameof(CloseShield), 0.5f);
        Health = 100f;
        if (healthBar != null)
        {
            healthBar.fillAmount = 1f;
        }

        canMove = true;
    }

    private void CloseShield()
    {
        onStartShield = false;
    }

    private void OnEnable()
    {
        Vector3 position = transform.position;
        position.y = 0f;
        transform.position = position;
        direction = Vector3.zero;
    }

    private void Update()
    {
        bool flappedThisFrame = false;
        if (canMove && Input.GetMouseButtonDown(0) && !IsPointerOverUIElement())
        {
            direction = Vector3.up * strength;
            flapPoseUntil = Time.time + 0.12f;
            flappedThisFrame = true;
            SoundManager.Instance.PlaySound(SoundType.TapSound);
            _smokeParticles.ForEach(p => p.Play());
        }

        direction.y += gravity * Time.deltaTime;
        Vector3 nextPosition = transform.position + direction * Time.deltaTime;
        ClampToCeiling(ref nextPosition);
        transform.position = nextPosition;

        if (!isDead && GetPlayerBottom() <= floorTop)
        {
            Die(false);
            return;
        }

        float targetTilt;
        float minTilt = -62f;
        float maxTilt = -20f;

        if (avatarIndex == 2)
        {
            minTilt = -110f;
            maxTilt = -80f;
        }

        if (flappedThisFrame || direction.y > 0f || Time.time < flapPoseUntil)
        {
            targetTilt = minTilt;
        }
        else
        {
            float fallFactor = Mathf.Clamp01(-direction.y / 15f);
            targetTilt = Mathf.Lerp(minTilt, maxTilt, fallFactor);
        }

        if (Avatars.Count > avatarIndex && Avatars[avatarIndex] != null)
        {
            float avatarTilt = avatarIndex == 3
                ? targetTilt * catBoxTiltMultiplier
                : targetTilt;
            Avatars[avatarIndex].transform.localRotation = Quaternion.Euler(avatarTilt, 90f, 0f);
        }

        Vector3 cameraPosition = camera.position;
        cameraPosition.x = transform.position.x;
        camera.position = cameraPosition;
    }

    private void LateUpdate()
    {
        if (!canMove || isDead || playerCollider == null)
        {
            return;
        }

        // Player ve engeller Transform üzerinden hareket ettiği için fizik dünyasını
        // sorgudan önce eşitleyip hızlı geçişlerde kaçan trigger temaslarını yakala.
        Physics2D.SyncTransforms();
        overlapResults.Clear();
        playerCollider.Overlap(overlapFilter, overlapResults);

        for (int i = 0; i < overlapResults.Count; i++)
        {
            HandleHazard(overlapResults[i]);
            if (isDead)
            {
                break;
            }
        }
    }

    private bool IsPointerOverUIElement()
    {
        if (eventSystem == null || graphicRaycaster == null)
        {
            return false;
        }

        PointerEventData pointerEventData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, results);
        return results.Count > 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Scoring"))
        {
            if (!onStartShield)
            {
                other.gameObject.SetActive(false);
                GameManager.Instance.IncreaseScore();
            }

            return;
        }

        HandleHazard(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleHazard(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHazard(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleHazard(collision.collider);
    }

    private void HandleHazard(Collider2D other)
    {
        if (other == null || other == playerCollider || isDead)
        {
            return;
        }

        if (other.CompareTag("Ceiling"))
        {
            Vector3 position = transform.position;
            ClampToCeiling(ref position);
            transform.position = position;
            return;
        }

        if (onStartShield)
        {
            return;
        }

        if (other.CompareTag("Obstacle"))
        {
            TakeObstacleDamage();
        }
        else if (other.CompareTag("Bottom"))
        {
            Die(false);
        }
    }

    private void TakeObstacleDamage()
    {
        if (isDead || isRecoveringFromHit)
        {
            return;
        }

        isRecoveringFromHit = true;
        onStartShield = true;
        Health = Mathf.Max(0f, Health - 35f);
        if (healthBar != null)
        {
            healthBar.fillAmount = Health / 100f;
        }

        SoundManager.Instance.PlaySound(SoundType.HitSound);
        CameraParent.DOShakePosition(shakeDuration, shakePower, shakeVibrato);
        transform.DOKill();

        if (Health <= 0f)
        {
            Die(false);
            return;
        }

        canMove = false;
        GameManager.Instance.mover.canMove = false;
        GameManager.Instance.mover.moveSpawner = true;
        transform.DOMoveX(transform.position.x - pushAmount, pushDuration).OnComplete(AfterDamage);
    }

    private void AfterDamage()
    {
        if (isDead)
        {
            return;
        }

        canMove = true;
        isRecoveringFromHit = false;
        onStartShield = false;
        GameManager.Instance.mover.canMove = true;
        GameManager.Instance.mover.moveSpawner = false;
    }

    private void Die(bool playHitSound)
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isRecoveringFromHit = false;
        canMove = false;
        Health = 0f;
        if (healthBar != null)
        {
            healthBar.fillAmount = 0f;
        }

        if (playHitSound)
        {
            SoundManager.Instance.PlaySound(SoundType.HitSound);
        }

        CameraParent.DOShakePosition(shakeDuration, shakePower, shakeVibrato);
        transform.DOKill();
        rb.bodyType = RigidbodyType2D.Dynamic;
        onStartShield = true;
        GameManager.Instance.GameOver();
    }

    private void ClampToCeiling(ref Vector3 position)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        float cameraTop = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, 0f)).y;
        float maximumY = cameraTop - GetPlayerHalfHeight() - ceilingPadding;
        if (position.y > maximumY)
        {
            position.y = maximumY;
            if (direction.y > 0f)
            {
                direction.y = 0f;
            }
        }
    }

    private float GetPlayerHalfHeight()
    {
        if (playerCollider == null)
        {
            return hitboxRadius;
        }

        return Mathf.Max(hitboxRadius, playerCollider.bounds.extents.y);
    }

    private float GetPlayerBottom()
    {
        return transform.position.y - GetPlayerHalfHeight();
    }

    private void CacheFloorHeight()
    {
        floorTop = float.NegativeInfinity;
        GameObject[] floorObjects = GameObject.FindGameObjectsWithTag("Bottom");
        for (int i = 0; i < floorObjects.Length; i++)
        {
            Collider2D floorCollider = floorObjects[i].GetComponent<Collider2D>();
            if (floorCollider != null && floorCollider.bounds.center.y < transform.position.y)
            {
                floorTop = Mathf.Max(floorTop, floorCollider.bounds.max.y);
            }
        }
    }

    private void OpenAvatar()
    {
        foreach (GameObject avatar in Avatars)
        {
            if (avatar != null)
            {
                avatar.SetActive(false);
            }
        }

        if (avatarIndex >= 0 && avatarIndex < Avatars.Count && Avatars[avatarIndex] != null)
        {
            Avatars[avatarIndex].SetActive(true);
        }
    }

    public void SetAvatarIndex(int i)
    {
        avatarIndex = i;
    }

    public void PreviewAvatar(int i)
    {
        avatarIndex = Mathf.Clamp(i, 0, Avatars.Count - 1);
        OpenAvatar();
    }
}
}
