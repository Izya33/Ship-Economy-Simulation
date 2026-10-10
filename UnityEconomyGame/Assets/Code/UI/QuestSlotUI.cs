using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI stateText;

    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;

    private Quest currentQuest;
    private QuestSlotAction currentAction;
    private UIManager uiManager;

    public void Setup(Quest quest, QuestSlotAction action, UIManager manager)
    {
        currentQuest = quest;
        currentAction = action;
        uiManager = manager;

        gameObject.SetActive(true);

        questNameText.text = quest.data.questName;
        descriptionText.text =$"{quest.data.description}\n Required: {quest.data.requiredAmount} {quest.data.itemType}";
        rewardText.text = $"Reward: ${quest.data.moneyReward:F2}";
        stateText.text = quest.state.ToString();

        if (currentAction == QuestSlotAction.Accept)
            actionButtonText.text = "Accept";
        else
            actionButtonText.text = "Complete";

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnActionClicked);
    }

    public void Clear()
    {
        currentQuest = null;
        uiManager = null;

        actionButton.onClick.RemoveAllListeners();
        gameObject.SetActive(false);
    }

    private void OnActionClicked()
    {
        if (currentQuest == null || uiManager == null) return;

        if (currentAction == QuestSlotAction.Accept)
        {
            uiManager.AcceptQuest(currentQuest);
        }
        else if (currentAction == QuestSlotAction.Complete)
        {
            uiManager.CompleteQuest(currentQuest);
        }
    }
}