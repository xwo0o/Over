using UnityEngine;

/// <summary>
/// 攻击范围可视化器
/// 使用LineRenderer绘制角色攻击范围，在Game视图中显示，不受Gizmos影响
/// </summary>
[RequireComponent(typeof(PlayerInputHandler))]
public class AttackRangeVisualizer : MonoBehaviour
{
    [Header("可视化设置")]
    [Tooltip("线条颜色")]
    public Color lineColor = new Color(1f, 0f, 0f, 0.5f);

    [Tooltip("线条宽度")]
    public float lineWidth = 0.05f;

    [Tooltip("扇形分段数，值越高越平滑")]
    public int segmentCount = 30;

    [Tooltip("是否填充扇形内部")]
    public bool fillInside = false;

    [Tooltip("填充颜色")]
    public Color fillColor = new Color(1f, 0f, 0f, 0.1f);

    [Tooltip("画线原点Y轴偏移（相对于角色中心）")]
    public float originYOffset = -0.9f;

    private PlayerInputHandler inputHandler;
    private LineRenderer lineRenderer;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private GameObject lineRendererObj;

    void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        CreateLineRenderer();
        CreateFillMesh();
    }

    void Start()
    {
        UpdateAttackRange();
    }

    void Update()
    {
        UpdateAttackRange();
    }

    /// <summary>
    /// 创建LineRenderer组件
    /// </summary>
    void CreateLineRenderer()
    {
        lineRendererObj = new GameObject("AttackRangeLine");
        lineRendererObj.transform.SetParent(transform);
        lineRendererObj.transform.localPosition = new Vector3(0f, originYOffset, 0f);
        lineRendererObj.transform.localRotation = Quaternion.identity;

        lineRenderer = lineRendererObj.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
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
        if (inputHandler == null || lineRenderer == null)
        {
            return;
        }

        // 只在攻击周期中显示攻击范围
        bool shouldShow = inputHandler.IsInAttackCycle;

        float range = inputHandler.attackRange;
        float angle = inputHandler.attackAngle;

        // 计算扇形点
        Vector3[] points = CalculateSectorPoints(range, angle);

        // 更新LineRenderer
        lineRenderer.enabled = shouldShow;
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // 更新填充网格
        if (fillInside)
        {
            UpdateFillMesh(points);
            meshRenderer.enabled = shouldShow;
        }
        else
        {
            meshRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 计算扇形点
    /// </summary>
    Vector3[] CalculateSectorPoints(float range, float angle)
    {
        int pointCount = segmentCount + 2; // +2 for center and closing point
        Vector3[] points = new Vector3[pointCount];

        // 第一个点是中心点
        points[0] = Vector3.zero;

        // 计算扇形弧线上的点
        float halfAngle = angle * 0.5f;
        float startAngle = -halfAngle;
        float angleStep = angle / segmentCount;

        for (int i = 0; i <= segmentCount; i++)
        {
            float currentAngle = startAngle + angleStep * i;
            float radians = currentAngle * Mathf.Deg2Rad;

            // 在XZ平面上计算点
            float x = Mathf.Sin(radians) * range;
            float z = Mathf.Cos(radians) * range;

            points[i + 1] = new Vector3(x, 0f, z);
        }

        return points;
    }

    /// <summary>
    /// 更新填充网格
    /// </summary>
    void UpdateFillMesh(Vector3[] points)
    {
        if (meshFilter == null || meshFilter.mesh == null)
        {
            return;
        }

        Mesh mesh = meshFilter.mesh;
        mesh.Clear();

        // 创建三角形
        int triangleCount = segmentCount;
        int[] triangles = new int[triangleCount * 3];
        Vector3[] vertices = new Vector3[points.Length];
        Vector2[] uv = new Vector2[points.Length];

        // 复制点作为顶点
        for (int i = 0; i < points.Length; i++)
        {
            vertices[i] = points[i];
            uv[i] = new Vector2(points[i].x, points[i].z);
        }

        // 创建三角形（从中心点向外的扇形）
        for (int i = 0; i < triangleCount; i++)
        {
            triangles[i * 3] = 0; // 中心点
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
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

        if (lineRendererObj != null)
        {
            lineRendererObj.transform.localPosition = new Vector3(0f, originYOffset, 0f);
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
