using UnityEngine;

/// <summary>
/// 自动化对象池测试脚本
/// </summary>
public class AutoObjectPoolTest : MonoBehaviour
{
    [Header("测试设置")]
    public string testPoolId = "SmallEnemy";
    public int testCount = 5;
    public float spawnInterval = 1f;
    public float returnDelay = 3f;

    private float spawnTimer = 0f;
    private int spawnedCount = 0;
    private System.Collections.Generic.List<GameObject> spawnedObjects = new System.Collections.Generic.List<GameObject>();

    private void Start()
    {
        Debug.Log("AutoObjectPoolTest: 开始测试自动化对象池系统");
        
        // 检查自动化对象池管理器是否存在
        if (AutoObjectPoolManager.Instance == null)
        {
            Debug.LogError("AutoObjectPoolTest: AutoObjectPoolManager不存在");
            enabled = false;
            return;
        }
        
        Debug.Log("AutoObjectPoolTest: AutoObjectPoolManager已初始化");
    }

    private void Update()
    {
        // 测试对象生成
        if (spawnedCount < testCount)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                SpawnTestObject();
                spawnedCount++;
            }
        }
    }

    /// <summary>
    /// 生成测试对象
    /// </summary>
    private void SpawnTestObject()
    {
        Vector3 spawnPosition = new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
        
        // 从对象池获取对象
        GameObject obj = AutoObjectPoolManager.Instance.GetObject(testPoolId, spawnPosition, Quaternion.identity);
        
        if (obj != null)
        {
            Debug.Log($"AutoObjectPoolTest: 成功从池 {testPoolId} 获取对象，位置: {spawnPosition}");
            spawnedObjects.Add(obj);
            
            // 延迟归还对象
            StartCoroutine(ReturnTestObject(obj, returnDelay));
        }
        else
        {
            Debug.LogError($"AutoObjectPoolTest: 从池 {testPoolId} 获取对象失败");
        }
    }

    /// <summary>
    /// 延迟归还测试对象
    /// </summary>
    /// <param name="obj">要归还的对象</param>
    /// <param name="delay">延迟时间（秒）</param>
    private System.Collections.IEnumerator ReturnTestObject(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (obj != null && spawnedObjects.Contains(obj))
        {
            // 归还对象到对象池
            AutoObjectPoolManager.Instance.ReturnObject(testPoolId, obj);
            spawnedObjects.Remove(obj);
            
            Debug.Log($"AutoObjectPoolTest: 对象已归还到池 {testPoolId}");
        }
    }

    /// <summary>
    /// 手动测试获取对象
    /// </summary>
    [ContextMenu("测试获取对象")]
    public void TestGetObject()
    {
        Vector3 spawnPosition = new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
        GameObject obj = AutoObjectPoolManager.Instance.GetObject(testPoolId, spawnPosition, Quaternion.identity);
        
        if (obj != null)
        {
            Debug.Log($"AutoObjectPoolTest: 手动测试 - 成功获取对象，位置: {spawnPosition}");
            spawnedObjects.Add(obj);
            StartCoroutine(ReturnTestObject(obj, returnDelay));
        }
        else
        {
            Debug.LogError($"AutoObjectPoolTest: 手动测试 - 获取对象失败");
        }
    }

    /// <summary>
    /// 手动测试清理对象池
    /// </summary>
    [ContextMenu("测试清理对象池")]
    public void TestClearPool()
    {
        AutoObjectPoolManager.Instance.ClearPool(testPoolId);
        Debug.Log($"AutoObjectPoolTest: 已清理对象池 {testPoolId}");
    }

    /// <summary>
    /// 手动测试清理所有对象池
    /// </summary>
    [ContextMenu("测试清理所有对象池")]
    public void TestClearAllPools()
    {
        AutoObjectPoolManager.Instance.ClearAllPools();
        Debug.Log("AutoObjectPoolTest: 已清理所有对象池");
    }
}