namespace BunkerGameWeb.Models
{
    // 3. СЕКРЕТЫ (Skeleton in the closet)
    public class ConfigCharacterSecret
    {
        public string[] Text = [];
        public CharacterSecret GetConfig(int id) => new(id);
    }
}
