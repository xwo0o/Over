using Mirror;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovementController : NetworkBehaviour
{
    public float moveSpeed = 5f;

    [Header("旋转设置")]
    [Tooltip("旋转平滑速度")]
    public float rotationSmoothSpeed = 10f;
    
    [Header("重力设置")]
    [Tooltip("重力强度")]
    public float gravity = -9.81f;
    
    [Tooltip("地面检测距离")]
    public float groundDistance = 0.1f;
    
    [Tooltip("地面层")]
    public LayerMask groundMask;

    Vector2 input;
    CharacterController controller;
    Rigidbody rb; // 添加对Rigidbody的引用
    float currentRotationY;
    float verticalVelocity;
    bool isGrounded;

    void Awake()
    {
        // 关键修复：确保CharacterController组件被正确获取
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("[CharacterMovementController] 未找到CharacterController组件！");
            return;
        }
        
        // 获取Rigidbody组件
        rb = GetComponent<Rigidbody>(); 
        
        // 如果存在Rigidbody，确保其设置为运动学以避免与CharacterController冲突
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            Debug.Log("[CharacterMovementController] 已将Rigidbody设置为运动学，避免与CharacterController冲突");
        }
        
        // 检查是否有其他可能导致冲突的物理组件
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            // 跳过CharacterController自带的碰撞体
            if (collider is CharacterController)
                continue;
                
            // 设置其他碰撞体为触发器，避免与CharacterController冲突
            collider.isTrigger = true;
        }
        
        currentRotationY = transform.eulerAngles.y;
        verticalVelocity = 0f;
        
        // 设置默认的地面层（Layer 0 - Default）
        if (groundMask == 0)
            groundMask = -1; // 所有层
        
        Debug.Log("[CharacterMovementController] 初始化完成，CharacterController: " + (controller != null ? "存在" : "不存在"));
    }

    public void SetInput(Vector2 value)
    {
        input = value;
    }

    public void SetRotation(float rotationY)
    {
        currentRotationY = rotationY;
    }

    public float GetCurrentRotation()
    {
        return currentRotationY;
    }

    [ServerCallback]
    void Update()
    {
        // 确保controller已初始化
        if (controller != null)
        {
            UpdateMovement();
        }
    }

    [Server]
    void UpdateMovement()
    {
        // 关键修复：确保CharacterController存在且有效
        if (controller == null)
        {
            return;
        }
        
        // 检查是否在地面上
        // 修复：使用更可靠的地面检测方法
        // 使用CharacterController的底部作为检测点，而不是中心
        Vector3 groundCheckPosition = transform.position + Vector3.down * (controller.height * 0.5f - controller.radius);
        isGrounded = Physics.CheckSphere(groundCheckPosition, controller.radius + groundDistance, groundMask, QueryTriggerInteraction.Ignore);
        
        // 应用重力
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -0.5f; // 稍微向下以保持贴地，减少穿透
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        
        Vector3 move = new Vector3(input.x, 0f, input.y);
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        if (move.sqrMagnitude > 0.01f)
        {
            move = transform.TransformDirection(move);
        }
        
        // 计算移动向量（包含重力）
        Vector3 movement = new Vector3(move.x, verticalVelocity, move.z) * Time.deltaTime;
        movement *= moveSpeed;
        
        controller.Move(movement);
        
        // 如果角色在地面上且垂直速度为负（向下），重置垂直速度
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -0.5f;
        }
    }
}
