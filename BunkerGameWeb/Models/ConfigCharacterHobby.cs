namespace BunkerGameWeb.Models
{
    public class ConfigCharacterHobby
    {
        public string[] Text = [];
        public CharacterHobby GetConfig(int idText, Random rnd)
        {
            return new(idText, 0, 10, rnd);
        }
    }
}
