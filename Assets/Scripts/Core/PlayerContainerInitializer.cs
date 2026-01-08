using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PlayerContainerInitializer : MonoBehaviour
{
    private void Awake()
    {
        EnsureCharacterModelManager();
    }

    private void EnsureCharacterModelManager()
    {
        CharacterModelManager modelManager = GetComponent<CharacterModelManager>();
        if (modelManager == null)
        {
            modelManager = gameObject.AddComponent<CharacterModelManager>();
            Debug.Log($"[PlayerContainerInitializer] 已为 {gameObject.name} 添加CharacterModelManager组件");
        }
    }
}
