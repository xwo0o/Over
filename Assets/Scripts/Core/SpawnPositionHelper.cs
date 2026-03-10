using UnityEngine;

namespace Core
{
    /// <summary>
    /// 生成位置辅助类，用于检测和避开特定区域和地形
    /// </summary>
    public static class SpawnPositionHelper
    {
        /// <summary>
        /// 检查位置是否在yingdi区域内
        /// </summary>
        /// <param name="position">要检查的位置</param>
        /// <param name="avoidDistance">避开yingdi的距离</param>
        /// <returns>如果位置在yingdi区域内返回true，否则返回false</returns>
        public static bool IsInYingdiArea(Vector3 position, float avoidDistance = 5f)
        {
            GameObject[] yingdiObjects = GameObject.FindGameObjectsWithTag("yingdi");
            
            foreach (GameObject yingdi in yingdiObjects)
            {
                if (yingdi == null) continue;
                
                float dist = Vector3.Distance(position, yingdi.transform.position);
                if (dist < avoidDistance)
                {
                    Debug.Log($"[SpawnPositionHelper] 位置 {position} 在yingdi区域内，距离: {dist}, 避开距离: {avoidDistance}");
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// 检查位置是否有效（不在yingdi区域内）
        /// </summary>
        /// <param name="position">要检查的位置</param>
        /// <param name="avoidYingdiDistance">避开yingdi的距离</param>
        /// <returns>如果位置有效返回true，否则返回false</returns>
        public static bool IsValidSpawnPosition(Vector3 position, float avoidYingdiDistance = 5f)
        {
            if (IsInYingdiArea(position, avoidYingdiDistance))
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// 获取一个随机的有效生成位置
        /// </summary>
        /// <param name="center">生成中心点</param>
        /// <param name="range">生成范围半径</param>
        /// <param name="avoidYingdiDistance">避开yingdi的距离</param>
        /// <param name="maxAttempts">最大尝试次数</param>
        /// <returns>有效的生成位置，如果找不到返回Vector3.zero</returns>
        public static Vector3 GetRandomValidPosition(Vector3 center, float range, float avoidYingdiDistance = 5f, int maxAttempts = 20)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 circle = Random.insideUnitCircle * range;
                Vector3 pos = center + new Vector3(circle.x, 0f, circle.y);
                
                if (IsValidSpawnPosition(pos, avoidYingdiDistance))
                {
                    return pos;
                }
            }
            
            Debug.LogWarning($"[SpawnPositionHelper] 在{maxAttempts}次尝试后未找到有效位置，中心点：{center}，范围：{range}");
            return Vector3.zero;
        }

        /// <summary>
        /// 获取一个随机的有效生成位置（以原点为中心）
        /// </summary>
        /// <param name="range">生成范围半径</param>
        /// <param name="avoidYingdiDistance">避开yingdi的距离</param>
        /// <param name="maxAttempts">最大尝试次数</param>
        /// <returns>有效的生成位置，如果找不到返回Vector3.zero</returns>
        public static Vector3 GetRandomValidPosition(float range, float avoidYingdiDistance = 5f, int maxAttempts = 20)
        {
            return GetRandomValidPosition(Vector3.zero, range, avoidYingdiDistance, maxAttempts);
        }

        /// <summary>
        /// 在Scene标签的地形上随机生成位置
        /// </summary>
        /// <param name="avoidYingdiDistance">避开yingdi的距离</param>
        /// <param name="maxAttempts">最大尝试次数</param>
        /// <returns>有效的生成位置，如果找不到返回Vector3.zero</returns>
        public static Vector3 GetRandomPositionOnSceneTerrain(float avoidYingdiDistance = 30f, int maxAttempts = 50)
        {
            GameObject[] sceneObjects = GameObject.FindGameObjectsWithTag("Scene");
            
            if (sceneObjects == null || sceneObjects.Length == 0)
            {
                Debug.LogWarning("[SpawnPositionHelper] 未找到Scene标签的对象，使用默认生成逻辑");
                return GetRandomValidPosition(100f, avoidYingdiDistance, maxAttempts);
            }
            
            Debug.Log($"[SpawnPositionHelper] 找到 {sceneObjects.Length} 个Scene标签的对象");
            
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 随机选择一个Scene对象
                GameObject sceneObj = sceneObjects[Random.Range(0, sceneObjects.Length)];
                
                if (sceneObj == null)
                    continue;
                
                // 获取场景对象的碰撞体
                Collider sceneCollider = sceneObj.GetComponent<Collider>();
                if (sceneCollider == null)
                    continue;
                
                Bounds bounds = sceneCollider.bounds;
                
                // 在边界内随机生成位置
                Vector3 randomPos = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    bounds.max.y + 0.1f,
                    Random.Range(bounds.min.z, bounds.max.z)
                );
                
                // 检查是否在yingdi区域内
                if (!IsInYingdiArea(randomPos, avoidYingdiDistance))
                {
                    Debug.Log($"[SpawnPositionHelper] 第 {attempt + 1} 次尝试成功找到有效位置: {randomPos}");
                    return randomPos;
                }
            }
            
            Debug.LogWarning($"[SpawnPositionHelper] 在{maxAttempts}次尝试后未找到Scene地形上的有效位置");
            return Vector3.zero;
        }
    }
}