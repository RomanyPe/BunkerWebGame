using System.Collections.Concurrent;

namespace BunkerGameWeb
{
    public static class ThemeManager
    {
        // Потокобезопасный кэш тем в оперативной памяти сервера
        private static readonly ConcurrentDictionary<string, StaticThemeData> _themesCache = new();

        // Путь к корневой папке с темами (по умолчанию папка "Themes" в корне проекта)
        private static readonly string _basePath = Path.Combine(AppContext.BaseDirectory, "Themes");
        // Счетчик: сколько КOMHAT сейчас используют каждую тему
        private static readonly ConcurrentDictionary<string, int> _themeUsageCounters = new();


        // Ваше кастомное расширение (обязательно с точкой)
        private const string Extension = ".bunk";
        /// <summary>
        /// Оперативно сканирует папку Themes и возвращает список названий доступных тем.
        /// Этот список можно сразу отправлять на фронтенд для выбора в выпадающем списке (Select).
        /// </summary>
        public static List<string> GetAvailableThemes()
        {
            var themes = new List<string>();

            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
                // Если папки не было, создаем дефолтную тему для примера
                CreateDefaultThemeFile(Path.Combine(_basePath, "Classic" + Extension));
            }

            // Сканируем только файлы с нашим расширением
            // EnumerationOptions с MatchCasing позволяет делать это быстро
            var files = Directory.EnumerateFiles(_basePath, "*" + Extension);

            foreach (var file in files)
            {
                // Получаем чистое имя файла без пути и без расширения (например, "Classic")
                themes.Add(Path.GetFileNameWithoutExtension(file));
            }

            return themes;
        }
        // Вспомогательный метод создания дефолтного шаблона при первом запуске
    private static void CreateDefaultThemeFile(string filePath)
    {
        string[] defaultTemplate = [
            "[Names]", "Игрок 1", "Игрок 2",
            "[Health]", "Здоровье хорошее", "Голова болит",
            "[Professions]", "Инженер", "Врач",
            "[BodyBuilds]", "Спортивное", "Плотное",
            "[Hobbies]", "Рыбалка", "Охота",
            "[Phobias]", "Темнота", "Высота",
            "[Inventories]", "Фонарик", "Нож",
            "[Traits]", "Добрый", "Хитрый",
            "[AdditionalInformation]", "Знает разметку", "Умеет шить",
            "[Baggage]", "Рюкзак", "Аптечка",
            "[Knowledge]", "Медицина", "Физика",
            "[Secret]", "Шпион", "Лунатик",
            "[Reproduction]", "Здоров", "Бесплоден",
            "[Vision]", "Отличное", "Близорукость",
            "[Equipment]", "Рация", "Компас",
            "[Relation]", "Друг админа", "Одиночка",
            "[Bunker]", "Затопление планеты", "Падение метеорита",
            "[CatastropheText]", "Катастрофа 1", "Катастрофа 2", "Катастрофа 3",
            "[CatastropheItemNames]", "Название предмета 1", "Название предмета 2", "Название предмета 3",
            "[CatastrophItemDescription]", "Описание предмета 1", "Описание предмета 2", "Описание предмета 3",
        ];
            string? directory = Path.GetDirectoryName(path: filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllLines(filePath, defaultTemplate);
        }
        /// <summary>
        /// Метод, который вызывает КОМНАТА при своем создании или смене темы.
        /// Увеличивает счетчик использования и возвращает тему.
        /// </summary>
        public static StaticThemeData RegisterAndGetTheme(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName)) return null;

            // Увеличиваем счетчик комнат для этой темы на 1
            _themeUsageCounters.AddOrUpdate(themeName, 1, (key, currentCount) => currentCount + 1);

            // Возвращаем тему из кэша (или загружаем, если её там не было)
            return _themesCache.GetOrAdd(themeName, LoadThemeFromCustomFile(themeName));
        }

        /// <summary>
        /// Метод, который вызывает КОМНАТА при своем УНИЧТОЖЕНИИ (когда игроки вышли).
        /// </summary>
        public static void UnregisterTheme(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName)) return;

            // Уменьшаем счетчик комнат на 1
            _themeUsageCounters.AddOrUpdate(themeName, 0, (key, currentCount) =>
            {
                int newCount = currentCount - 1;
                return newCount < 0 ? 0 : newCount; // Защита от ухода в минус
            });

            // Проверяем, играет ли КТО-НИБУДЬ еще в эту тему на сервере
            if (_themeUsageCounters.TryGetValue(themeName, out int activeRoomsCount) && activeRoomsCount == 0)
            {
                // Если комнат больше нет, запускаем фоновую задачу удаления.
                // Даем игрокам "буфер" в 5 минут (вдруг они просто пересоздают лобби).
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));

                    // Проверяем еще раз спустя 5 минут: не зашел ли кто-то новый в эту тему?
                    if (_themeUsageCounters.TryGetValue(themeName, out int currentCount) && currentCount == 0)
                    {
                        // Если всё еще 0 комнат — полностью стираем тему из оперативной памяти
                        if (_themesCache.TryRemove(themeName, out _))
                        {
                            Console.WriteLine($"[THEME LOG] Тема '{themeName}' полностью выгружена из RAM, так как нет активных комнат.");
                        }
                    }
                });
            }
        }


        /// <summary>
        /// Парсит один файл .bunk и раскладывает строки по массивам
        /// </summary>
        private static StaticThemeData LoadThemeFromCustomFile(string themeName)
        {
            string filePath = Path.Combine(_basePath, themeName + Extension);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[THEME ERROR] Файл темы не найден: {filePath}");
                // ИСПРАВЛЕНИЕ: Передаем полный путь к файлу (filePath), а не короткое имя темы!
                CreateDefaultThemeFile(filePath);
            }

            Console.WriteLine($"[THEME] Чтение кастомного файла темы: {filePath}");

            // Списки для временного сбора данных (инициализируем списки, чтобы не создавать массивы раньше времени)
            var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Names", new() },
                { "Health", new() },
                { "Professions", new() }, 
                { "BodyBuilds", new() },
                { "Hobbies", new() }, 
                { "Phobias", new() }, 
                { "Inventories", new() },
                { "Traits", new() }, 
                { "AdditionalInformation", new() }, 
                { "Baggage", new() },
                { "Knowledge", new() }, 
                { "Secret", new() }, 
                { "Reproduction", new() },
                { "Vision", new() }, 
                { "Equipment", new() }, 
                { "Relation", new() },
                { "CatastropheText", new() },
                { "CatastropheItemNames", new() },
                { "CatastrophItemDescription", new() }
            };

            string? currentSection = null;

            // Построчно читаем файл (это экономит память, так как файл не грузится в RAM целиком как гигантская строка)
            foreach (string line in File.ReadLines(filePath))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//")) continue;

                // Если строка — это заголовок секции, например [Professions]
                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    currentSection = trimmed[1..^1];
                    continue;
                }

                // Если мы внутри известной секции, добавляем строку туда
                if (currentSection != null && sections.TryGetValue(currentSection, out var list))
                {
                    list.Add(trimmed);
                }
            }

            // Переводим списки в фиксированные массивы для StaticThemeData
            return new StaticThemeData
            {
                Names = [.. sections["Names"]],
                Health = [.. sections["Health"]],
                Professions = [.. sections["Professions"]],
                BodyBuilds = [.. sections["BodyBuilds"]],
                Hobbies = [.. sections["Hobbies"]],
                Phobias = [.. sections["Phobias"]],
                Inventories = [.. sections["Inventories"]],
                Traits = [.. sections["Traits"]],
                AdditionalInformations = [.. sections["AdditionalInformation"]],
                Baggages = [.. sections["Baggage"]],
                Knowledges = [.. sections["Knowledge"]],
                Secrets = [.. sections["Secret"]],
                Reproductions = [.. sections["Reproduction"]],
                Visions = [.. sections["Vision"]],
                Equipments = [.. sections["Equipment"]],
                Relations = [.. sections["Relation"]],
                CatastropheText = [.. sections["CatastropheText"]],
                CatastropheItemNames = [.. sections["CatastropheItemNames"]],
                CatastrophItemDescription = [.. sections["CatastrophItemDescription"]]
            };
        }
    }

    // Плоский класс для хранения массивов строк в памяти
    public class StaticThemeData
    {
        // Системные и основные характеристики
        public string[] Names { get; init; } = [];
        public string[] Health { get; init; } = [];
        public string[] Professions { get; init; } = [];
        public string[] BodyBuilds { get; init; } = [];
        public string[] Hobbies { get; init; } = [];
        public string[] Phobias { get; init; } = [];
        public string[] Inventories { get; init; } = [];
        public string[] Traits { get; init; } = [];
        public string[] AdditionalInformations { get; init; } = [];

        // Дополнительные характеристики карточек
        public string[] Baggages { get; init; } = [];
        public string[] Knowledges { get; init; } = [];
        public string[] Secrets { get; init; } = [];
        public string[] Reproductions { get; init; } = [];
        public string[] Visions { get; init; } = [];
        public string[] Equipments { get; init; } = [];
        public string[] Relations { get; init; } = [];

        // Данные самого бункера (условия катастрофы и т.д.)
        public string[] CatastropheText { get; init; } = [];
        public string[] CatastropheItemNames { get; init; } = [];
        public string[] CatastrophItemDescription { get; init; } = [];
    }
}
