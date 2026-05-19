namespace BunkerGameWeb.Models
{
    public class ConfigCharacterBaggage
    {
        public string[] Text = [];
        public CharacterBaggage GetConfig(int id, Random rnd) => new(id, 5, 80, rnd);
    }
}
