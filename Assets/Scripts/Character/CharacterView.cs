using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 角色选择视图 - 负责显示角色信息
/// </summary>
public class CharacterView : MVCView
{
    [Header("UI组件")]
    public Text characterNameText;
    public Text characterDescriptionText;
    public Text characterStatsText;
    public Text controlHintText;
    public Button confirmSelectionButton;
    public GameObject characterSelectionPanel;
    
    private CharacterDataModel currentCharacterData;

    public override void BindModel(object model)
    {
        if (model is CharacterDataModel data)
        {
            currentCharacterData = data;
            UpdateView();
        }
        else
        {
            Debug.LogWarning("[CharacterView] 绑定的模型类型不正确");
        }
    }

    public override void UpdateView()
    {
        if (currentCharacterData != null)
        {
            if (characterNameText != null)
                characterNameText.text = currentCharacterData.name;
                
            if (characterDescriptionText != null)
                characterDescriptionText.text = currentCharacterData.description;
                
            if (characterStatsText != null)
                characterStatsText.text = $"生命值: {currentCharacterData.health}\n" +
                                        $"速度: {currentCharacterData.speed}\n" +
                                        $"攻击力: {currentCharacterData.attack}\n" +
                                        $"特殊能力: {currentCharacterData.specialAbility}";
        }
        else
        {
            if (characterNameText != null)
                characterNameText.text = "未选择角色";
                
            if (characterDescriptionText != null)
                characterDescriptionText.text = "请选择一个角色";
                
            if (characterStatsText != null)
                characterStatsText.text = "无角色数据";
        }
    }
    
    /// <summary>
    /// 设置选择确认按钮的交互状态
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public void SetConfirmButtonInteractable(bool interactable)
    {
        if (confirmSelectionButton != null)
        {
            confirmSelectionButton.interactable = interactable;
        }
    }
    
    /// <summary>
    /// 设置控制提示文本
    /// </summary>
    /// <param name="text">提示文本</param>
    public void SetControlHint(string text)
    {
        if (controlHintText != null)
        {
            controlHintText.text = text;
        }
    }
    
    /// <summary>
    /// 设置选择面板的激活状态
    /// </summary>
    /// <param name="active">是否激活</param>
    public void SetSelectionPanelActive(bool active)
    {
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.SetActive(active);
        }
    }
}