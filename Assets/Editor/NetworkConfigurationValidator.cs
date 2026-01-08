using UnityEngine;
using UnityEditor;
using Mirror;
using System.Reflection;

public class NetworkConfigurationValidator : EditorWindow
{
    [MenuItem("Tools/Network/验证网络配置")]
    public static void ShowWindow()
    {
        GetWindow<NetworkConfigurationValidator>("网络配置验证器");
    }

    void OnGUI()
    {
        GUILayout.Label("NetworkManager和Transport配置验证", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("验证当前场景"))
        {
            ValidateCurrentScene();
        }

        if (GUILayout.Button("检测ParrelSync环境"))
        {
            DetectParrelSyncEnvironment();
        }

        GUILayout.Space(10);
        GUILayout.Label("验证结果:", EditorStyles.boldLabel);
        
        if (validationResult != null)
        {
            EditorGUILayout.HelpBox(validationResult, validationType);
        }
    }

    private string validationResult = "";
    private MessageType validationType = MessageType.None;

    void ValidateCurrentScene()
    {
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();
        
        if (networkManager == null)
        {
            validationResult = "错误：场景中未找到NetworkManager组件";
            validationType = MessageType.Error;
            return;
        }

        Transport transport = networkManager.GetComponent<Transport>();
        
        if (transport == null)
        {
            validationResult = "错误：NetworkManager上未找到Transport组件";
            validationType = MessageType.Error;
            return;
        }

        string result = $"NetworkManager: {networkManager.name}\n";
        result += $"Transport类型: {transport.GetType().Name}\n";
        result += $"Transport.active: {(Transport.active != null ? Transport.active.GetType().Name : "null")}\n";
        result += $"NetworkManager.singleton: {(NetworkManager.singleton != null ? NetworkManager.singleton.name : "null")}\n";
        
        // 获取Transport端口配置
        try
        {
            var portProperty = transport.GetType().GetProperty("Port");
            if (portProperty != null)
            {
                int port = (int)portProperty.GetValue(transport);
                result += $"Transport端口: {port}\n";
            }
        }
        catch
        {
            result += "Transport端口: 无法读取\n";
        }

        // 检查NetworkManager配置
        result += $"网络地址: {networkManager.networkAddress}\n";
        result += $"最大连接数: {networkManager.maxConnections}\n";
        result += $"发送频率: {networkManager.sendRate}\n";
        
        if (Transport.active != null && Transport.active == transport)
        {
            result += "\n✓ Transport.active正确配置";
            validationType = MessageType.Info;
        }
        else
        {
            result += "\n✗ Transport.active未正确配置，可能导致连接失败";
            validationType = MessageType.Warning;
        }

        // 检查是否在ParrelSync环境中
        bool isParrelSync = IsParrelSyncEnvironment();
        if (isParrelSync)
        {
            result += "\n⚠ 检测到ParrelSync环境，请确保Transport端口配置正确";
        }

        validationResult = result;
        Debug.Log(result);
    }

    void DetectParrelSyncEnvironment()
    {
        string result = "ParrelSync环境检测结果:\n\n";
        
        bool isParrelSync = IsParrelSyncEnvironment();
        result += $"是否在ParrelSync环境中: {(isParrelSync ? "是" : "否")}\n";
        
        if (isParrelSync)
        {
            result += "\nParrelSync环境注意事项:\n";
            result += "- 每个复制的编辑器实例需要使用不同的端口\n";
            result += "- 建议主机使用端口7777，客户端使用端口7778\n";
            result += "- 确保NetworkManager上的Transport组件已正确配置\n";
            result += "- Transport.active需要在运行时正确初始化\n";
            result += "- 检查Clones Manager中的端口偏移设置\n";
            
            validationType = MessageType.Warning;
        }
        else
        {
            result += "\n当前不在ParrelSync环境中";
            validationType = MessageType.Info;
        }

        // 检查当前场景的NetworkManager配置
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager != null)
        {
            result += "\n\n当前场景NetworkManager配置:\n";
            result += $"名称: {networkManager.name}\n";
            
            Transport transport = networkManager.GetComponent<Transport>();
            if (transport != null)
            {
                result += $"Transport类型: {transport.GetType().Name}\n";
                try
                {
                    var portProperty = transport.GetType().GetProperty("Port");
                    if (portProperty != null)
                    {
                        int port = (int)portProperty.GetValue(transport);
                        result += $"端口: {port}\n";
                    }
                }
                catch
                {
                    result += "端口: 无法读取\n";
                }
            }
        }

        validationResult = result;
        Debug.Log(result);
    }

    bool IsParrelSyncEnvironment()
    {
        // 检查是否在ParrelSync复制的编辑器中运行
        // ParrelSync通常会在项目路径中添加特定标识
        string projectPath = Application.dataPath;
        
        // 检查是否有ParrelSync相关的文件夹或文件
        if (System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, "../ParrelSync")))
        {
            return true;
        }

        // 检查项目名称是否包含ParrelSync的标识
        if (projectPath.Contains("_Clone"))
        {
            return true;
        }

        // 检查是否有ParrelSync的配置文件
        string parrelSyncConfig = System.IO.Path.Combine(Application.dataPath, "../ProjectSettings/ParrelSyncSettings.asset");
        if (System.IO.File.Exists(parrelSyncConfig))
        {
            return true;
        }

        return false;
    }
}
