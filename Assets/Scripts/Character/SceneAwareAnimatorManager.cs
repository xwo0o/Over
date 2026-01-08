using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;

/// <summary>
/// 场景感知的动画管理器 - 解耦Character场景和GameScene的动画获取逻辑
/// </summary>
public class SceneAwareAnimatorManager : MonoBehaviour
{
    [Header("动画引用配置")]
    [Tooltip("角色选择场景中的动画引用")]
    [SerializeField] private Animator characterSelectionAnimator;
    
    [Tooltip("游戏场景中的动画路径")]
    [SerializeField] private string gameSceneAnimatorPath = "ModelParent";
    
    [Header("调试信息")]
    [SerializeField] private bool isInCharacterScene = false;
    [SerializeField] private bool isInGameScene = false;
    
    private List<Animator> currentAnimators = new List<Animator>();
    private NetworkPlayer networkPlayer;
    private bool hasInitialized = false;
    
    // 初始化完成事件
    public event Action<List<Animator>> OnAnimatorsInitialized;
    
    // 初始化状态
    public bool IsInitialized => hasInitialized;
    public bool HasValidAnimator => currentAnimators.Count > 0;
    
    /// <summary>
    /// 获取当前场景的所有动画器
    /// </summary>
    public List<Animator> GetCurrentAnimators()
    {
        return currentAnimators;
    }
    
    /// <summary>
    /// 获取当前场景的第一个动画器（向后兼容）
    /// </summary>
    public Animator GetCurrentAnimator()
    {
        return currentAnimators.Count > 0 ? currentAnimators[0] : null;
    }
    
    /// <summary>
    /// 初始化动画管理器
    /// </summary>
    public void Initialize(NetworkPlayer player)
    {
        networkPlayer = player;
        
        // 检测当前场景类型
        DetectCurrentScene();
        
        // 根据场景类型获取动画引用
        if (isInCharacterScene)
        {
            InitializeCharacterSceneAnimator();
            OnInitializeComplete();
        }
        else if (isInGameScene)
        {
            // 游戏场景不立即初始化，等待模型加载完成后通过UpdateAnimatorReference获取
            Debug.Log($"[SceneAwareAnimatorManager] 游戏场景模式，等待模型加载完成后更新动画引用");
            hasInitialized = true;
        }
        else
        {
            hasInitialized = true;
            Debug.LogWarning($"[SceneAwareAnimatorManager] 未知场景类型，初始化完成但无动画器");
            OnAnimatorsInitialized?.Invoke(currentAnimators);
        }
    }
    
    /// <summary>
    /// 检测当前场景类型
    /// </summary>
    private void DetectCurrentScene()
    {
        // 检测是否在角色选择场景
        GameObject characterSelectionManager = GameObject.Find("CharacterSelectionManager");
        if (characterSelectionManager != null)
        {
            isInCharacterScene = true;
            isInGameScene = false;
            return;
        }
        
        // 检测是否在游戏场景
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager != null)
        {
            isInGameScene = true;
            isInCharacterScene = false;
            return;
        }
        
        // 默认认为是游戏场景
        isInGameScene = true;
        isInCharacterScene = false;
        
        Debug.LogWarning($"[SceneAwareAnimatorManager] 无法明确检测场景类型，默认使用游戏场景模式");
    }
    
    /// <summary>
    /// 初始化角色选择场景的动画引用
    /// </summary>
    private void InitializeCharacterSceneAnimator()
    {
        currentAnimators.Clear();
        
        // 如果在角色选择场景，直接使用预设的动画引用
        if (characterSelectionAnimator != null)
        {
            currentAnimators.Add(characterSelectionAnimator);
            Debug.Log($"[SceneAwareAnimatorManager] 角色选择场景 - 使用预设动画引用");
        }
        else
        {
            // 尝试从当前对象获取所有动画器
            Animator[] animators = GetComponentsInChildren<Animator>(true);
            currentAnimators.AddRange(animators);
            Debug.Log($"[SceneAwareAnimatorManager] 角色选择场景 - 尝试从子对象获取动画器: {animators.Length} 个");
        }
    }
    
    /// <summary>
    /// 初始化游戏场景的动画引用
    /// </summary>
    private void InitializeGameSceneAnimator()
    {
        StartCoroutine(InitializeGameSceneAnimatorCoroutine());
    }
    
    /// <summary>
    /// 协程方式初始化游戏场景动画引用
    /// </summary>
    private System.Collections.IEnumerator InitializeGameSceneAnimatorCoroutine()
    {
        int maxAttempts = 20;
        int attempts = 0;
        float retryDelay = 0.1f;
        
        Debug.Log($"[SceneAwareAnimatorManager] 开始获取游戏场景动画引用，路径: {gameSceneAnimatorPath}");
        
        while (currentAnimators.Count == 0 && attempts < maxAttempts)
        {
            if (networkPlayer != null)
            {
                List<Animator> foundAnimators = new List<Animator>();
                
                // 首先尝试通过路径获取ModelParent
                Transform modelParent = networkPlayer.transform.Find(gameSceneAnimatorPath);
                if (modelParent != null)
                {
                    Debug.Log($"[SceneAwareAnimatorManager] 找到ModelParent: {gameSceneAnimatorPath}");
                    
                    // 从ModelParent的子对象中查找所有Animator
                    Animator[] modelAnimators = modelParent.GetComponentsInChildren<Animator>(true);
                    Debug.Log($"[SceneAwareAnimatorManager] ModelParent下找到 {modelAnimators.Length} 个动画器组件");
                    
                    // 添加所有找到的动画器
                    foundAnimators.AddRange(modelAnimators);
                    
                    foreach (Animator anim in modelAnimators)
                    {
                        string path = GetTransformPath(anim.transform);
                        Debug.Log($"[SceneAwareAnimatorManager] 发现动画器: {path}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[SceneAwareAnimatorManager] 未找到路径 {gameSceneAnimatorPath}，尝试从网络玩家的子对象获取");
                    
                    // 如果通过路径找不到，尝试从网络玩家的所有子对象获取动画器
                    Animator[] allAnimators = networkPlayer.GetComponentsInChildren<Animator>(true);
                    Debug.Log($"[SceneAwareAnimatorManager] 网络玩家下找到 {allAnimators.Length} 个动画器组件");
                    
                    // 添加所有找到的动画器
                    foundAnimators.AddRange(allAnimators);
                    
                    foreach (Animator anim in allAnimators)
                    {
                        string path = GetTransformPath(anim.transform);
                        Debug.Log($"[SceneAwareAnimatorManager] 发现动画器: {path}");
                    }
                }
                
                // 如果找到了动画器，添加到列表中
                if (foundAnimators.Count > 0)
                {
                    currentAnimators = foundAnimators;
                    Debug.Log($"[SceneAwareAnimatorManager] 成功获取 {foundAnimators.Count} 个动画器组件");
                }
            }
            
            attempts++;
            if (currentAnimators.Count == 0)
            {
                Debug.Log($"[SceneAwareAnimatorManager] 第 {attempts}/{maxAttempts} 次尝试获取动画器失败，等待 {retryDelay} 秒后重试");
                yield return new WaitForSeconds(retryDelay);
            }
        }
        
        if (currentAnimators.Count == 0)
        {
            Debug.LogError($"[SceneAwareAnimatorManager] 多次尝试后仍无法获取游戏场景动画引用");
        }
        else
        {
            Debug.Log($"[SceneAwareAnimatorManager] 游戏场景动画引用获取成功，共找到 {currentAnimators.Count} 个动画器");
        }
        
        // 调用初始化完成方法
        OnInitializeComplete();
        yield break;
    }
    
    /// <summary>
    /// 获取Transform的完整路径
    /// </summary>
    private string GetTransformPath(Transform target)
    {
        string path = target.name;
        Transform parent = target.parent;
        
        while (parent != null && parent != transform)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    /// <summary>
    /// 更新动画引用（用于模型加载完成后的重新获取）
    /// </summary>
    public void UpdateAnimatorReference()
    {
        if (!hasInitialized) return;
        
        if (isInGameScene)
        {
            Debug.Log($"[SceneAwareAnimatorManager] 更新游戏场景动画引用");
            
            // 直接从NetworkPlayer的子对象获取所有Animator
            if (networkPlayer != null)
            {
                currentAnimators.Clear();
                
                // 获取NetworkPlayer下的所有Animator组件（包括inactive的）
                Animator[] allAnimators = networkPlayer.GetComponentsInChildren<Animator>(true);
                Debug.Log($"[SceneAwareAnimatorManager] NetworkPlayer下找到 {allAnimators.Length} 个动画器组件");
                
                // 添加所有找到的动画器
                currentAnimators.AddRange(allAnimators);
                
                foreach (Animator anim in allAnimators)
                {
                    string path = GetTransformPath(anim.transform);
                    Debug.Log($"[SceneAwareAnimatorManager] 发现动画器: {path}");
                }
                
                Debug.Log($"[SceneAwareAnimatorManager] 动画引用更新完成，共 {currentAnimators.Count} 个动画器");
                
                // 触发初始化完成事件
                if (currentAnimators.Count > 0)
                {
                    OnAnimatorsInitialized?.Invoke(currentAnimators);
                }
                else
                {
                    Debug.LogWarning($"[SceneAwareAnimatorManager] 更新后仍未找到任何动画器");
                }
            }
            else
            {
                Debug.LogWarning($"[SceneAwareAnimatorManager] NetworkPlayer为空，无法更新动画引用");
            }
        }
    }
    
    /// <summary>
    /// 设置角色选择场景的动画引用
    /// </summary>
    public void SetCharacterSelectionAnimator(Animator animator)
    {
        characterSelectionAnimator = animator;
        if (isInCharacterScene)
        {
            currentAnimators.Clear();
            currentAnimators.Add(animator);
            Debug.Log($"[SceneAwareAnimatorManager] 设置角色选择场景动画引用");
        }
    }
    
    /// <summary>
    /// 设置游戏场景的动画路径
    /// </summary>
    public void SetGameSceneAnimatorPath(string path)
    {
        gameSceneAnimatorPath = path;
        if (isInGameScene)
        {
            UpdateAnimatorReference();
        }
    }
    
    /// <summary>
    /// 初始化完成处理
    /// </summary>
    private void OnInitializeComplete()
    {
        hasInitialized = true;
        Debug.Log($"[SceneAwareAnimatorManager] 初始化完成，当前动画器数量: {currentAnimators.Count}");
        OnAnimatorsInitialized?.Invoke(currentAnimators);
    }
    
    private void OnDestroy()
    {
        Debug.Log($"[SceneAwareAnimatorManager] 销毁，当前场景: {(isInCharacterScene ? "Character选择" : isInGameScene ? "Game游戏" : "未知")}");
    }
}