namespace BunkerGameWeb.Models
{
    public class ConfigCharacterAge
    {
        public string[] Text = ["младенец", "ребенок","подросток","молод еще","взрослый","пенсионер"];

        public CharacterAge GetConfig(int Age)
        {
            byte id = Age switch
            {
                <= 3 and >= 0 => 0,
                <= 14 and >= 4 => 1,
                <= 20 and >= 15 => 2,
                <= 30 and >= 21 => 3, // Вложенная проверка пола
                <= 65 and >= 31 => 4,
                >= 65 => 5,
                _ => 5
            };



            return new(id, Age);
        }

    }
}
