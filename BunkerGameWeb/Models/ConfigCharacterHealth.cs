namespace BunkerGameWeb.Models
{
    public class ConfigCharacterHealth  
    {
        public string[] Text = [];


        public CharacterHealth GetConfig(int idText, Random rnd)
        {
            return new(idText, 0, 10, rnd);
        }
    }
}
