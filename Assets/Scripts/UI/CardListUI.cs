using System;
using UnityEngine;

public class CardListUI : MonoBehaviour
{
    [SerializeField] public CardButtonUI[] cardList;

    public event Action<int> SelectTower;
    public int selectedCardTowerId = -1;
    private LevelManager owner;

    public void Configure(LevelManager level)
    {
        owner = level;
        foreach (CardButtonUI card in cardList) card.Configure(level.Config.Tower((UnitId)card.id));
        owner.Changed += RefreshCards;
        RefreshCards();
    }

    private void RefreshCards()
    {
        foreach (CardButtonUI card in cardList)
        {
            TowerDefinition data = owner.Config.Tower((UnitId)card.id);
            bool unlocked = data != null && data.unlockLevel <= owner.LevelNumber;
            card.gameObject.SetActive(unlocked);
            card.button.interactable = unlocked && owner.CanBuild((UnitId)card.id);
        }
        if (selectedCardTowerId > 0 && !owner.CanBuild((UnitId)selectedCardTowerId)) ResetSelected();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (CardButtonUI cardButtonUI in cardList)
        {
            cardButtonUI.ButtonPressed += OnCardSelected;
        }
    }

    private void OnCardSelected(int designatedId)
    {
        if (owner != null && !owner.CanBuild((UnitId)designatedId)) return;
        selectedCardTowerId = designatedId;
        SelectTower?.Invoke(selectedCardTowerId);
    }

    private void OnDestroy()
    {
        if (owner != null) owner.Changed -= RefreshCards;
        foreach (CardButtonUI card in cardList) if (card != null) card.ButtonPressed -= OnCardSelected;
    }

    public void ResetSelected()
    {
        selectedCardTowerId = -1;
    }

    public UnitId GetSelectedUnitId()
    {
        if (selectedCardTowerId <= 0) return UnitId.None;

        return (UnitId)selectedCardTowerId;
    }

    public int GetUnitCost()
    {
        if (selectedCardTowerId <= 0) return 0;

        foreach (CardButtonUI cardButtonUI in cardList) 
        {
            if(cardButtonUI.id == selectedCardTowerId) return cardButtonUI.price;
        }

        return 0;
    }
}
