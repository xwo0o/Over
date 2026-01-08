using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

public class EnemyAnimatorDiagnostics : EditorWindow
{
    private Vector2 scrollPosition;
    private List<EnemyAnalyzerResult> analysisResults = new List<EnemyAnalyzerResult>();

    [MenuItem("Tools/Enemy/动画控制器诊断工具")]
    public static void ShowWindow()
    {
        GetWindow<EnemyAnimatorDiagnostics>("敌人动画诊断");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("敌人动画控制器诊断工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("分析敌人动画控制器"))
        {
            AnalyzeEnemyAnimators();
        }

        EditorGUILayout.Space();

        if (analysisResults.Count > 0)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var result in analysisResults)
            {
                DrawAnalysisResult(result);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    void DrawAnalysisResult(EnemyAnalyzerResult result)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"敌人类型: {result.enemyType}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"预制体路径: {result.prefabPath}");
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Animator组件信息:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"  - Animator存在: {(result.hasAnimator ? "是" : "否")}");
        if (result.hasAnimator)
        {
            EditorGUILayout.LabelField($"  - Animator Controller: {result.animatorControllerName}");
            EditorGUILayout.LabelField($"  - Avatar: {result.avatarName}");
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("动画参数:", EditorStyles.boldLabel);
        if (result.parameters.Count > 0)
        {
            foreach (var param in result.parameters)
            {
                EditorGUILayout.LabelField($"  - {param.name} ({param.type})");
            }
        }
        else
        {
            EditorGUILayout.LabelField("  - 无参数");
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("问题诊断:", EditorStyles.boldLabel);
        if (result.issues.Count > 0)
        {
            foreach (var issue in result.issues)
            {
                EditorGUILayout.HelpBox(issue, MessageType.Error);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("未发现问题", MessageType.Info);
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("建议:", EditorStyles.boldLabel);
        if (result.suggestions.Count > 0)
        {
            foreach (var suggestion in result.suggestions)
            {
                EditorGUILayout.HelpBox(suggestion, MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.LabelField("  - 无建议");
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    void AnalyzeEnemyAnimators()
    {
        analysisResults.Clear();

        string[] enemyPrefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemy" });

        foreach (string guid in enemyPrefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab != null)
            {
                EnemyAnalyzerResult result = AnalyzeEnemyPrefab(prefab, assetPath);
                analysisResults.Add(result);
            }
        }

        Debug.Log($"[EnemyAnimatorDiagnostics] 分析完成，共分析 {analysisResults.Count} 个敌人预制体");
    }

    EnemyAnalyzerResult AnalyzeEnemyPrefab(GameObject prefab, string assetPath)
    {
        EnemyAnalyzerResult result = new EnemyAnalyzerResult();
        result.enemyType = prefab.name;
        result.prefabPath = assetPath;

        Animator animator = prefab.GetComponent<Animator>();
        result.hasAnimator = animator != null;

        if (animator != null)
        {
            if (animator.runtimeAnimatorController != null)
            {
                result.animatorControllerName = animator.runtimeAnimatorController.name;
                AnalyzeAnimatorController(animator.runtimeAnimatorController, result);
            }
            else
            {
                result.issues.Add("Animator Controller未设置");
            }

            if (animator.avatar != null)
            {
                result.avatarName = animator.avatar.name;
            }
        }
        else
        {
            result.issues.Add("缺少Animator组件");
        }

        return result;
    }

    void AnalyzeAnimatorController(RuntimeAnimatorController controller, EnemyAnalyzerResult result)
    {
        if (controller is AnimatorController)
        {
            AnimatorController animatorController = (AnimatorController)controller;

            foreach (var param in animatorController.parameters)
            {
                result.parameters.Add(new AnimatorParameterInfo
                {
                    name = param.name,
                    type = param.type.ToString()
                });
            }

            CheckIsAtkParameter(result);
            CheckAnimationStates(animatorController, result);
        }
    }

    void CheckIsAtkParameter(EnemyAnalyzerResult result)
    {
        bool hasIsAtkTrigger = result.parameters.Any(p => p.name == "IsAtk" && p.type == "Trigger");
        bool hasIsWorkBool = result.parameters.Any(p => p.name == "IsWork" && p.type == "Bool");
        bool hasIsRunBool = result.parameters.Any(p => p.name == "IsRun" && p.type == "Bool");
        bool hasIsDieTrigger = result.parameters.Any(p => p.name == "IsDie" && p.type == "Trigger");

        if (!hasIsAtkTrigger)
        {
            result.issues.Add("缺少IsAtk触发器参数");
            result.suggestions.Add("添加IsAtk触发器参数，类型为Trigger");
        }
        else
        {
            result.suggestions.Add("IsAtk触发器参数存在");
        }

        if (!hasIsWorkBool)
        {
            result.issues.Add("缺少IsWork布尔参数");
            result.suggestions.Add("添加IsWork布尔参数，类型为Bool");
        }

        if (!hasIsRunBool)
        {
            result.issues.Add("缺少IsRun布尔参数");
            result.suggestions.Add("添加IsRun布尔参数，类型为Bool");
        }

        if (!hasIsDieTrigger)
        {
            result.issues.Add("缺少IsDie触发器参数");
            result.suggestions.Add("添加IsDie触发器参数，类型为Trigger");
        }
    }

    void CheckAnimationStates(AnimatorController controller, EnemyAnalyzerResult result)
    {
        foreach (var layer in controller.layers)
        {
            foreach (var state in layer.stateMachine.states)
            {
                string stateName = state.state.name;

                if (stateName.ToLower().Contains("attack") || stateName.ToLower().Contains("atk"))
                {
                    result.suggestions.Add($"发现攻击动画状态: {stateName}");

                    if (state.state.motion != null)
                    {
                        float animationLength = state.state.motion.averageDuration;
                        result.suggestions.Add($"  - 攻击动画时长: {animationLength:F2}秒");

                        if (animationLength > 3f)
                        {
                            result.issues.Add($"攻击动画时长({animationLength:F2}秒)超过冷却时间(3秒)，可能导致动画重叠");
                        }
                    }
                }
            }
        }
    }

    class EnemyAnalyzerResult
    {
        public string enemyType;
        public string prefabPath;
        public bool hasAnimator;
        public string animatorControllerName;
        public string avatarName;
        public List<AnimatorParameterInfo> parameters = new List<AnimatorParameterInfo>();
        public List<string> issues = new List<string>();
        public List<string> suggestions = new List<string>();
    }

    class AnimatorParameterInfo
    {
        public string name;
        public string type;
    }
}
