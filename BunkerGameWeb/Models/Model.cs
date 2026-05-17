namespace BunkerGameWeb.Models
{

    public struct CharacterBiologicalSex
    {
        public int NameTrait;
        public float MinAmount;
        public float MaxAmount;
        public bool IsOpened;
        public CharacterBiologicalSex(int nameTrait, float minAmount, float maxAmount, Random rng)
        {
            
            NameTrait = nameTrait;
            MinAmount = minAmount;
            MaxAmount = maxAmount;
            InitAmount(rng);
        }

        public float FinalAmount { get; set; } = 0;

        public void InitAmount(Random rnd)
        {
            float randomValue = (float)rnd.NextDouble() * (MaxAmount - MinAmount) + MinAmount;

            // Округляем до 1 знака после запятой (например, 75.5 кг)
            FinalAmount = (float)Math.Round(randomValue, 1);
        }
    }

    public struct CharacterAge(int nameTrait, float amount)
    {
        public int NameTrait = nameTrait;
        public float Amount = amount;
        public bool IsOpened;
    }

    public struct CharacterProfession
    {
        public int NameTrait ;
        public float MinAmount;
        public float MaxAmount;
        public bool IsOpened;
        public CharacterProfession(int nameTrait, float minAmount, float maxAmount, Random rng)
        {
            NameTrait = nameTrait;
            MinAmount = minAmount;
            MaxAmount = maxAmount;
            InitAmount(rng);
        }

        public float FinalAmount { get; set;}

        public void InitAmount(Random rnd)
        {
            float randomValue = (float)rnd.NextDouble() * (MaxAmount - MinAmount) + MinAmount;

            // Округляем до 1 знака после запятой (например, 75.5 кг)
            FinalAmount = (float)Math.Round(randomValue, 1);
        }

    }

    public struct CharacterHealth
    {
        public int NameTrait ;
        public float MinAmount;
        public float MaxAmount;
        public bool IsOpened;
        public CharacterHealth(int nameTrait, float minAmount, float maxAmount, Random rng)
        {
            NameTrait = nameTrait;
            MinAmount = minAmount;
            MaxAmount = maxAmount;
            InitAmount(rng);
        }

        public float FinalAmount { get; set;}

        public void InitAmount(Random rnd)
        {
            float randomValue = (float)rnd.NextDouble() * (MaxAmount - MinAmount) + MinAmount;

            // Округляем до 1 знака после запятой (например, 75.5 кг)
            FinalAmount = (float)Math.Round(randomValue, 1);
        }

    }

    public struct CharacterBodyBuild(int nameTrait)
    {
        public int NameTrait = nameTrait;
        public bool IsOpened;
    }

    public struct CharacterHobby
    {
        public int NameTrait ;
        public float MinAmount;
        public float MaxAmount;
        public bool IsOpened;
        public CharacterHobby(int nameTrait, float minAmount, float maxAmount, Random rng)
        {
            NameTrait = nameTrait;
            MinAmount = minAmount;
            MaxAmount = maxAmount;
            InitAmount(rng);
        }

        public float FinalAmount { get; set;}

        public void InitAmount(Random rnd)
        {
            float randomValue = (float)rnd.NextDouble() * (MaxAmount - MinAmount) + MinAmount;

            // Округляем до 1 знака после запятой (например, 75.5 кг)
            FinalAmount = (float)Math.Round(randomValue, 1);
        }

    }

    public struct CharacterPhobia(int nameTrait)
    {
        public int NameTrait = nameTrait;
        public bool IsOpened;
    }

    public struct CharacterInventory(int nameTrait)
    {
        public int NameTrait = nameTrait;
        public bool IsOpened;
    }

    public struct CharacterTrait(int nameTrait)
    {
        public int NameTrait = nameTrait;
        public bool IsOpened;
    }

    public struct CharacterAdditionalInformation(int nameTrait)
    {
        public int NameTrait = nameTrait;
        public bool IsOpened;
    }

    public struct CharacterSpecialCondition(
        int nameTrait,
        CharacterSpecialConditionType type,
        PlayerFieldType playerFieldType)
    {
        public bool IsOpened;
        public int NameTrait = nameTrait;
        public CharacterSpecialConditionType Type = type;
        public PlayerFieldType PlayerFieldType = playerFieldType;
        public bool IsUsed = false;
    }

    // Структуры для новых полей
    public struct CharacterBaggage 
    { 
        public int NameTrait; 
        public float MinAmount; 
        public float MaxAmount; 
        public bool IsOpened;
        public float FinalAmount { get; set; } 
        public CharacterBaggage(int nameTrait, float min, float max, Random rnd) 
        { NameTrait = nameTrait; 
            MinAmount = min; MaxAmount = max; 
            IsOpened = false; 
            FinalAmount = InitAmount(rnd);

        }
        public readonly float InitAmount(Random rnd)
        {
            return (float)Math.Round((float)rnd.NextDouble() * (MaxAmount - MinAmount) + MinAmount, 1);
        }
    }

    public struct CharacterKnowledge 
    { 
        public int NameTrait; 
        public float MinAmount; 
        public float MaxAmount; 
        public bool IsOpened;
        public float FinalAmount { get; set; } 
        public CharacterKnowledge(int nameTrait, float min, float max, Random rnd) 
        { NameTrait = nameTrait; 
            MinAmount = min; MaxAmount = max; 
            IsOpened = false; FinalAmount = InitAmount(rnd);

        }
        public readonly float InitAmount(Random rnd)
        {
            return (float)Math.Round((float)rnd.NextDouble() * (MaxAmount - MinAmount) + MinAmount, 1);
        }
    }

    public struct CharacterSecret(int nameTrait)
    { 
        public int NameTrait = nameTrait; 
        public bool IsOpened = false;
    }

    public struct CharacterReproduction 
    { 
        public int NameTrait; 
        public float MinAmount; 
        public float MaxAmount; 
        public bool IsOpened;
        public float FinalAmount { get; set; } 
        public CharacterReproduction(int nameTrait, float min, float max, Random rnd
            ) { NameTrait = nameTrait; 
            MinAmount = min; MaxAmount = max; 
            IsOpened = false; 
            FinalAmount = InitAmount(rnd);
        }
        public readonly float InitAmount(Random rnd)
        {
            return (float)Math.Round((float)rnd.NextDouble() * (MaxAmount - MinAmount) + MinAmount, 1);
        }
    }

    public struct CharacterVision(int nameTrait)
    { 
        public int NameTrait = nameTrait; 
        public bool IsOpened = false;
    }
    // Структура для Снаряжения
    public struct CharacterEquipment(int nameTrait)
    {
        public int NameTrait = nameTrait;
        public bool IsOpened = false;
    }

    // Структура для Отношений
    public struct CharacterRelation(int nameTrait)
    {
        public int NameTrait = nameTrait;
        public bool IsOpened = false;
    }

    public enum CharacterSpecialConditionType : byte
    {
        Swap,
        Rerole,
        Upgrade,
        Snow
    }

    public enum PlayerFieldType : byte
    {
        BiologicalSex,
        Age,
        Profession,
        Health,
        BodyBuild,
        Hobby,
        Phobia,
        Inventory,
        Trait,
        AdditionalInformation,
        SpecialCondition,
        Baggage,               // Багаж (крупный инвентарь/груз)
        Knowledge,             // Знания и навыки (Hard Skills)
        Secret,                // Секреты (скелеты в шкафу)
        Reproduction,          // Полезность для возрождения (биологический потенциал)
        Vision,                // Видение катастрофы (мировоззрение)
        Equipment,             // Снаряжение (одежда/защита)
        Relation,              // Отношение к другим (социальный статус/связи)

       
    }

    public struct BunkerStats(BunkerItem[] bunkerItems, int timeToNeedInBunker, int maxPlayerCount, int bunkerDescriptionID)
    {
        public BunkerItem[] BunkerItems = bunkerItems;
        public int TimeToNeedInBunker = timeToNeedInBunker;
        public int MaxPlayerCount = maxPlayerCount;
        public int BunkerDescriptionID = bunkerDescriptionID;
        public bool IsCreate = false;
    }

    public struct BunkerItem(int itemName, int itemDescription, int itemCount)
    {
        public int ItemName = itemName;
        public int ItemDescription = itemDescription;
        public int ItemCount = itemCount;
    }
}
