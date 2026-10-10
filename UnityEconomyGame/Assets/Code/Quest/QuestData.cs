using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest",menuName = "Quests/Quest")]
public class QuestData : ScriptableObject
{
    [Header("Information")]
    public string questName;
    [TextArea] public string description;

    [Header("Quest")]
    public QuestType questType;
    public ItemType itemType;
    public int requiredAmount = 1;

    [Header("Quest Giver")]
    public string giverTraderId;
    
    [Header("Destination")]
    public string destinationTraderId;

    [Header("Reward")]
    public float moneyReward = 100;
    public float experienceReward = 100; 
}
