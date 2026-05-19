namespace BunkerGameWeb.Models
{
    public class ConfigCharacterProfession
    {
        public string[] Text = [];

        public CharacterProfession GetConfig(int idText, Random rnd)
        {
            return new(idText, 0, 10, rnd);
        }
    }
}
