namespace BunkerGameWeb.Models
{
    // 7. ОТНОШЕНИЯ (Социальный статус и связи)
    public class ConfigCharacterRelation
    {
        public string[] Text = [];
        public CharacterRelation GetConfig(int id) => new(id);
    }
}
