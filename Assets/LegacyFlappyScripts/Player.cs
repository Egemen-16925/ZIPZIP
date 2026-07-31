namespace Legacy2D {
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public GraphicRaycaster graphicRaycaster;
    private EventSystem eventSystem;
    public List<GameObject> Avatars;

    public float strength = 5f;
    public float gravity = -9.81f;
    public float tilt = 5f;
    public float pushAmount = 5f;
    public float pushDuration = 5f;

    public Transform birds;
    public Transform CameraParent;
    public Rigidbody2D rb;

    private SpriteRenderer spriteRenderer;
    private Vector3 direction;
    private int spriteIndex;

    public float Health;

    private bool canMove = false;
    public Transform camera;
    public Image healthBar;

    public float shakeDuration;
    public float shakePower;
    public int shakeVibrato;

    private bool onStartShield = true;
    private int avatarIndex;

    [SerializeField] private List<ParticleSystem> _smokeParticles;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true;

        // Çarpışma (Hitbox) ekleme ve büyütme
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            // Orijinal 2D sprite'ın collider'ı silinmişse, Player'a yenisini ekliyoruz!
            col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true; // Engellere çarpabilmesi için Trigger olmalı
        }
        
        if (col is CircleCollider2D circle) circle.radius = 0.4f; // Önceden 1.5'ti, çarpmadan ölüyordu
        else if (col is BoxCollider2D box) box.size = new Vector2(0.6f, 0.6f);
    }

    private void Start()
    {
        eventSystem = EventSystem.current;
    }

    public void OnPlay()
    {
        OpenAvatar();
        onStartShield = true;
        CancelInvoke(nameof(CloseShield));
        Invoke(nameof(CloseShield),0.5f);
        Health = 100;
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
        if (canMove && Input.GetMouseButtonDown(0))
        {
            if(!IsPointerOverUIElement())
            {
                direction = Vector3.up * strength;
                SoundManager.Instance.PlaySound(SoundType.TapSound);
                _smokeParticles.ForEach(p => p.Play());
            }

        }

        // Apply gravity and update the position
        direction.y += gravity * Time.deltaTime;
        transform.position += direction * Time.deltaTime;

        // Tıklanınca anında -62, düşerken yumuşakça -20 olacak şekilde ayarlandı
        float targetTilt;
        float minTilt = -62f;
        float maxTilt = -20f;

        // Eğer Ördek seçiliyse (avatarIndex 2) açıları -110 ile -80 yap
        if (avatarIndex == 2)
        {
            minTilt = -110f;
            maxTilt = -80f;
        }

        if (direction.y > 0) 
        {
            targetTilt = minTilt; // Zıplarken anında burnu yukarı diker
        }
        else 
        {
            // Düşerken (direction.y negatif) hızına göre burnunu aşağı doğru yavaşça indirir
            float fallFactor = Mathf.Clamp01(-direction.y / 15f); 
            targetTilt = Mathf.Lerp(minTilt, maxTilt, fallFactor);
        }
        
        if (Avatars.Count > avatarIndex && Avatars[avatarIndex] != null)
        {
            // Sahnede ayarlanan 90 derecelik Y açısını koruyarak X eksenine tilt veriyoruz
            Avatars[avatarIndex].transform.localRotation = Quaternion.Euler(targetTilt, 90f, 0f);
        }

        Vector3 pos = camera.position;
        pos.x = transform.position.x;
        camera.position = pos;
    }

    private bool IsPointerOverUIElement()
    {
        PointerEventData pointerEventData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, results);
        return results.Count > 0; // Eğer bir UI elemanına tıklandıysa true döner.
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (onStartShield)
            return;

        if (other.gameObject.CompareTag("Obstacle"))
        {
            SoundManager.Instance.PlaySound(SoundType.HitSound);
            OnCollideWithObstacle();
        }
        else if (other.gameObject.CompareTag("Scoring"))
        {
            other.gameObject.SetActive(false);
            GameManager.Instance.IncreaseScore();
        }
        else if (other.gameObject.CompareTag("Bottom"))
        {
            CameraParent.DOShakePosition(shakeDuration, shakePower, shakeVibrato);
            transform.DOKill();
            rb.isKinematic = false;
            onStartShield = true;
            GameManager.Instance.GameOver();
        }
    }

    private void OnCollideWithObstacle()
    {
        print(1231);

        Health -= 35;
        healthBar.fillAmount = Health / 100f;
        CameraParent.DOShakePosition(shakeDuration, shakePower, shakeVibrato);

        transform.DOKill();
        if (Health < 0)
        {
            rb.isKinematic = false;
            onStartShield = true;
            GameManager.Instance.GameOver();
        }
        else
        {
            canMove = false;
            GameManager.Instance.mover.canMove = false;
            GameManager.Instance.mover.moveSpawner = true;
            transform.DOMoveX(transform.position.x - pushAmount, pushDuration).OnComplete(() => AfterDamage());
        }
    }

    private void AfterDamage()
    {
        canMove = true;
        GameManager.Instance.mover.canMove = true;
        GameManager.Instance.mover.moveSpawner = false;
    }

    private void OpenAvatar()
    {
        // Önce tüm karakterleri kapatıyoruz ki sahnede üst üste iki karakter görünmesin!
        foreach(var avatar in Avatars)
        {
            if(avatar != null) avatar.SetActive(false);
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
}

}
