using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    [SerializeField] private List<QuestData> availableQuestData = new();

    private readonly List<Quest> availableQuests = new();
    private readonly List<Quest> activeQuests = new();
    private readonly List<Quest> completedQuests = new();

    public IReadOnlyList<Quest> AvailableQuests => availableQuests;
    public IReadOnlyList<Quest> ActiveQuests => activeQuests;
    public IReadOnlyList<Quest> CompletedQuests => completedQuests;

    private int questAmount;

    private void Start()
    {
        CreateQuests();
    }

    private void CreateQuests()
    {
        availableQuests.Clear();

        foreach (QuestData questData in availableQuestData)
        {
            if (questData == null) continue;

            availableQuests.Add(new Quest(questData));
        }
        questAmount = availableQuests.Count;
    }

    public bool AcceptQuest(Quest quest)
    {
        if (quest == null) return false;
        if (!availableQuests.Contains(quest)) return false;

        availableQuests.Remove(quest);

        quest.state = QuestState.Active;
        activeQuests.Add(quest);

        Debug.Log($"Quest accepted: {quest.data.questName}");

        return true;
    }

    public bool CancelQuest(Quest quest)
    {
        if (quest == null) return false;
        if (!activeQuests.Contains(quest)) return false;

        activeQuests.Remove(quest);

        quest.state = QuestState.Available;
        availableQuests.Add(quest);

        Debug.Log($"Quest cancelled: {quest.data.questName}");

        return true;
    }

    public void CompleteQuest(Quest quest)
    {
        if (quest == null) return;
        if (!activeQuests.Contains(quest)) return;

        activeQuests.Remove(quest);

        quest.state = QuestState.Completed;
        completedQuests.Add(quest);

        if(completedQuests.Count == questAmount)
            CreateQuests();
        

        Debug.Log($"Quest completed: {quest.data.questName}");
    }

    public List<Quest> GetAvailableQuests(string traderId)
    {
        List<Quest> result = new();

        foreach (Quest quest in availableQuests)
        {
            if (quest == null || quest.data == null) continue;
            if (quest.data.giverTraderId != traderId) continue;

            result.Add(quest);
        }

        return result;
    }

    public List<Quest> GetCompletableQuests(string traderId)
    {
        List<Quest> result = new();

        foreach (Quest quest in activeQuests)
        {
            if (quest == null || quest.data == null) continue;
            if (quest.data.questType != QuestType.Delivery) continue;
            if (quest.data.destinationTraderId != traderId) continue;

            result.Add(quest);
        }

        return result;
    }
    public bool TryCompleteDelivery(Player player, TraderData trader, Quest quest)
    {
        if (player == null || trader == null || quest == null) return false;
        if (!activeQuests.Contains(quest)) return false;
        if (quest.data.questType != QuestType.Delivery) return false;
        if (quest.data.destinationTraderId != trader.traderId) return false;

        ItemType itemType = quest.data.itemType;
        int amount = quest.data.requiredAmount;

        if (!player.HasItem(itemType, amount))
        {
            Debug.Log($"Not enough {itemType}. Required: {amount}");
            return false;
        }

        if (!player.RemoveItem(itemType, amount))
        {
            Debug.Log("Could not remove quest items.");
            return false;
        }

        player.money += quest.data.moneyReward;
        player.AddExperience(quest.data.experienceReward);

        CompleteQuest(quest);

        Debug.Log($"Delivered {amount} {itemType} to {trader.name}. Reward: {quest.data.moneyReward} and {quest.data.experienceReward} Exp");
        return true;
    }
}
