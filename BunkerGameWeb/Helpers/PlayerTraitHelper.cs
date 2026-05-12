using BunkerGameWeb.Models;

namespace BunkerGameWeb.Helpers
{
    public static class PlayerTraitHelper
    {
        // Метод для проверки, открыта ли характеристика
        public static bool IsOpened(Player player, PlayerFieldType traitName)
        {
            return traitName switch
            {
                PlayerFieldType.BiologicalSex => player.BiologicalSex.IsOpened,
                PlayerFieldType.Age => player.Age.IsOpened,
                PlayerFieldType.Profession => player.Profession.IsOpened,
                PlayerFieldType.Health => player.Health.IsOpened,
                PlayerFieldType.BodyBuild => player.BodyBuild.IsOpened,
                PlayerFieldType.Hobby => player.Hobby.IsOpened,
                PlayerFieldType.Phobia => player.Phobia.IsOpened,
                PlayerFieldType.Inventory => player.Inventory.IsOpened,
                PlayerFieldType.Trait => player.Trait.IsOpened,
                PlayerFieldType.AdditionalInformation => player.AdditionalInformation.IsOpened,
                PlayerFieldType.SpecialCondition => player.SpecialCondition.IsOpened,
                PlayerFieldType.Baggage => player.Baggage.IsOpened,
                PlayerFieldType.Knowledge => player.Knowledge.IsOpened,
                PlayerFieldType.Secret => player.Secret.IsOpened,
                PlayerFieldType.Reproduction => player.Reproduction.IsOpened,
                PlayerFieldType.Vision => player.Vision.IsOpened,
                PlayerFieldType.Equipment => player.Equipment.IsOpened,
                PlayerFieldType.Relation => player.Relation.IsOpened,
                _ => false
            };
        }

        // Метод для открытия характеристики
        public static void SetOpened(Player player, PlayerFieldType traitName, bool isOpened)
        {
            switch (traitName)
            {
                case PlayerFieldType.BiologicalSex:
                    player.BiologicalSex.IsOpened = isOpened;
                    break;
                case PlayerFieldType.Age:
                    player.Age.IsOpened = isOpened;
                    break;
                case PlayerFieldType.Profession:
                    player.Profession.IsOpened = isOpened;
                    break;
                case PlayerFieldType.Health:
                    player.Health.IsOpened = isOpened;
                    break;
                case PlayerFieldType.BodyBuild:
                    player.BodyBuild.IsOpened = isOpened;
                    break;
                case PlayerFieldType.Hobby:
                    player.Hobby.IsOpened = isOpened;
                    break;
                case PlayerFieldType.Phobia:
                    player.Phobia.IsOpened = isOpened;
                    break;
                case PlayerFieldType.Inventory:
                    player.Inventory.IsOpened = isOpened;
                    break;
                case PlayerFieldType.Trait:
                    player.Trait.IsOpened = isOpened;
                    break;
                case PlayerFieldType.AdditionalInformation:
                    player.AdditionalInformation.IsOpened = isOpened;
                    break;
                case PlayerFieldType.SpecialCondition:
                    player.SpecialCondition.IsOpened = isOpened;
                    break;
                case PlayerFieldType.Baggage:
                    player.Baggage.IsOpened = isOpened;
                    break;
                case PlayerFieldType.Knowledge:
                    player.Knowledge.IsOpened = isOpened;
                    break;
                case PlayerFieldType.Secret:
                    player.Secret.IsOpened = isOpened;
                    break;
                case PlayerFieldType.Reproduction:
                    player.Reproduction.IsOpened = isOpened;
                    break;
                case PlayerFieldType.Vision:
                    player.Vision.IsOpened = isOpened;
                    break;
                case PlayerFieldType.Equipment:
                    player.Equipment.IsOpened = isOpened;
                    break;
                case PlayerFieldType.Relation:
                    player.Relation.IsOpened = isOpened;
                    break;
            }
        }

        // Метод для получения значения характеристики
        public static object GetValue(Player player, PlayerFieldType fieldType)
        {
            return fieldType switch
            {
                PlayerFieldType.BiologicalSex => player.BiologicalSex,
                PlayerFieldType.Age => player.Age,
                PlayerFieldType.Profession => player.Profession,
                PlayerFieldType.Health => player.Health,
                PlayerFieldType.BodyBuild => player.BodyBuild,
                PlayerFieldType.Hobby => player.Hobby,
                PlayerFieldType.Phobia => player.Phobia,
                PlayerFieldType.Inventory => player.Inventory,
                PlayerFieldType.Trait => player.Trait,
                PlayerFieldType.AdditionalInformation => player.AdditionalInformation,
                PlayerFieldType.SpecialCondition => player.SpecialCondition,
                PlayerFieldType.Baggage => player.Baggage,
                PlayerFieldType.Knowledge => player.Knowledge,
                PlayerFieldType.Secret => player.Secret,
                PlayerFieldType.Reproduction => player.Reproduction,
                PlayerFieldType.Vision => player.Vision,
                PlayerFieldType.Equipment => player.Equipment,
                PlayerFieldType.Relation => player.Relation,
                _ => null!
            };
        }

        // Метод для установки значения характеристики
        public static void SetValue(Player player, PlayerFieldType fieldType, object value)
        {
            switch (fieldType)
            {
                case PlayerFieldType.BiologicalSex:
                    player.BiologicalSex = (CharacterBiologicalSex)value;
                    break;
                case PlayerFieldType.Age:
                    player.Age = (CharacterAge)value;
                    break;
                case PlayerFieldType.Profession:
                    player.Profession = (CharacterProfession)value;
                    break;
                case PlayerFieldType.Health:
                    player.Health = (CharacterHealth)value;
                    break;
                case PlayerFieldType.BodyBuild:
                    player.BodyBuild = (CharacterBodyBuild)value;
                    break;
                case PlayerFieldType.Hobby:
                    player.Hobby = (CharacterHobby)value;
                    break;
                case PlayerFieldType.Phobia:
                    player.Phobia = (CharacterPhobia)value;
                    break;
                case PlayerFieldType.Inventory:
                    player.Inventory = (CharacterInventory)value;
                    break;
                case PlayerFieldType.Trait:
                    player.Trait = (CharacterTrait)value;
                    break;
                case PlayerFieldType.AdditionalInformation:
                    player.AdditionalInformation = (CharacterAdditionalInformation)value;
                    break;
                case PlayerFieldType.SpecialCondition:
                    player.SpecialCondition = (CharacterSpecialCondition)value;
                    break;
                case PlayerFieldType.Baggage:
                    player.Baggage = (CharacterBaggage)value;
                    break;
                case PlayerFieldType.Knowledge:
                    player.Knowledge = (CharacterKnowledge)value;
                    break;
                case PlayerFieldType.Secret:
                    player.Secret = (CharacterSecret)value;
                    break;
                case PlayerFieldType.Reproduction:
                    player.Reproduction = (CharacterReproduction)value;
                    break;
                case PlayerFieldType.Vision:
                    player.Vision = (CharacterVision)value;
                    break;
                case PlayerFieldType.Equipment:
                    player.Equipment = (CharacterEquipment)value;
                    break;
                case PlayerFieldType.Relation:
                    player.Relation = (CharacterRelation)value;
                    break;
            }
        }

        // Метод для получения FinalAmount характеристики
        public static float GetFinalAmount(Player player, PlayerFieldType fieldType)
        {
            return fieldType switch
            {
                PlayerFieldType.BiologicalSex => player.BiologicalSex.FinalAmount,
                PlayerFieldType.Age => player.Age.Amount,
                PlayerFieldType.Profession => player.Profession.FinalAmount,
                PlayerFieldType.Health => player.Health.FinalAmount,
                PlayerFieldType.BodyBuild => player.BodyBuild.FinalAmount,
                PlayerFieldType.Hobby => player.Hobby.FinalAmount,
                PlayerFieldType.Phobia => player.Phobia.FinalAmount,
                PlayerFieldType.Inventory => player.Inventory.FinalAmount,
                PlayerFieldType.Trait => player.Trait.FinalAmount,
                PlayerFieldType.AdditionalInformation => player.AdditionalInformation.FinalAmount,
                PlayerFieldType.SpecialCondition => 0f,
                PlayerFieldType.Baggage => player.Baggage.FinalAmount,
                PlayerFieldType.Knowledge => player.Knowledge.FinalAmount,
                PlayerFieldType.Secret => player.Secret.FinalAmount,
                PlayerFieldType.Reproduction => player.Reproduction.FinalAmount,
                PlayerFieldType.Vision => player.Vision.FinalAmount,
                PlayerFieldType.Equipment => player.Equipment.FinalAmount,
                PlayerFieldType.Relation => player.Relation.FinalAmount,
                _ => 0f
            };
        }

        // Метод для установки FinalAmount характеристики
        public static void SetFinalAmount(Player player, PlayerFieldType fieldType, float value)
        {
            switch (fieldType)
            {
                case PlayerFieldType.BiologicalSex:
                    player.BiologicalSex.FinalAmount = value;
                    break;
                case PlayerFieldType.Age:
                    // Age.Amount только для чтения
                    break;
                case PlayerFieldType.Profession:
                    player.Profession.FinalAmount = value;
                    break;
                case PlayerFieldType.Health:
                    player.Health.FinalAmount = value;
                    break;
                case PlayerFieldType.BodyBuild:
                    player.BodyBuild.FinalAmount = value;
                    break;
                case PlayerFieldType.Hobby:
                    player.Hobby.FinalAmount = value;
                    break;
                case PlayerFieldType.Phobia:
                    player.Phobia.FinalAmount = value;
                    break;
                case PlayerFieldType.Inventory:
                    player.Inventory.FinalAmount = value;
                    break;
                case PlayerFieldType.Trait:
                    player.Trait.FinalAmount = value;
                    break;
                case PlayerFieldType.AdditionalInformation:
                    player.AdditionalInformation.FinalAmount = value;
                    break;
                case PlayerFieldType.SpecialCondition:
                    // SpecialCondition не имеет FinalAmount
                    break;
                case PlayerFieldType.Baggage:
                    player.Baggage.FinalAmount = value;
                    break;
                case PlayerFieldType.Knowledge:
                    player.Knowledge.FinalAmount = value;
                    break;
                case PlayerFieldType.Secret:
                    player.Secret.FinalAmount = value;
                    break;
                case PlayerFieldType.Reproduction:
                    player.Reproduction.FinalAmount = value;
                    break;
                case PlayerFieldType.Vision:
                    player.Vision.FinalAmount = value;
                    break;
                case PlayerFieldType.Equipment:
                    player.Equipment.FinalAmount = value;
                    break;
                case PlayerFieldType.Relation:
                    player.Relation.FinalAmount = value;
                    break;
            }
        }

        // Метод для получения максимального значения характеристики
        public static float GetMaxAmount(Player player, PlayerFieldType fieldType)
        {
            return fieldType switch
            {
                PlayerFieldType.Profession => player.Profession.MaxAmount,
                PlayerFieldType.Health => player.Health.MaxAmount,
                PlayerFieldType.BodyBuild => player.BodyBuild.MaxAmount,
                PlayerFieldType.Hobby => player.Hobby.MaxAmount,
                PlayerFieldType.Phobia => player.Phobia.MaxAmount,
                PlayerFieldType.Inventory => player.Inventory.MaxAmount,
                PlayerFieldType.Trait => player.Trait.MaxAmount,
                PlayerFieldType.AdditionalInformation => player.AdditionalInformation.MaxAmount,
                PlayerFieldType.Knowledge => player.Knowledge.MaxAmount,
                PlayerFieldType.Reproduction => player.Reproduction.MaxAmount,
                PlayerFieldType.Baggage => player.Baggage.MaxAmount,
                PlayerFieldType.Equipment => player.Equipment.MaxAmount,
                PlayerFieldType.Relation => player.Relation.MaxAmount,
                _ => 10f // Значение по умолчанию
            };
        }
    }
}
