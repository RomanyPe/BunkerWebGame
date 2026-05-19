namespace BunkerGameWeb.Models
{
    public class ConfigCharacterPhobia
    {
        public string[] Text = [];
        public CharacterPhobia GetConfig(int idText)
        {
            return new(idText);
        }
    }
}
