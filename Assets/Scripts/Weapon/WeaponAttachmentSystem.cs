using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

/// <summary>
/// 武器挂载信息 - 存储单个武器的挂载数据
/// </summary>
[System.Serializable]
public class WeaponMountInfo
{
    [Tooltip("武器实例")]
    public GameObject weaponInstance;
    [Tooltip("资源加载句柄")]
    public AsyncOperationHandle<GameObject> handle;
    [Tooltip("挂载的骨骼名称")]
    public string boneName;
}

/// <summary>
/// 武器挂载系统 - 将武器模型挂载到角色指定骨骼上，支持单手和双手武器
/// </summary>
public class WeaponAttachmentSystem : MonoBehaviour
{
    [Header("偏移设置")]
    [Tooltip("右手武器相对于骨骼的本地位置偏移")]
    public Vector3 rightHandPositionOffset = Vector3.zero;
    [Tooltip("右手武器相对于骨骼的本地旋转偏移（欧拉角）")]
    public Vector3 rightHandRotationOffset = Vector3.zero;
    [Tooltip("左手武器相对于骨骼的本地位置偏移")]
    public Vector3 leftHandPositionOffset = Vector3.zero;
    [Tooltip("左手武器相对于骨骼的本地旋转偏移（欧拉角）")]
    public Vector3 leftHandRotationOffset = Vector3.zero;
    [Tooltip("武器缩放")]
    public Vector3 weaponScale = Vector3.one;
    
    [Header("可选设置")]
    [Tooltip("是否在切换武器时播放音效")]
    public bool playSwitchSound = true;
    [Tooltip("切换武器音效")]
    public AudioClip switchSound;
    
    // 当前挂载的武器列表（支持多武器，如双手武器）
    private List<WeaponMountInfo> mountedWeapons = new List<WeaponMountInfo>();
    // 当前装备的武器ID
    private int currentWeaponId = -1;
    // 当前武器数据
    private WeaponData currentWeaponData;
    // 音频源组件
    private AudioSource audioSource;
    
    // 事件
    public System.Action<int> OnWeaponEquipped;  // 武器装备事件
    public System.Action OnWeaponUnequipped;     // 武器卸下事件
    
    void Awake()
    {
        // 获取或添加AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && playSwitchSound)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    /// <summary>
    /// 延迟查找骨骼并挂载武器
    /// </summary>
    public void DelayedFindBoneAndAttach(string boneName, string modelKey)
    {
        StartCoroutine(DelayedFindBoneCoroutine(boneName, modelKey));
    }
    
    System.Collections.IEnumerator DelayedFindBoneCoroutine(string boneName, string modelKey)
    {
        // 关键修复：添加详细的调试信息
        Debug.Log($"[WeaponAttachmentSystem] DelayedFindBoneCoroutine开始: boneName={boneName}, modelKey={modelKey}, 当前角色={gameObject.name}, instanceID={GetInstanceID()}");
        
        // 等待一帧，确保模型已加载
        yield return null;
        
        // 查找骨骼，带重试机制
        Transform boneTransform = null;
        int retryCount = 0;
        const int maxRetries = 10;
        
        while (boneTransform == null && retryCount < maxRetries)
        {
            boneTransform = FindTargetBone(boneName);
            if (boneTransform == null)
            {
                retryCount++;
                Debug.Log($"[WeaponAttachmentSystem] 第{retryCount}次查找骨骼失败，等待重试: {boneName}");
                yield return new WaitForSeconds(0.2f);
            }
        }
        
        if (boneTransform == null)
        {
            Debug.LogWarning($"[WeaponAttachmentSystem] 经过{maxRetries}次尝试仍未能找到骨骼: {boneName}，放弃挂载");
            yield break;
        }
        
        // 关键修复：验证骨骼是否属于当前角色
        if (!IsTransformInHierarchy(boneTransform, transform))
        {
            Debug.LogError($"[WeaponAttachmentSystem] 严重错误：找到的骨骼 {boneTransform.name} 不属于当前角色 {gameObject.name}！");
            yield break;
        }
        
        Debug.Log($"[WeaponAttachmentSystem] 成功找到骨骼: {boneName} (尝试{retryCount + 1}次), 骨骼路径={GetTransformPath(boneTransform)}");
        
        // 加载武器模型
        Debug.Log($"[WeaponAttachmentSystem] 开始加载武器模型: {modelKey}");
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(modelKey);
        yield return handle;
        
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[WeaponAttachmentSystem] 武器模型加载失败: {handle.OperationException}");
            yield break;
        }
        
        // 关键修复：再次验证骨骼是否有效
        if (boneTransform == null)
        {
            Debug.LogError("[WeaponAttachmentSystem] 骨骼在加载模型期间变为null！");
            yield break;
        }
        
        // 实例化武器
        GameObject weaponInstance = Instantiate(handle.Result, boneTransform);
        
        // 关键修复：验证武器实例是否正确挂载
        if (weaponInstance.transform.parent != boneTransform)
        {
            Debug.LogError($"[WeaponAttachmentSystem] 武器实例挂载错误！期望父级: {boneTransform.name}, 实际父级: {weaponInstance.transform.parent?.name}");
        }
        else
        {
            Debug.Log($"[WeaponAttachmentSystem] 武器实例正确挂载到: {boneTransform.name}");
        }
        
        // 应用位置偏移（使用右手偏移作为默认偏移）
        weaponInstance.transform.localPosition = rightHandPositionOffset;
        
        // 保持原始旋转和缩放，不做矫正
        weaponInstance.transform.localScale = weaponScale;
        
        // 失活资源上的触发器
        DeactivateWeaponTriggers(weaponInstance);
        
        // 存储武器信息
        WeaponMountInfo mountInfo = new WeaponMountInfo
        {
            weaponInstance = weaponInstance,
            handle = handle,
            boneName = boneName
        };
        mountedWeapons.Add(mountInfo);
        
        Debug.Log($"[WeaponAttachmentSystem] 武器挂载成功: {modelKey} -> {boneName} 到 {gameObject.name}");
        
        // 触发事件
        if (mountedWeapons.Count == 1)
        {
            if (playSwitchSound && audioSource != null && switchSound != null)
            {
                audioSource.PlayOneShot(switchSound);
            }
            OnWeaponEquipped?.Invoke(currentWeaponId);
        }
    }
    
    /// <summary>
    /// 装备武器（根据武器ID从数据库加载）
    /// </summary>
    /// <param name="weaponId">武器ID</param>
    public void EquipWeapon(int weaponId)
    {
        // 关键修复：添加详细的调试信息
        Debug.Log($"[WeaponAttachmentSystem] EquipWeapon被调用: weaponId={weaponId}, currentWeaponId={currentWeaponId}, gameObject={gameObject.name}, instanceID={GetInstanceID()}");
        
        if (weaponId == currentWeaponId && mountedWeapons.Count > 0)
        {
            Debug.Log($"[WeaponAttachmentSystem] 武器 {weaponId} 已经装备，跳过");
            return;
        }

        // 先卸下当前武器
        UnequipWeapon();

        // 从数据库获取武器数据
        if (WeaponDatabase.Instance == null)
        {
            Debug.LogError("[WeaponAttachmentSystem] WeaponDatabase.Instance 为空");
            return;
        }

        currentWeaponData = WeaponDatabase.Instance.GetWeapon(weaponId);
        if (currentWeaponData == null)
        {
            Debug.LogError($"[WeaponAttachmentSystem] 未找到武器ID: {weaponId}");
            return;
        }

        currentWeaponId = weaponId;

        // 加载并挂载武器
        LoadAndAttachWeapon(currentWeaponData);

        Debug.Log($"[WeaponAttachmentSystem] 开始装备武器: {currentWeaponData.weaponName} (ID: {weaponId}) 到 {gameObject.name}");
    }
    
    /// <summary>
    /// 加载并挂载武器
    /// </summary>
    void LoadAndAttachWeapon(WeaponData weaponData)
    {
        if (weaponData.boneNames == null || weaponData.boneNames.Length == 0)
        {
            Debug.LogError($"[WeaponAttachmentSystem] 武器 {weaponData.weaponName} 没有配置骨骼名称");
            return;
        }

        // 获取所有模型地址
        string[] modelKeys = weaponData.GetAllModelKeys();
        if (modelKeys.Length == 0)
        {
            Debug.LogError($"[WeaponAttachmentSystem] 武器 {weaponData.weaponName} 没有配置模型地址");
            return;
        }

        // 为每个骨骼挂载武器（延迟2秒执行，等待角色模型完全加载）
        for (int i = 0; i < weaponData.boneNames.Length; i++)
        {
            string boneName = weaponData.boneNames[i];
            string modelKey = modelKeys[Mathf.Min(i, modelKeys.Length - 1)];
            
            Debug.Log($"[WeaponAttachmentSystem] 准备挂载武器: {modelKey} -> {boneName}（延迟2秒）");
            DelayedFindBoneAndAttach(boneName, modelKey);
        }
    }
    
    /// <summary>
    /// 直接挂载武器到指定骨骼（旧版接口，用于预览系统）
    /// </summary>
    /// <param name="addressableKey">武器的Addressable Key</param>
    /// <param name="targetBoneName">目标骨骼名称</param>
    public void AttachWeapon(string addressableKey, string targetBoneName = "Prop_R")
    {
        if (string.IsNullOrEmpty(addressableKey))
        {
            Debug.LogWarning("[WeaponAttachmentSystem] addressableKey为空");
            return;
        }
        
        // 如果当前已经挂载了相同的武器，则不重复加载
        if (mountedWeapons.Count > 0)
        {
            // 简单处理：先卸下再挂载
            UnequipWeapon();
        }
        
        // 使用延迟查找骨骼的方式挂载武器
        DelayedFindBoneAndAttach(targetBoneName, addressableKey);
        
        Debug.Log($"[WeaponAttachmentSystem] 开始加载武器: {addressableKey} -> {targetBoneName}");
    }
    
    /// <summary>
    /// 失活武器上的所有触发器（Collider），避免与角色手部发生碰撞
    /// </summary>
    /// <param name="weapon">武器实例</param>
    void DeactivateWeaponTriggers(GameObject weapon)
    {
        if (weapon == null) return;
        
        // 获取武器上的所有Collider组件
        Collider[] colliders = weapon.GetComponentsInChildren<Collider>(true);
        int deactivatedCount = 0;
        
        foreach (Collider collider in colliders)
        {
            // 只处理触发器（Trigger）
            if (collider.isTrigger)
            {
                collider.enabled = false;
                deactivatedCount++;
                Debug.Log($"[WeaponAttachmentSystem] 失活触发器: {collider.name} ({collider.GetType().Name})");
            }
        }
        
        if (deactivatedCount > 0)
        {
            Debug.Log($"[WeaponAttachmentSystem] 共失活 {deactivatedCount} 个触发器");
        }
    }
    
    /// <summary>
    /// 卸下当前武器
    /// </summary>
    public void UnequipWeapon()
    {
        // 销毁所有武器实例
        foreach (var mountInfo in mountedWeapons)
        {
            if (mountInfo.weaponInstance != null)
            {
                Destroy(mountInfo.weaponInstance);
                Debug.Log($"[WeaponAttachmentSystem] 武器已卸下: {mountInfo.boneName}");
            }
            
            // 释放Addressable资源
            if (mountInfo.handle.IsValid())
            {
                Addressables.Release(mountInfo.handle);
            }
        }
        
        mountedWeapons.Clear();
        currentWeaponId = -1;
        currentWeaponData = null;
        
        OnWeaponUnequipped?.Invoke();
        Debug.Log("[WeaponAttachmentSystem] 所有武器已卸下");
    }
    
    /// <summary>
    /// 查找目标骨骼变换
    /// 关键修复：确保只查找当前角色的骨骼，避免找到其他角色的骨骼
    /// </summary>
    Transform FindTargetBone(string boneName)
    {
        Debug.Log($"[WeaponAttachmentSystem] 开始查找骨骼: {boneName}, 当前角色={gameObject.name}");
        
        // 关键修复：首先验证transform是否属于当前GameObject
        if (transform == null)
        {
            Debug.LogError("[WeaponAttachmentSystem] transform为null！");
            return null;
        }
        
        // 方法1: 在model子物体下递归查找（你的角色结构）
        Transform modelTransform = transform.Find("model");
        if (modelTransform != null)
        {
            Debug.Log($"[WeaponAttachmentSystem] 找到model，开始递归查找骨骼: {boneName}");
            Transform bone = FindDeepChild(modelTransform, boneName);
            if (bone != null)
            {
                // 关键修复：验证找到的骨骼是否属于当前角色
                if (IsTransformInHierarchy(bone, transform))
                {
                    Debug.Log($"[WeaponAttachmentSystem] 在model下找到骨骼: {bone.name}, 路径={GetTransformPath(bone)}");
                    return bone;
                }
                else
                {
                    Debug.LogError($"[WeaponAttachmentSystem] 找到的骨骼 {bone.name} 不属于当前角色！");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[WeaponAttachmentSystem] 未找到model子物体");
        }
        
        // 方法2: 在ModelParent下递归查找（兼容旧结构）
        Transform modelParent = transform.Find("ModelParent");
        if (modelParent != null)
        {
            Debug.Log($"[WeaponAttachmentSystem] 找到ModelParent，开始递归查找骨骼: {boneName}");
            Transform bone = FindDeepChild(modelParent, boneName);
            if (bone != null)
            {
                // 关键修复：验证找到的骨骼是否属于当前角色
                if (IsTransformInHierarchy(bone, transform))
                {
                    Debug.Log($"[WeaponAttachmentSystem] 在ModelParent下找到骨骼: {bone.name}, 路径={GetTransformPath(bone)}");
                    return bone;
                }
                else
                {
                    Debug.LogError($"[WeaponAttachmentSystem] 找到的骨骼 {bone.name} 不属于当前角色！");
                }
            }
        }
        
        // 方法3: 在整个角色层级中查找
        if (!string.IsNullOrEmpty(boneName))
        {
            Debug.Log($"[WeaponAttachmentSystem] 在整个角色层级中查找骨骼: {boneName}");
            
            // 从当前物体开始递归查找
            Transform bone = FindDeepChild(transform, boneName);
            if (bone != null)
            {
                // 关键修复：验证找到的骨骼是否属于当前角色
                if (IsTransformInHierarchy(bone, transform))
                {
                    Debug.Log($"[WeaponAttachmentSystem] 在角色层级中找到骨骼: {bone.name}, 路径={GetTransformPath(bone)}");
                    return bone;
                }
                else
                {
                    Debug.LogError($"[WeaponAttachmentSystem] 找到的骨骼 {bone.name} 不属于当前角色！");
                }
            }
        }
        
        Debug.LogError($"[WeaponAttachmentSystem] 所有查找方法都失败，未找到骨骼: {boneName}");
        return null;
    }
    
    /// <summary>
    /// 关键修复：验证transform是否属于指定的层级结构
    /// </summary>
    bool IsTransformInHierarchy(Transform target, Transform parent)
    {
        if (target == null || parent == null) return false;
        
        Transform current = target;
        while (current != null)
        {
            if (current == parent) return true;
            current = current.parent;
        }
        return false;
    }
    
    /// <summary>
    /// 递归查找子物体
    /// </summary>
    Transform FindDeepChild(Transform parent, string name, int depth = 0)
    {
        // 限制递归深度，避免过深的递归
        if (depth > 20)
        {
            Debug.LogWarning($"[WeaponAttachmentSystem] FindDeepChild 达到最大递归深度20，停止查找");
            return null;
        }
        
        Transform result = parent.Find(name);
        if (result != null)
        {
            Debug.Log($"[WeaponAttachmentSystem] FindDeepChild 在深度{depth}找到 {name}: {GetTransformPath(result)}");
            return result;
        }
        
        foreach (Transform child in parent)
        {
            result = FindDeepChild(child, name, depth + 1);
            if (result != null) return result;
        }
        return null;
    }
    
    /// <summary>
    /// 获取Transform的完整路径
    /// </summary>
    string GetTransformPath(Transform target)
    {
        string path = target.name;
        Transform parent = target.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    /// <summary>
    /// 更新武器偏移（可在运行时调整）
    /// </summary>
    public void UpdateOffset(Vector3 newPosition, Vector3 newRotation, bool isLeftHand = false)
    {
        if (isLeftHand)
        {
            leftHandPositionOffset = newPosition;
            leftHandRotationOffset = newRotation;
        }
        else
        {
            rightHandPositionOffset = newPosition;
            rightHandRotationOffset = newRotation;
        }
        
        // 更新已挂载的武器
        foreach (var mountInfo in mountedWeapons)
        {
            if (mountInfo.weaponInstance != null)
            {
                bool weaponIsLeftHand = mountInfo.boneName.Contains("_L") || mountInfo.boneName.ToLower().Contains("left");
                if (weaponIsLeftHand == isLeftHand)
                {
                    mountInfo.weaponInstance.transform.localPosition = newPosition;
                    mountInfo.weaponInstance.transform.localRotation = Quaternion.Euler(newRotation);
                }
            }
        }
    }
    
    /// <summary>
    /// 获取当前挂载的主武器实例
    /// </summary>
    public GameObject GetCurrentWeapon()
    {
        if (mountedWeapons.Count > 0)
        {
            return mountedWeapons[0].weaponInstance;
        }
        return null;
    }
    
    /// <summary>
    /// 获取所有挂载的武器实例
    /// </summary>
    public List<GameObject> GetAllMountedWeapons()
    {
        List<GameObject> weapons = new List<GameObject>();
        foreach (var mountInfo in mountedWeapons)
        {
            if (mountInfo.weaponInstance != null)
            {
                weapons.Add(mountInfo.weaponInstance);
            }
        }
        return weapons;
    }
    
    /// <summary>
    /// 获取当前装备的武器ID
    /// </summary>
    public int GetCurrentWeaponId()
    {
        return currentWeaponId;
    }
    
    /// <summary>
    /// 获取当前武器数据
    /// </summary>
    public WeaponData GetCurrentWeaponData()
    {
        return currentWeaponData;
    }
    
    /// <summary>
    /// 检查是否有武器挂载
    /// </summary>
    public bool HasWeaponAttached()
    {
        return mountedWeapons.Count > 0;
    }
    
    /// <summary>
    /// 检查是否装备了指定ID的武器
    /// </summary>
    public bool IsWeaponEquipped(int weaponId)
    {
        return currentWeaponId == weaponId;
    }
    
    void OnDestroy()
    {
        UnequipWeapon();
    }
}
