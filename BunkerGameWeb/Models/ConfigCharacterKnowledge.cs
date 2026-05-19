namespace BunkerGameWeb.Models
{
    // 2. ЗНАНИЯ (Skills) - в FinalAmount будет стаж или уровень навыка
    public class ConfigCharacterKnowledge
    {
        public string[] Text = [];
        public CharacterKnowledge GetConfig(int id, Random rnd) => new(id, 1, 10, rnd);
    }
}
