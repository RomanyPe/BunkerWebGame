namespace BunkerGameWeb.Models
{
    public class Player
    {
        public int Id { get; set; }
        public bool IsReady { get; set; } = false;
        public string Name { get; set; } = string.Empty;
        public CharacterBiologicalSex BiologicalSex;
        public CharacterAge Age;
        public CharacterProfession Profession;
        public CharacterHealth Health;
        public CharacterBodyBuild BodyBuild;
        public CharacterHobby Hobby;
        public CharacterPhobia Phobia;
        public CharacterInventory Inventory;
        public CharacterTrait Trait;
        public CharacterAdditionalInformation AdditionalInformation;
        public CharacterSpecialCondition SpecialCondition;

        public CharacterBaggage Baggage;              // Крупный инвентарь
        public CharacterKnowledge Knowledge;          // Знания/Навыки
        public CharacterSecret Secret;                // Секреты
        public CharacterReproduction Reproduction;    // Полезность для возрождения
        public CharacterVision Vision;

        public CharacterEquipment Equipment;
        public CharacterRelation Relation;

        public int CountNeedOpen = 1;
        public int CurrentOpenedCard = 0;
        public bool IsEliminated { get; set; } = false;
        public HashSet<PlayerFieldType> ListOpenedTypes = [];

        // ✅ Временный выбор (виден только игроку)
        public HashSet<PlayerFieldType> PendingOpenedTypes = [];
        public bool IsSelectionConfirmed { get; set; } = false;
        public string SessionKey { get; set; } = string.Empty;
        public bool IsWinner { get; set; } = false;
        public bool IsConnected { get; set; } = true;   // активно ли соединение
        public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;

        // Метод для применения выбора
        public void ApplyPendingSelections()
        {
            foreach (var type in PendingOpenedTypes)
            {
                ListOpenedTypes.Add(type);
            }
            PendingOpenedTypes.Clear();
        }

        // Метод для отмены выбора
        public void CancelPendingSelections()
        {
            PendingOpenedTypes.Clear();
            CurrentOpenedCard = 0;
            IsSelectionConfirmed = false;
        }

    }
}
