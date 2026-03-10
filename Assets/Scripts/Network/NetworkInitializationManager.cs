using UnityEngine;
using Mirror;
using System;
using NetworkCore;

[DefaultExecutionOrder(-200)]
public class NetworkInitializationManager : MonoBehaviour
{
    private static NetworkInitializationManager instance;
    public static NetworkInitializationManager Instance => instance;

    [Header("网络组件引用")]
    public GameNetworkManager gameNetworkManager;
    public NetworkMessageHandler messageHandler;
    public NetworkResponseProcessor responseProcessor;
    public NetworkConnectionManager connectionManager;
    public NetworkStateMonitor stateMonitor;
    public NetworkErrorHandler errorHandler;
    public NetworkDataParser dataParser;

    [Header("初始化设置")]
    public bool autoInitialize = true;
    public float initializationTimeout = 180f;

    public bool IsInitialized { get; private set; }
    public event Action OnInitializationComplete;
    public event Action<string> OnInitializationFailed;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        if (autoInitialize)
        {
            InitializeNetworkComponents();
        }
    }

    public void InitializeNetworkComponents()
    {
        StartCoroutine(InitializationCoroutine());
    }

    System.Collections.IEnumerator InitializationCoroutine()
    {
        Debug.Log("[NetworkInitializationManager] 开始初始化网络组件...");
        float startTime = Time.time;

        while (Time.time - startTime < initializationTimeout)
        {
            if (TryInitializeComponents())
            {
                IsInitialized = true;
                OnInitializationComplete?.Invoke();
                Debug.Log("[NetworkInitializationManager] 网络组件初始化完成");
                yield break;
            }
            yield return new WaitForSeconds(0.1f);
        }

        string errorMessage = $"网络组件初始化超时（{initializationTimeout}秒）";
        Debug.LogError($"[NetworkInitializationManager] {errorMessage}");
        OnInitializationFailed?.Invoke(errorMessage);
    }

    bool TryInitializeComponents()
    {
        if (!InitializeDataParser())
        {
            return false;
        }

        if (!InitializeErrorHandler())
        {
            return false;
        }

        if (!InitializeMessageHandler())
        {
            return false;
        }

        if (!InitializeResponseProcessor())
        {
            return false;
        }

        if (!InitializeConnectionManager())
        {
            return false;
        }

        if (!InitializeStateMonitor())
        {
            return false;
        }

        if (!InitializeGameNetworkManager())
        {
            return false;
        }

        SetupComponentDependencies();
        return true;
    }

    bool InitializeDataParser()
    {
        if (dataParser != null)
        {
            return true;
        }

        dataParser = FindObjectOfType<NetworkDataParser>();
        if (dataParser == null)
        {
            GameObject parserObj = new GameObject("NetworkDataParser");
            dataParser = parserObj.AddComponent<NetworkDataParser>();
            DontDestroyOnLoad(parserObj);
        }
        return dataParser != null;
    }

    bool InitializeErrorHandler()
    {
        if (errorHandler != null)
        {
            return true;
        }

        errorHandler = FindObjectOfType<NetworkErrorHandler>();
        if (errorHandler == null)
        {
            GameObject handlerObj = new GameObject("NetworkErrorHandler");
            errorHandler = handlerObj.AddComponent<NetworkErrorHandler>();
            DontDestroyOnLoad(handlerObj);
        }
        return errorHandler != null;
    }

    bool InitializeMessageHandler()
    {
        if (messageHandler != null)
        {
            return true;
        }

        messageHandler = FindObjectOfType<NetworkMessageHandler>();
        if (messageHandler == null)
        {
            GameObject handlerObj = new GameObject("NetworkMessageHandler");
            messageHandler = handlerObj.AddComponent<NetworkMessageHandler>();
            DontDestroyOnLoad(handlerObj);
        }
        return messageHandler != null;
    }

    bool InitializeResponseProcessor()
    {
        if (responseProcessor != null)
        {
            return true;
        }

        responseProcessor = FindObjectOfType<NetworkResponseProcessor>();
        if (responseProcessor == null)
        {
            GameObject processorObj = new GameObject("NetworkResponseProcessor");
            responseProcessor = processorObj.AddComponent<NetworkResponseProcessor>();
            DontDestroyOnLoad(processorObj);
        }
        return responseProcessor != null;
    }

    bool InitializeConnectionManager()
    {
        if (connectionManager != null)
        {
            return true;
        }

        connectionManager = FindObjectOfType<NetworkConnectionManager>();
        if (connectionManager == null)
        {
            GameObject managerObj = new GameObject("NetworkConnectionManager");
            connectionManager = managerObj.AddComponent<NetworkConnectionManager>();
            DontDestroyOnLoad(managerObj);
        }
        return connectionManager != null;
    }

    bool InitializeStateMonitor()
    {
        if (stateMonitor != null)
        {
            return true;
        }

        stateMonitor = FindObjectOfType<NetworkStateMonitor>();
        if (stateMonitor == null)
        {
            GameObject monitorObj = new GameObject("NetworkStateMonitor");
            stateMonitor = monitorObj.AddComponent<NetworkStateMonitor>();
            DontDestroyOnLoad(monitorObj);
        }
        return stateMonitor != null;
    }

    bool InitializeGameNetworkManager()
    {
        if (gameNetworkManager != null)
        {
            return true;
        }

        gameNetworkManager = FindObjectOfType<GameNetworkManager>();
        if (gameNetworkManager == null)
        {
            GameObject managerObj = new GameObject("GameNetworkManager");
            gameNetworkManager = managerObj.AddComponent<GameNetworkManager>();
            DontDestroyOnLoad(managerObj);
        }
        return gameNetworkManager != null;
    }

    void SetupComponentDependencies()
    {
        if (connectionManager != null)
        {
            connectionManager.OnConnected += HandleConnectionEstablished;
            connectionManager.OnDisconnected += HandleConnectionLost;
            connectionManager.OnConnectionError += HandleConnectionError;
        }

        if (stateMonitor != null)
        {
            stateMonitor.OnNetworkStateChanged += HandleNetworkStateChanged;
        }

        if (errorHandler != null)
        {
            errorHandler.OnNetworkError += HandleNetworkError;
        }
    }

    void HandleConnectionEstablished()
    {
        Debug.Log("[NetworkInitializationManager] 网络连接已建立");
    }

    void HandleConnectionLost()
    {
        Debug.LogWarning("[NetworkInitializationManager] 网络连接已断开");
    }

    void HandleConnectionError(string errorMessage)
    {
        Debug.LogError($"[NetworkInitializationManager] 连接错误: {errorMessage}");
        if (errorHandler != null)
        {
            errorHandler.ReportError(NetworkErrorType.ConnectionError, errorMessage);
        }
    }

    void HandleNetworkStateChanged(NetworkState newState)
    {
        Debug.Log($"[NetworkInitializationManager] 网络状态变更: {newState}");
    }

    void HandleNetworkError(NetworkError error)
    {
        Debug.LogError($"[NetworkInitializationManager] 网络错误: {error.errorType} - {error.errorMessage}");
    }

    public void StartHost(string serverAddress = "127.0.0.1", int port = 7777)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[NetworkInitializationManager] 网络组件未初始化，无法启动主机");
            return;
        }

        if (connectionManager != null)
        {
            connectionManager.StartHost(serverAddress, port);
        }
    }

    public void StartClient(string serverAddress = "127.0.0.1", int port = 7777)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[NetworkInitializationManager] 网络组件未初始化，无法启动客户端");
            return;
        }

        if (connectionManager != null)
        {
            connectionManager.StartClient(serverAddress, port);
        }
    }

    public void Disconnect()
    {
        if (connectionManager != null)
        {
            connectionManager.Disconnect();
        }
    }

    void OnDestroy()
    {
        if (connectionManager != null)
        {
            connectionManager.OnConnected -= HandleConnectionEstablished;
            connectionManager.OnDisconnected -= HandleConnectionLost;
            connectionManager.OnConnectionError -= HandleConnectionError;
        }

        if (stateMonitor != null)
        {
            stateMonitor.OnNetworkStateChanged -= HandleNetworkStateChanged;
        }

        if (errorHandler != null)
        {
            errorHandler.OnNetworkError -= HandleNetworkError;
        }
    }
}
