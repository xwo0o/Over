using UnityEngine;

public class EnemyAnimatorRuntimeDiagnostics : MonoBehaviour
{
    [Header("诊断设置")]
    [SerializeField] private bool enableDiagnostics = true;
    [SerializeField] private float diagnosticInterval = 0.5f;

    private float lastDiagnosticTime;
    private EnemyAIController enemyAI;
    private Animator animator;

    void Start()
    {
        enemyAI = GetComponent<EnemyAIController>();
        animator = GetComponent<Animator>();

        if (enemyAI == null)
        {
            Debug.LogError("[EnemyAnimatorRuntimeDiagnostics] 未找到EnemyAIController组件");
            enabled = false;
            return;
        }

        if (animator == null)
        {
            Debug.LogError("[EnemyAnimatorRuntimeDiagnostics] 未找到Animator组件");
            enabled = false;
            return;
        }

        LogAnimatorControllerInfo();
    }

    void Update()
    {
        if (!enableDiagnostics)
            return;

        if (Time.time - lastDiagnosticTime >= diagnosticInterval)
        {
            lastDiagnosticTime = Time.time;
            DiagnoseAnimatorState();
        }
    }

    void LogAnimatorControllerInfo()
    {
        Debug.Log($"[EnemyAnimatorRuntimeDiagnostics] === 动画控制器信息 ===");
        Debug.Log($"  - RuntimeAnimatorController: {animator.runtimeAnimatorController?.name ?? "未设置"}");
        Debug.Log($"  - Avatar: {animator.avatar?.name ?? "未设置"}");
        Debug.Log($"  - 参数数量: {animator.parameterCount}");

        for (int i = 0; i < animator.parameterCount; i++)
        {
            AnimatorControllerParameter param = animator.GetParameter(i);
            Debug.Log($"    参数 {i}: {param.name} (类型: {param.type})");
        }

        Debug.Log($"[EnemyAnimatorRuntimeDiagnostics] =========================");
    }

    void DiagnoseAnimatorState()
    {
        Debug.Log($"[EnemyAnimatorRuntimeDiagnostics] === 动画状态诊断 ===");
        Debug.Log($"  - 当前状态: {animator.GetCurrentAnimatorStateInfo(0).fullPathHash}");
        Debug.Log($"  - 当前动画名称: {animator.GetCurrentAnimatorClipInfo(0)[0].clip.name}");
        Debug.Log($"  - 动画进度: {animator.GetCurrentAnimatorStateInfo(0).normalizedTime:F2}");

        for (int i = 0; i < animator.parameterCount; i++)
        {
            AnimatorControllerParameter param = animator.GetParameter(i);
            object value = GetParameterValue(param);

            Debug.Log($"  参数 {param.name} ({param.type}): {value}");
        }

        Debug.Log($"[EnemyAnimatorRuntimeDiagnostics] =====================");
    }

    object GetParameterValue(AnimatorControllerParameter param)
    {
        switch (param.type)
        {
            case AnimatorControllerParameterType.Bool:
                return animator.GetBool(param.name);
            case AnimatorControllerParameterType.Float:
                return animator.GetFloat(param.name);
            case AnimatorControllerParameterType.Int:
                return animator.GetInteger(param.name);
            case AnimatorControllerParameterType.Trigger:
                return animator.GetBool(param.name);
            default:
                return "未知类型";
        }
    }

    public void TriggerIsAtkAndDiagnose()
    {
        Debug.Log($"[EnemyAnimatorRuntimeDiagnostics] === 触发IsAtk诊断 ===");

        if (animator == null)
        {
            Debug.LogError("[EnemyAnimatorRuntimeDiagnostics] Animator为空，无法触发");
            return;
        }

        Debug.Log($"  - 触发前IsAtk状态: {animator.GetBool("IsAtk")}");
        Debug.Log($"  - 当前动画: {animator.GetCurrentAnimatorClipInfo(0)[0].clip.name}");

        animator.ResetTrigger("IsAtk");
        animator.SetTrigger("IsAtk");

        Debug.Log($"  - 触发后IsAtk状态: {animator.GetBool("IsAtk")}");
        Debug.Log($"  - 触发后动画: {animator.GetCurrentAnimatorClipInfo(0)[0].clip.name}");

        StartCoroutine(MonitorAnimationTransition());
    }

    System.Collections.IEnumerator MonitorAnimationTransition()
    {
        Debug.Log($"[EnemyAnimatorRuntimeDiagnostics] === 监控动画转换 ===");

        for (int i = 0; i < 20; i++)
        {
            yield return new WaitForSeconds(0.1f);

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"  帧数 {i}: 动画={animator.GetCurrentAnimatorClipInfo(0)[0].clip.name}, 进度={stateInfo.normalizedTime:F2}, IsAtk={animator.GetBool("IsAtk")}");
        }

        Debug.Log($"[EnemyAnimatorRuntimeDiagnostics] =====================");
    }

    public void LogAllTransitions()
    {
        Debug.Log($"[EnemyAnimatorRuntimeDiagnostics] === 所有转换条件 ===");

        if (animator.runtimeAnimatorController is UnityEditor.Animations.AnimatorController)
        {
            UnityEditor.Animations.AnimatorController controller = (UnityEditor.Animations.AnimatorController)animator.runtimeAnimatorController;

            foreach (var layer in controller.layers)
            {
                Debug.Log($"  层: {layer.name}");

                foreach (var state in layer.stateMachine.states)
                {
                    Debug.Log($"    状态: {state.state.name}");

                    foreach (var transition in state.state.transitions)
                    {
                        Debug.Log($"      转换到: {transition.destinationState.name}");

                        foreach (var condition in transition.conditions)
                        {
                            Debug.Log($"        条件: {condition.parameter} {condition.mode} {condition.threshold}");
                        }
                    }
                }
            }
        }

        Debug.Log($"[EnemyAnimatorRuntimeDiagnostics] =====================");
    }
}
