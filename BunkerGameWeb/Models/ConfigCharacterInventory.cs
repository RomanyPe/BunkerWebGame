namespace BunkerGameWeb.Models
{
    public class ConfigCharacterInventory
    {
        public string[] Text = [];
        public CharacterInventory GetConfig(int idText)
        {
            return new(idText);
        }
    }
}
