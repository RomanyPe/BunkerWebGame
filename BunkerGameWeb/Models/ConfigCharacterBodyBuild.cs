namespace BunkerGameWeb.Models
{
    public class ConfigCharacterBodyBuild
    {
        public string[] Text = [];
        public CharacterBodyBuild GetConfig(int idText)
        {
            return new(idText);
        }
    }
}
