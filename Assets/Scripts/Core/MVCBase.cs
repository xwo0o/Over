using UnityEngine;
using System.Collections;
using Mirror;

/// <summary>
/// MVC架构 - Model基类
/// </summary>
/// <typeparam name="T">具体的Model类型</typeparam>
public abstract class MVCModel<T> where T : class
{
    public static T Instance { get; private set; }
    
    protected virtual void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            return;
        }
        Instance = this as T;
    }
    
    protected void SetInstance(T instance)
    {
        Instance = instance;
    }
}

/// <summary>
/// MVC架构 - View基类
/// </summary>
public abstract class MVCView : MonoBehaviour
{
    public abstract void BindModel(object model);
    public abstract void UpdateView();
}

/// <summary>
/// MVC架构 - Controller基类
/// </summary>
public abstract class MVCController : NetworkBehaviour
{
    protected virtual void InitializeNetwork()
    {
        if (isServer) OnServerInitialize();
        if (isClient) OnClientInitialize();
    }
    
    protected virtual void OnServerInitialize() { }
    protected virtual void OnClientInitialize() { }
}