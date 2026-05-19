namespace BunkerGameWeb.Models
{
    // 6. ОДЕЖДА (Equipment)
    public class ConfigCharacterEquipment
    {
        public string[] Text = [];
        public CharacterEquipment GetConfig(int id) => new(id);
    }
}
