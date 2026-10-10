[System.Serializable]
public class Quest
{
    public QuestData data;
    public QuestState state;

    public Quest(QuestData data)
    {
        this.data = data;
        state = QuestState.Available;
    }
}
