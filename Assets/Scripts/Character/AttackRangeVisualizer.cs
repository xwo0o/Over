using UnityEngine;

/// <summary>
/// 攻击范围可视化器
/// 根据武器配置绘制圆形攻击范围，只在攻击动画播放时显示
/// </summary>
public class AttackRangeVisualizer : MonoBehaviour
{
    [Header("可视化设置")]
    [Tooltip("线条颜色")]
    public Color lineColor = new Color(1f, 0.3f, 0f, 0.8f);

    [Tooltip("线条宽度")]
    public float lineWidth = 0.03f;

    [Tooltip("圆形分段数，值越高越平滑")]
    public int segmentCount = 50;

    [Tooltip("是否填充圆形内部")]
    public bool fillInside = false;

    [Tooltip("填充颜色")]
    public Color fillColor = new Color(1f, 0.3f, 0f, 0.2f);

    [Tooltip("画线原点Y轴偏移（相对于角色中心）")]
    public float originYOffset = 0f;

    [Header("武器配置（只读，数据来自配置文件）")]
    [Tooltip("当前武器数据 - 数据来自WeaponData.json配置文件")]
    [SerializeField]
    private WeaponData currentWeaponData;

    [Header("状态（运行时）")]
    [Tooltip("当前是否正在攻击")]
    [SerializeField]
    private bool isAttacking = false;

    [Header("超时保护")]
    [Tooltip("攻击画线最大显示时间（秒）")]
    public float maxAttackDisplayTime = 2.0f;

    private float attackStartTime = 0f;
    private LineRenderer lineRenderer;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private GameObject lineRendererObj;
    private Transform playerTransform;

    void Awake()
    {
        playerTransform = transform;
        CreateLineRenderer();
        CreateFillMesh();
        
        // 初始隐藏
        HideAttackRange();
    }

    void Update()
    {
        // 只有在攻击状态才更新和显示
        if (isAttacking)
        {
            // 关键修复：超时保护，防止画线一直显示
            if (Time.time - attackStartTime > maxAttackDisplayTime)
            {
                Debug.LogWarning("[AttackRangeVisualizer] 攻击画线超时，自动隐藏");
                EndAttack();
                return;
            }
            
            UpdateAttackRange();
        }
    }

    /// <summary>
    /// 设置当前武器数据
    /// </summary>
    public void SetWeaponData(WeaponData weaponData)
    {
        currentWeaponData = weaponData;
    }

    /// <summary>
    /// 获取当前武器数据（只读）
    /// </summary>
    public WeaponData GetCurrentWeaponData()
    {
        return currentWeaponData;
    }

    /// <summary>
    /// 开始攻击 - 显示攻击范围
    /// </summary>
    public void StartAttack()
    {
        isAttacking = true;
        attackStartTime = Time.time; // 关键修复：记录开始时间
        UpdateAttackRange();
    }

    /// <summary>
    /// 结束攻击 - 隐藏攻击范围
    /// </summary>
    public void EndAttack()
    {
        isAttacking = false;
        HideAttackRange();
    }

    /// <summary>
    /// 隐藏攻击范围
    /// </summary>
    void HideAttackRange()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 创建LineRenderer组件
    /// </summary>
    void CreateLineRenderer()
    {
        lineRendererObj = new GameObject("AttackRangeLine");
        lineRendererObj.transform.SetParent(transform);
        lineRendererObj.transform.localPosition = Vector3.zero;
        lineRendererObj.transform.localRotation = Quaternion.identity;

        lineRenderer = lineRendererObj.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true; // 圆形需要闭合
    }

    /// <summary>
    /// 创建填充网格组件
    /// </summary>
    void CreateFillMesh()
    {
        GameObject fillMeshObj = new GameObject("AttackRangeFill");
        fillMeshObj.transform.SetParent(transform);
        fillMeshObj.transform.localPosition = Vector3.zero;
        fillMeshObj.transform.localRotation = Quaternion.identity;

        meshFilter = fillMeshObj.AddComponent<MeshFilter>();
        meshRenderer = fillMeshObj.AddComponent<MeshRenderer>();

        Material fillMaterial = new Material(Shader.Find("Unlit/Transparent"));
        fillMaterial.color = fillColor;
        meshRenderer.material = fillMaterial;

        meshFilter.mesh = new Mesh();
        meshFilter.mesh.name = "AttackRangeMesh";
    }

    /// <summary>
    /// 更新攻击范围显示
    /// </summary>
    void UpdateAttackRange()
    {
        if (currentWeaponData == null || lineRenderer == null)
        {
            HideAttackRange();
            return;
        }

        // 获取攻击范围（从配置文件）
        float range = currentWeaponData.attackRange;

        // 计算圆形点
        Vector3[] points = CalculateCirclePoints(range);

        // 更新LineRenderer
        lineRenderer.enabled = true;
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // 更新填充网格
        if (fillInside)
        {
            UpdateFillCircleMesh(points);
            meshRenderer.enabled = true;
        }
        else
        {
            meshRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 计算圆形点
    /// </summary>
    Vector3[] CalculateCirclePoints(float range)
    {
        Vector3[] points = new Vector3[segmentCount];

        // 角色位置
        Vector3 origin = playerTransform.position + Vector3.up * originYOffset;

        // 计算圆形上的点
        float angleStep = 360f / segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * range;
            float z = Mathf.Sin(angle) * range;
            
            points[i] = origin + new Vector3(x, 0, z);
        }

        return points;
    }

    /// <summary>
    /// 更新填充圆形网格
    /// </summary>
    void UpdateFillCircleMesh(Vector3[] points)
    {
        if (meshFilter == null || meshFilter.mesh == null)
        {
            return;
        }

        Mesh mesh = meshFilter.mesh;
        mesh.Clear();

        // 创建三角形（从中心点向外的扇形）
        int triangleCount = segmentCount;
        int[] triangles = new int[triangleCount * 3];
        
        // 顶点：中心点 + 圆形上的点
        Vector3[] vertices = new Vector3[segmentCount + 1];
        Vector2[] uv = new Vector2[segmentCount + 1];

        // 角色位置作为中心点
        Vector3 origin = playerTransform.position + Vector3.up * originYOffset;
        vertices[0] = origin;
        uv[0] = new Vector2(0.5f, 0.5f);

        // 圆形上的点
        for (int i = 0; i < segmentCount; i++)
        {
            vertices[i + 1] = points[i];
            uv[i + 1] = new Vector2(
                (points[i].x - origin.x) / (currentWeaponData.attackRange * 2) + 0.5f,
                (points[i].z - origin.z) / (currentWeaponData.attackRange * 2) + 0.5f
            );
        }

        // 创建三角形
        for (int i = 0; i < triangleCount; i++)
        {
            triangles[i * 3] = 0; // 中心点
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 2) > segmentCount ? 1 : i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    /// <summary>
    /// 在Inspector中实时更新
    /// </summary>
    void OnValidate()
    {
        if (lineRenderer != null)
        {
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
        }

        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = fillColor;
        }
    }

    void OnDestroy()
    {
        if (meshFilter != null && meshFilter.mesh != null)
        {
            Destroy(meshFilter.mesh);
        }
    }
}
