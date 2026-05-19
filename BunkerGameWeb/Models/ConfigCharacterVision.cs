namespace BunkerGameWeb.Models
{
    // 5. ВИДЕНИЕ КАТАСТРОФЫ (Vision)
    public class ConfigCharacterVision
    {
        public string[] Text = [];
        public CharacterVision GetConfig(int id) => new(id);
    }
}
