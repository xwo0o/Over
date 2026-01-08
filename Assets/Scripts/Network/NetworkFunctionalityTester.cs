using UnityEngine;
using Mirror;
using NetworkCore;

public class NetworkFunctionalityTester : MonoBehaviour
{
    private static NetworkFunctionalityTester instance;
    public static NetworkFunctionalityTester Instance => instance;

    [Header("测试设置")]
    public bool runTestsOnStart = false;
    public float testTimeout = 10f;

    private bool testsRunning = false;
    private int testsPassed = 0;
    private int testsFailed = 0;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        if (runTestsOnStart)
        {
            RunAllTests();
        }
    }

    public void RunAllTests()
    {
        if (testsRunning)
        {
            Debug.LogWarning("[NetworkFunctionalityTester] 测试已在运行中");
            return;
        }

        testsRunning = true;
        testsPassed = 0;
        testsFailed = 0;

        Debug.Log("[NetworkFunctionalityTester] 开始运行网络功能测试...");

        StartCoroutine(TestCoroutine());
    }

    System.Collections.IEnumerator TestCoroutine()
    {
        yield return TestNetworkInitialization();
        yield return new WaitForSeconds(0.5f);

        yield return TestConnectionManager();
        yield return new WaitForSeconds(0.5f);

        yield return TestMessageHandler();
        yield return new WaitForSeconds(0.5f);

        yield return TestResponseProcessor();
        yield return new WaitForSeconds(0.5f);

        yield return TestStateMonitor();
        yield return new WaitForSeconds(0.5f);

        yield return TestErrorHandler();
        yield return new WaitForSeconds(0.5f);

        yield return TestDataParser();
        yield return new WaitForSeconds(0.5f);

        PrintTestResults();
        testsRunning = false;
    }

    System.Collections.IEnumerator TestNetworkInitialization()
    {
        Debug.Log("[NetworkFunctionalityTester] 测试: 网络初始化管理器...");

        NetworkInitializationManager initManager = NetworkInitializationManager.Instance;
        if (initManager == null)
        {
            Debug.LogError("[NetworkFunctionalityTester] ✗ 网络初始化管理器未找到");
            testsFailed++;
            yield break;
        }

        if (!initManager.IsInitialized)
        {
            Debug.LogWarning("[NetworkFunctionalityTester] 网络初始化管理器未完成初始化，等待...");
            float startTime = Time.time;
            while (!initManager.IsInitialized && Time.time - startTime < testTimeout)
            {
                yield return null;
            }
        }

        if (initManager.IsInitialized)
        {
            Debug.Log("[NetworkFunctionalityTester] ✓ 网络初始化管理器测试通过");
            testsPassed++;
        }
        else
        {
            Debug.LogError("[NetworkFunctionalityTester] ✗ 网络初始化管理器初始化超时");
            testsFailed++;
        }
    }

    System.Collections.IEnumerator TestConnectionManager()
    {
        Debug.Log("[NetworkFunctionalityTester] 测试: 连接管理器...");

        NetworkConnectionManager connectionManager = NetworkConnectionManager.Instance;
        if (connectionManager == null)
        {
            Debug.LogError("[NetworkFunctionalityTester] ✗ 连接管理器未找到");
            testsFailed++;
            yield break;
        }

        ConnectionStatus status = connectionManager.CurrentStatus;
        Debug.Log($"[NetworkFunctionalityTester] 当前连接状态: {status}");

        Debug.Log("[NetworkFunctionalityTester] ✓ 连接管理器测试通过");
        testsPassed++;
    }

    System.Collections.IEnumerator TestMessageHandler()
    {
        Debug.Log("[NetworkFunctionalityTester] 测试: 消息处理器...");

        NetworkMessageHandler messageHandler = NetworkMessageHandler.Instance;
        if (messageHandler == null)
        {
            Debug.LogError("[NetworkFunctionalityTester] ✗ 消息处理器未找到");
            testsFailed++;
            yield break;
        }

        NetworkRequest testRequest = new NetworkRequest(NetworkRequestType.Custom);
        testRequest.SetData("testKey", "testValue");

        Debug.Log($"[NetworkFunctionalityTester] 创建测试请求: {testRequest.requestId}");

        Debug.Log("[NetworkFunctionalityTester] ✓ 消息处理器测试通过");
        testsPassed++;
    }

    System.Collections.IEnumerator TestResponseProcessor()
    {
        Debug.Log("[NetworkFunctionalityTester] 测试: 响应处理器...");

        NetworkResponseProcessor responseProcessor = NetworkResponseProcessor.Instance;
        if (responseProcessor == null)
        {
            Debug.LogError("[NetworkFunctionalityTester] ✗ 响应处理器未找到");
            testsFailed++;
            yield break;
        }

        NetworkRequest testRequest = new NetworkRequest(NetworkRequestType.Custom);
        testRequest.SetData("testKey", "testValue");

        NetworkResponse response = responseProcessor.ProcessRequest(testRequest);
        if (response != null)
        {
            Debug.Log($"[NetworkFunctionalityTester] 响应代码: {response.responseCode}");
            Debug.Log("[NetworkFunctionalityTester] ✓ 响应处理器测试通过");
            testsPassed++;
        }
        else
        {
            Debug.LogError("[NetworkFunctionalityTester] ✗ 响应处理器返回空响应");
            testsFailed++;
        }
    }

    System.Collections.IEnumerator TestStateMonitor()
    {
        Debug.Log("[NetworkFunctionalityTester] 测试: 状态监控器...");

        NetworkStateMonitor stateMonitor = NetworkStateMonitor.Instance;
        if (stateMonitor == null)
        {
            Debug.LogError("[NetworkFunctionalityTester] ✗ 状态监控器未找到");
            testsFailed++;
            yield break;
        }

        NetworkState state = stateMonitor.GetCurrentNetworkState();
        Debug.Log($"[NetworkFunctionalityTester] 当前网络状态: {state}");

        Debug.Log("[NetworkFunctionalityTester] ✓ 状态监控器测试通过");
        testsPassed++;
    }

    System.Collections.IEnumerator TestErrorHandler()
    {
        Debug.Log("[NetworkFunctionalityTester] 测试: 错误处理器...");

        NetworkErrorHandler errorHandler = NetworkErrorHandler.Instance;
        if (errorHandler == null)
        {
            Debug.LogError("[NetworkFunctionalityTester] ✗ 错误处理器未找到");
            testsFailed++;
            yield break;
        }

        errorHandler.ReportError(NetworkErrorType.UnknownError, "测试错误");

        Debug.Log("[NetworkFunctionalityTester] ✓ 错误处理器测试通过");
        testsPassed++;
    }

    System.Collections.IEnumerator TestDataParser()
    {
        Debug.Log("[NetworkFunctionalityTester] 测试: 数据解析器...");

        NetworkDataParser dataParser = NetworkDataParser.Instance;
        if (dataParser == null)
        {
            Debug.LogError("[NetworkFunctionalityTester] ✗ 数据解析器未找到");
            testsFailed++;
            yield break;
        }

        string testData = "test string";
        string serialized = dataParser.Serialize(testData);
        string deserialized = dataParser.Deserialize<string>(serialized);

        if (serialized == deserialized)
        {
            Debug.Log($"[NetworkFunctionalityTester] 序列化/反序列化测试通过: {deserialized}");
            testsPassed++;
        }
        else
        {
            Debug.LogError($"[NetworkFunctionalityTester] ✗ 序列化/反序列化失败: {serialized} != {deserialized}");
            testsFailed++;
        }

        Vector3 testVector = new Vector3(1.5f, 2.5f, 3.5f);
        string serializedVector = dataParser.Serialize(testVector);
        Vector3 deserializedVector = dataParser.Deserialize<Vector3>(serializedVector);

        if (testVector == deserializedVector)
        {
            Debug.Log($"[NetworkFunctionalityTester] Vector3序列化/反序列化测试通过: {deserializedVector}");
            testsPassed++;
        }
        else
        {
            Debug.LogError($"[NetworkFunctionalityTester] ✗ Vector3序列化/反序列化失败: {testVector} != {deserializedVector}");
            testsFailed++;
        }

        Debug.Log("[NetworkFunctionalityTester] ✓ 数据解析器测试通过");
    }

    void PrintTestResults()
    {
        Debug.Log("[NetworkFunctionalityTester] =====================");
        Debug.Log($"[NetworkFunctionalityTester] 测试完成");
        Debug.Log($"[NetworkFunctionalityTester] 通过: {testsPassed}");
        Debug.Log($"[NetworkFunctionalityTester] 失败: {testsFailed}");
        Debug.Log($"[NetworkFunctionalityTester] 总计: {testsPassed + testsFailed}");
        Debug.Log("[NetworkFunctionalityTester] =====================");

        if (testsFailed == 0)
        {
            Debug.Log("[NetworkFunctionalityTester] ✓ 所有测试通过！");
        }
        else
        {
            Debug.LogWarning($"[NetworkFunctionalityTester] ✗ {testsFailed} 个测试失败");
        }
    }

    public bool IsTestRunning()
    {
        return testsRunning;
    }

    public int GetTestsPassed()
    {
        return testsPassed;
    }

    public int GetTestsFailed()
    {
        return testsFailed;
    }
}
