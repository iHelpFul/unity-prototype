using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/UI/Quest UI Icon Catalog")]
public class QuestUiIconCatalog : ScriptableObject
{
    [Header("Rewards")]
    [SerializeField] private Sprite mesosIcon;
    [SerializeField] private Sprite expIcon;

    public Sprite MesosIcon => mesosIcon;
    public Sprite ExpIcon => expIcon;
}
