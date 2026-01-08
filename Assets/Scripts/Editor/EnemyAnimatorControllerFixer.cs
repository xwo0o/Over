using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public class EnemyAnimatorControllerFixer : EditorWindow
{
    private AnimatorController animatorController;
    private string controllerPath = "Assets/SazenGames/Skeleton/Art/Demo Animator Controllers/idle.controller";

    [MenuItem("Tools/Enemy/修复Animator Controller攻击动画")]
    public static void ShowWindow()
    {
        GetWindow<EnemyAnimatorControllerFixer>("修复敌人Animator Controller");
    }

    void OnGUI()
    {
        GUILayout.Label("敌人Animator Controller修复工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("此工具将修复敌人攻击动画转换配置，确保攻击动画可以正常播放和重复触发。", MessageType.Info);

        GUILayout.Space(10);

        EditorGUILayout.LabelField("Animator Controller路径:");
        controllerPath = EditorGUILayout.TextField(controllerPath);

        GUILayout.Space(10);

        if (GUILayout.Button("加载Animator Controller"))
        {
            LoadAnimatorController();
        }

        if (animatorController != null)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField($"已加载: {animatorController.name}");

            GUILayout.Space(10);

            if (GUILayout.Button("分析当前配置"))
            {
                AnalyzeCurrentConfiguration();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("修复攻击动画转换", GUILayout.Height(40)))
            {
                FixAttackAnimationTransitions();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请先加载Animator Controller", MessageType.Warning);
        }
    }

    void LoadAnimatorController()
    {
        animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (animatorController == null)
        {
            EditorUtility.DisplayDialog("错误", $"无法加载Animator Controller: {controllerPath}", "确定");
        }
        else
        {
            Debug.Log($"[EnemyAnimatorControllerFixer] 已加载Animator Controller: {animatorController.name}");
        }
    }

    void AnalyzeCurrentConfiguration()
    {
        if (animatorController == null) return;

        Debug.Log("=== 敌人Animator Controller配置分析 ===");

        var parameters = animatorController.parameters;
        Debug.Log($"参数数量: {parameters.Length}");
        foreach (var param in parameters)
        {
            Debug.Log($"  - {param.name}: {param.type}");
        }

        var layers = animatorController.layers;
        Debug.Log($"层级数量: {layers.Length}");

        foreach (var layer in layers)
        {
            Debug.Log($"层级: {layer.name}");
            var stateMachine = layer.stateMachine;

            foreach (var state in stateMachine.states)
            {
                Debug.Log($"  状态: {state.state.name}");
                foreach (var transition in state.state.transitions)
                {
                    string destState = transition.destinationState != null ? transition.destinationState.name : "Exit";
                    Debug.Log($"    转换到 {destState}:");
                    Debug.Log($"      - ExitTime: {transition.exitTime}");
                    Debug.Log($"      - TransitionDuration: {transition.duration}");
                    Debug.Log($"      - HasExitTime: {transition.hasExitTime}");
                    Debug.Log($"      - Conditions: {transition.conditions.Length}");
                    foreach (var condition in transition.conditions)
                    {
                        Debug.Log($"        * {condition.mode} {condition.parameter}");
                    }
                }
            }

            Debug.Log($"AnyState转换数量: {stateMachine.anyStateTransitions.Length}");
            foreach (var transition in stateMachine.anyStateTransitions)
            {
                string destState = transition.destinationState != null ? transition.destinationState.name : "Exit";
                Debug.Log($"  AnyState → {destState}:");
                Debug.Log($"    - ExitTime: {transition.exitTime}");
                Debug.Log($"    - TransitionDuration: {transition.duration}");
                Debug.Log($"    - HasExitTime: {transition.hasExitTime}");
                Debug.Log($"    - Conditions: {transition.conditions.Length}");
                foreach (var condition in transition.conditions)
                {
                    Debug.Log($"      * {condition.mode} {condition.parameter}");
                }
            }
        }

        Debug.Log("=====================================");
    }

    void FixAttackAnimationTransitions()
    {
        if (animatorController == null) return;

        Debug.Log("=== 开始修复攻击动画转换 ===");

        var layers = animatorController.layers;
        var baseLayer = layers[0];
        var stateMachine = baseLayer.stateMachine;

        AnimatorState attackState = null;
        AnimatorState idleState = null;

        foreach (var state in stateMachine.states)
        {
            if (state.state.name.Contains("slash"))
            {
                attackState = state.state;
                Debug.Log($"找到攻击状态: {attackState.name}");
            }
            else if (state.state.name == "anim")
            {
                idleState = state.state;
                Debug.Log($"找到待机状态: {idleState.name}");
            }
        }

        if (attackState == null)
        {
            EditorUtility.DisplayDialog("错误", "未找到攻击动画状态（slash01）", "确定");
            return;
        }

        if (idleState == null)
        {
            EditorUtility.DisplayDialog("错误", "未找到待机动画状态（anim）", "确定");
            return;
        }

        bool hasFix = false;

        foreach (var transition in attackState.transitions.ToList())
        {
            if (transition.isExit)
            {
                Debug.Log($"删除攻击状态到Exit的错误转换");
                attackState.RemoveTransition(transition);
                hasFix = true;
            }
        }

        bool hasIdleTransition = false;
        foreach (var transition in attackState.transitions)
        {
            if (transition.destinationState == idleState)
            {
                hasIdleTransition = true;
                Debug.Log($"找到攻击到待机的转换，开始修复...");

                transition.exitTime = 1.0f;
                transition.duration = 0.1f;
                transition.hasExitTime = true;
                transition.canTransitionToSelf = false;

                Debug.Log($"  - ExitTime: {transition.exitTime}");
                Debug.Log($"  - TransitionDuration: {transition.duration}");
                Debug.Log($"  - HasExitTime: {transition.hasExitTime}");
                hasFix = true;
            }
        }

        if (!hasIdleTransition)
        {
            Debug.Log($"创建攻击到待机的新转换...");
            var transition = attackState.AddTransition(idleState);
            transition.exitTime = 1.0f;
            transition.duration = 0.1f;
            transition.hasExitTime = true;
            transition.canTransitionToSelf = false;
            hasFix = true;
        }

        if (hasFix)
        {
            EditorUtility.SetDirty(animatorController);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("成功", "攻击动画转换已修复！\n\n修复内容：\n1. 删除了攻击到Exit的错误转换\n2. 设置攻击动画完整播放（ExitTime=1.0）\n3. 设置快速转换到待机（Duration=0.1）", "确定");

            Debug.Log("=== 攻击动画转换修复完成 ===");
        }
        else
        {
            EditorUtility.DisplayDialog("提示", "未发现需要修复的问题", "确定");
        }
    }
}
