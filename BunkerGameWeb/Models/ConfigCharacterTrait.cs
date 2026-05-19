namespace BunkerGameWeb.Models
{
    public class ConfigCharacterTrait
    {
        public string[] Text = [];
        public CharacterTrait GetConfig(int idText)
        {
            return new(idText);
        }
    }
}
