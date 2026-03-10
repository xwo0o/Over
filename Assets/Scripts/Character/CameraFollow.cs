using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("目标设置")]
    public Transform target;

    [Header("位置跟随设置")]
    public float distance = 10f;
    public float height = 10f;
    [Range(1f, 20f)]
    public float positionSmoothSpeed = 5f;
    [Range(5f, 50f)]
    public float maxPositionSpeed = 20f;
    
    [Tooltip("摄像机相对于角色的水平角度（0表示在角色正后方）")]
    [Range(-180f, 180f)]
    public float cameraHorizontalAngle = 0f;

    [Header("偏移角度设置")]
    [Range(-180f, 180f)]
    public float xOffsetAngle = 0f;
    [Range(-90f, 90f)]
    public float yOffsetAngle = 0f;
    [Range(-180f, 180f)]
    public float zOffsetAngle = 0f;

    [Header("旋转设置")]
    [Range(1f, 30f)]
    public float rotationSmoothSpeed = 10f;
    [Range(30f, 360f)]
    public float maxRotationSpeed = 180f;

    [Header("遮挡检测设置")]
    public LayerMask obstacleLayer = 1;
    public float minDistance = 2f;
    public float occlusionCheckRadius = 0.3f;
    public float occlusionSmoothSpeed = 8f;

    [Header("视野设置")]
    [Range(30f, 120f)]
    public float fieldOfView = 60f;
    public float fovSmoothSpeed = 3f;

    [Header("灵敏度设置")]
    [Range(0.1f, 2f)]
    public float positionSensitivity = 1f;
    [Range(0.1f, 2f)]
    public float rotationSensitivity = 1f;

    private Camera cameraComponent;
    private float currentDistance;
    private float currentFOV;
    private Vector3 lastTargetPosition;
    private Quaternion lastTargetRotation;
    private float currentCameraAngle;
    private float cameraAngleVelocity;

    void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        if (cameraComponent == null)
        {
            cameraComponent = gameObject.AddComponent<Camera>();
        }
        
        currentDistance = distance;
        currentFOV = fieldOfView;
        lastTargetPosition = Vector3.zero;
        lastTargetRotation = Quaternion.identity;
        currentCameraAngle = 0f;
        cameraAngleVelocity = 0f;
    }

    void Start()
    {
        if (cameraComponent != null)
        {
            cameraComponent.fieldOfView = currentFOV;
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindLocalPlayer();
            return;
        }

        UpdateCameraPosition();
        UpdateCameraRotation();
        UpdateFieldOfView();
    }

    void UpdateCameraPosition()
    {
        Vector3 targetPosition = CalculateDesiredPosition();
        Vector3 currentPosition = transform.position;

        float deltaTime = Time.deltaTime * positionSensitivity;
        
        Vector3 positionDelta = targetPosition - currentPosition;
        float maxDelta = maxPositionSpeed * deltaTime;
        
        if (positionDelta.magnitude > maxDelta)
        {
            positionDelta = positionDelta.normalized * maxDelta;
        }

        Vector3 newPosition = currentPosition + positionDelta;
        transform.position = newPosition;
    }

    Vector3 CalculateDesiredPosition()
    {
        // 使用当前摄像机角度，而不是直接跟随角色朝向
        // 这样可以避免角色旋转时摄像机突然移动产生的平移感
        float cameraAngle = currentCameraAngle + cameraHorizontalAngle;
        
        // 将角度转换为方向向量
        float radians = cameraAngle * Mathf.Deg2Rad;
        Vector3 cameraDirection = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
        
        // 计算摄像机位置：在角色后方固定距离和高度
        Vector3 desiredOffset = -cameraDirection * distance + Vector3.up * height;
        
        // 应用偏移角度旋转
        Quaternion offsetRotation = Quaternion.Euler(yOffsetAngle, xOffsetAngle, zOffsetAngle);
        desiredOffset = offsetRotation * desiredOffset;

        Vector3 desiredPosition = target.position + desiredOffset;

        Vector3 occlusionAdjustedPosition = AdjustForOcclusion(target.position, desiredPosition);
        
        return occlusionAdjustedPosition;
    }

    Vector3 AdjustForOcclusion(Vector3 targetPos, Vector3 desiredPos)
    {
        Vector3 direction = (desiredPos - targetPos).normalized;
        float maxDistance = Vector3.Distance(targetPos, desiredPos);

        if (Physics.SphereCast(targetPos, occlusionCheckRadius, direction, out RaycastHit hit, maxDistance, obstacleLayer))
        {
            float adjustedDistance = hit.distance - occlusionCheckRadius;
            adjustedDistance = Mathf.Max(adjustedDistance, minDistance);
            
            currentDistance = Mathf.Lerp(currentDistance, adjustedDistance, occlusionSmoothSpeed * Time.deltaTime);
        }
        else
        {
            currentDistance = Mathf.Lerp(currentDistance, distance, occlusionSmoothSpeed * Time.deltaTime);
        }

        return targetPos + direction * currentDistance;
    }

    void UpdateCameraRotation()
    {
        // 计算角色朝向角度
        float targetRotationAngle = target.eulerAngles.y;
        
        // 计算目标摄像机角度（角色朝向 + 固定偏移）
        float targetCameraAngle = targetRotationAngle + cameraHorizontalAngle;
        
        // 使用SmoothDamp进行角度平滑，减少抖动
        // 这种方法比Slerp更平滑，避免了旋转时的抖动感
        float deltaTime = Time.deltaTime * rotationSensitivity;
        currentCameraAngle = Mathf.SmoothDampAngle(
            currentCameraAngle, 
            targetCameraAngle, 
            ref cameraAngleVelocity, 
            1f / rotationSmoothSpeed,
            maxRotationSpeed,
            deltaTime
        );
        
        // 将角度转换为方向向量
        float radians = currentCameraAngle * Mathf.Deg2Rad;
        Vector3 cameraDirection = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
        
        // 计算摄像机应该朝向的位置（看向角色）
        Vector3 lookDirection = target.position - transform.position;
        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        
        // 使用Slerp进行最终旋转平滑，确保摄像机始终看向角色
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * deltaTime);
    }

    void UpdateFieldOfView()
    {
        if (cameraComponent == null)
            return;

        currentFOV = Mathf.Lerp(currentFOV, fieldOfView, fovSmoothSpeed * Time.deltaTime);
        cameraComponent.fieldOfView = currentFOV;
    }

    void FindLocalPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player_new");
        foreach (var player in players)
        {
            NetworkPlayer np = player.GetComponent<NetworkPlayer>();
            if (np != null && np.isLocalPlayer)
            {
                target = player.transform;
                lastTargetPosition = target.position;
                lastTargetRotation = target.rotation;
                // 初始化摄像机角度为角色朝向
                currentCameraAngle = target.eulerAngles.y;
                break;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (target == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, target.position);

        Gizmos.color = Color.yellow;
        Vector3 desiredPosition = CalculateDesiredPosition();
        Gizmos.DrawWireSphere(desiredPosition, 0.5f);

        Gizmos.color = Color.red;
        Vector3 direction = (transform.position - target.position).normalized;
        Gizmos.DrawRay(target.position, direction * distance);
    }
}
