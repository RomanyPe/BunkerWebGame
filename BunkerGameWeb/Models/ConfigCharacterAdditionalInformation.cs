namespace BunkerGameWeb.Models
{
    public class ConfigCharacterAdditionalInformation
    {
        public string[] Text =[];
        public CharacterAdditionalInformation GetConfig(int idText)
        {
            return new(idText);
        }
    }
}
