namespace BunkerGameWeb.Models
{
    public interface IConfigurableScenario
    {
        void LoadFromScenario(ScenarioData scenario);
        string GetConfigKey(); // Например: "names", "professions", "hobbies"
    }
    public class ScenarioData
    {
        // Ключ = имя файла (без расширения), Значение = массив строк из файла
        public Dictionary<string, string[]> TextFiles { get; set; } = [];
    }
}
