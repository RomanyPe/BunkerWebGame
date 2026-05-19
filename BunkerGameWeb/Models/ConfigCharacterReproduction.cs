namespace BunkerGameWeb.Models
{
    // 4. ПОЛЕЗНОСТЬ ДЛЯ ВОЗРОЖДЕНИЯ (Reproduction) - FinalAmount % вероятности успеха
    public class ConfigCharacterReproduction
    {
        public string[] Text = [];
        public CharacterReproduction GetConfig(int id, Random rnd) => new(id, 0, 100, rnd);
    }
}
