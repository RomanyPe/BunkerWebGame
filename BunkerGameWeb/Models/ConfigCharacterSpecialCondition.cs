namespace BunkerGameWeb.Models
{
    public class ConfigCharacterSpecialCondition
    {
        public string[] Text = [
            "Поменяться характеристокой с игроком",
            "Поменять на случайную",
            "Улучишить//облегчить характеристику",
            "Показать чужую характеристику",
];
        public CharacterSpecialCondition GetConfig(int idText, Random rnd)
        {

            CharacterSpecialConditionType type = Enum.GetValues<CharacterSpecialConditionType>()[idText];
            PlayerFieldType[] values;
           
            if (type == CharacterSpecialConditionType.Upgrade)
            {
                values = [PlayerFieldType.Age, PlayerFieldType.Health, PlayerFieldType.Knowledge, PlayerFieldType.Reproduction];
            }
            else
            {
                values = Enum.GetValues<PlayerFieldType>();
            }

            PlayerFieldType field = values[rnd.Next(values.Length)];

            return new(idText, type, field);
        }
    }
}
