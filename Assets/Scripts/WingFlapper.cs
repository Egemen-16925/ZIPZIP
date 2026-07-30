using UnityEngine;

public class WingFlapper : MonoBehaviour
{
    public enum FlapAxis { Z_Axis, X_Axis, Y_Axis }

    [Header("Kanat Objeleri")]
    [Tooltip("1. Kanat Transformu (Örn: Sol / Ön Kanat)")]
    public Transform wing1;
    
    [Tooltip("2. Kanat Transformu (Örn: Sağ / Arka Kanat)")]
    public Transform wing2;

    [Header("Dönme Ekseni (Flap Axis)")]
    [Tooltip("Kanatların hangi yerel eksende çırpılacağı")]
    public FlapAxis flapAxis = FlapAxis.Z_Axis;

    [Header("1. Kanat Açı Sınırları (Derece)")]
    [Tooltip("1. Kanat minimum açısı")]
    public float wing1MinAngle = -60f;
    [Tooltip("1. Kanat maksimum açısı")]
    public float wing1MaxAngle = 60f;

    [Header("2. Kanat Açı Sınırları (Derece)")]
    [Tooltip("2. Kanat minimum açısı")]
    public float wing2MinAngle = -60f;
    [Tooltip("2. Kanat maksimum açısı")]
    public float wing2MaxAngle = 60f;

    // Geriye dönük uyumluluk (Backward Compatibility)
    public float wing1MinZ { get => wing1MinAngle; set => wing1MinAngle = value; }
    public float wing1MaxZ { get => wing1MaxAngle; set => wing1MaxAngle = value; }
    public float wing2MinZ { get => wing2MinAngle; set => wing2MinAngle = value; }
    public float wing2MaxZ { get => wing2MaxAngle; set => wing2MaxAngle = value; }

    [Header("Animasyon Ayarları")]
    [Tooltip("Kanat çırpma hızı")]
    [Range(0.1f, 30f)]
    public float flapSpeed = 5f;

    [Tooltip("2. Kanadı zıt/simetrik yönde çırp")]
    public bool invertWing2 = true;

    [Tooltip("Yumuşak sinüs eğrisi kullanımı")]
    public bool useSmoothSine = true;

    [Header("Yerel Duruş Rotasyonları")]
    [SerializeField] private Quaternion wing1InitialRot;
    [SerializeField] private Quaternion wing2InitialRot;
    private bool initialRotationsSaved = false;

    private void Awake()
    {
        AutoAssignWings();
        SaveInitialRotations();
    }

    private void Start()
    {
        if (!initialRotationsSaved)
        {
            SaveInitialRotations();
        }
    }

    [ContextMenu("Başlangıç Rotasyonlarını Yeniden Kaydet")]
    public void SaveInitialRotations()
    {
        if (wing1 != null) wing1InitialRot = wing1.localRotation;
        if (wing2 != null) wing2InitialRot = wing2.localRotation;
        initialRotationsSaved = true;
    }

    private void AutoAssignWings()
    {
        if (wing1 == null && transform.childCount > 0) wing1 = transform.GetChild(0);
        if (wing2 == null && transform.childCount > 1) wing2 = transform.GetChild(1);
    }

    private Vector3 GetAxisVector()
    {
        switch (flapAxis)
        {
            case FlapAxis.X_Axis: return Vector3.right;
            case FlapAxis.Y_Axis: return Vector3.up;
            case FlapAxis.Z_Axis:
            default: return Vector3.forward;
        }
    }

    private void Update()
    {
        float t = useSmoothSine ? (Mathf.Sin(Time.time * flapSpeed) + 1f) * 0.5f : Mathf.PingPong(Time.time * flapSpeed, 1f);
        Vector3 axis = GetAxisVector();

        // 1. Kanat (Yerel eksende rotasyon)
        if (wing1 != null)
        {
            float angle1 = Mathf.Lerp(wing1MinAngle, wing1MaxAngle, t);
            wing1.localRotation = wing1InitialRot * Quaternion.AngleAxis(angle1, axis);
        }

        // 2. Kanat (Yerel eksende rotasyon)
        if (wing2 != null)
        {
            float t2 = invertWing2 ? (1f - t) : t;
            float angle2 = Mathf.Lerp(wing2MinAngle, wing2MaxAngle, t2);
            wing2.localRotation = wing2InitialRot * Quaternion.AngleAxis(angle2, axis);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 axis = GetAxisVector();
        if (wing1 != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(wing1.position, wing1.TransformDirection(axis) * 0.5f);
        }
        if (wing2 != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(wing2.position, wing2.TransformDirection(axis) * 0.5f);
        }
    }
}
