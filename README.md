[![Build status](https://ci.appveyor.com/api/projects/status/ny3vxn69eht1j00p?svg=true)](https://ci.appveyor.com/project/sunloving/jbta)

# Тестовое задание (выдано 18.09.2017)
Библиотека, реализующая средства индексации текстовых файлов по словам. <br/>
Функционал решения:
* строит поисковый индекс по заданным файлам и каталогам;
* позволяет добавлять в индекс и удалять из индекса файлы и каталоги не блокируя поиск;
* позволяет осуществлять поиск файлов по индексу заданного слова целиком (whole word) и по префиксу;
* осуществляет мониторинг за изменениями добавленных в индекс файлов и каталогов и оперативно актуализирует индекс в соответствии с этими изменениями.
Вместе с библиотекой поставляется небольшое десктопное приложение, позволяющее использовать все возможности библиотеки.

## О решении
Решение является объектом, предоставляющим следующую функциональность:
* Метод добавления пути в индекс;
* Метод удаления пути из индекса;
* Метод поиска множества вхождений слова или префикса в файлы;
* Метод поиска файлов по заданному слову или префиксу;
* Набор событий, срабатывающих по началу/окончанию индексации файла, началу/окончанию удаления файла, при обновлении файла и при изменении пути файла.

### Индекс
Поисковый индекс представляет собой т.н. обратный индекс, т.е. соответствие `"слово" — "набор вхождений в файлы, в которых это слово встречается"`. В качестве основной структуры данных используется сжатое префиксное дерево (PATRICIA trie). Это обеспечивает пропорциональность ожидаемого времени поиска длине поискового запроса.<br/>

Для быстрого удаления файла из индекса строится прямой индекс (обратный к поисковому) по соответствию `"файл" — "множество слов из этого файла"`. Это необходимо, т.к. при удалении файла из файловой системы, из индекса он удаляется постфактум (по событию), а значит у нас нет списка ключей, которые нужно удалить из индекса. Чтобы избежать полного сканирования поискового индекса, прямой индекс предоставляет такой список.<br/>

### Обслуживание индекса
Внутри поисковой системы файлы регистрируются по версиям, которая представляет собой совокупность пути файла, даты последней записи и даты создания.<br/>

Содержимое добавленных путей отслеживается на предмет созданий/удалений/переименований файлов и каталогов. Изменения содержимого файла приводят к его переиндексации. Добавление или удаление файла приводит, соответственно, к добавлению или удалению его из индекса. Переименование файлов и каталогов влияет только пути файлов, сами файлы не переиндексируются.<br/>

Удаление файла из индекса представляет собой только маркировку неактуальных версий файла мёртвыми. Реальная очистка индекса происходит в фоне, инициируется по таймеру с заданным интервалом и удаляет все вхождения и ключи по версиям файла.<br/>

### Профиль системы
* Лучше всего подходит для реализации строки поиска с автокомплитом по регулярной базе документов.
* Наилучшим образом система будет работать для небольших и средних файлов. Для файлов относительно большого объёма система будет работать, однако переиндексация может занимать значительное время.
* Единицей индексации является файл.
* Количество файлов для скорости поиска значения не имеет.

## О реализации
Репозиторий содержит 3 проекта:
* `Jbta.SearchEngine` — библиотека, реализующая поисковую систему.
* `Jbta.SearchEngine.IntegrationTests` — набор тестов на библиотеку.
* `Jbta.SearchEngine.DemoApp` — демо-приложение, позволяющее добавлять файлы и каталоги и осуществлять простые поисковые запросы.

Библиотека реализована на базе .NET Frameword 4.7. Содержит следующие зависимости:
* `System.Runtime.CompilerServices.Unsafe` — требуется для `NonBlocking.ConcurrentDictionary`;
* `UDE.CSharp` — порт Mozilla Universal Charset Detector на .NET: инструмент для определения кодировок файлов;
Внедрённые исходники (vendored code), которые неудобно доставлять NuGet-пакетами:
* https://github.com/VSadov/NonBlocking — lock-free реализация `ConcurrentDictionary`;
* https://github.com/khalidsalomao/SimpleHelpers.Net — удобная обёртка над `UDE.CSharp`.

Писалось и собиралось в Visual Studio 2017, тест запускались через R# и xUnit Console Runner.

### Библиотека
Основным объектом является экземпляр класса `SearchEngine`, который предоставляет всю необходимую функциональность.

#### Создание
``` C#
var searchEngine = SearchEngineFactory.New();
```
или
``` C#
var settings = new SearchEngineSettings();
var searchEngine = SearchEngineFactory.New();
```
Объект настроек `SearchEngineSettings` имеет следующие опции:
* `SupportedFilesExtensions` — набор расширений индексируемых файлов в lowecase'е; по умолчанию содержит значение массива строк `txt`, `log`, `cs`, `js`, `fs`, `css`, `sql`;
* `FileParsers` — набор парсеров, которые можно самостоятельно реализовать;
* `GcCollect` — флаг, управляющий принудительной сборкой мусора после очистки индекса;
* `CleaUpIntervalMs` — double число, задающее интервал в миллисекундах, через который будут запускаться процессы очистки индекса.

#### Добавление в индекс
``` C#
var isAdded = searchEngine.Add(pathToFolderOrFile);
```

#### Удаление из индекса
``` C#
var isRemoved = searchEngine.Remove(pathToFolderOrFile);
```

#### События
``` C#
searchEngine.FileIndexingStarted += args => Console.WriteLine($"File {args.FilePath} indexing is started");
searchEngine.FileIndexingEnded += args => Console.WriteLine($"File {args.FilePath} indexing is ended");
searchEngine.FileRemovingStarted += args => Console.WriteLine($"File {args.FilePath} removing is started");
searchEngine.FileRemovingEnded += args => Console.WriteLine($"File {args.FilePath} removing is ended");
searchEngine.FileUpdateInitiated += args => Console.WriteLine($"File {args.FilePath} update is started");
searchEngine.FilePathChanged += args => Console.WriteLine($"File {args.FilePath} path is changed");
```

#### Поиск
``` C#
searchEngine.Search("foo");                       // возвращает все вхождения префикса "foo"
searchEngine.Search("foo", wholeWord: true);      // возвращает все вхождения слова "foo"
searchEngine.SearchFiles("foo");                  // возвращает все файлы, содержащие префикс "foo"
searchEngine.SearchFiles("foo", wholeWord: true); // возвращает все файлы, содержащие слово "foo"
```

#### Собственный файловый парсер
Например, можно реализовать собственный разбор определённых типов файлов:
``` C#
public class CsFileParser : IFileParser
{
    private static readonly string[] FilesExts = { "cs" };
    public IEnumerable<string> FileExtensions => FilesExts;

    public IEnumerable<ParsedWord> Parse(IFileVersion fileVersion, Encoding encoding = null)
    {
        // parse your file here; fileVersion.Path contains path to actual file
    }
}
```
и передать его в объект настроек фабрики 
``` C#
var settings = new SearchEngineSettings 
{
  FileParsers = new [] {new CsFileParser()}
}
var searchEngine = SearchEngineFactory.New();
```
При разборе `cs` файлов будет применяться `CsFileParser`, вместо стандартного.

## Куда можно двигаться дальше?
* Персистентность. Для быстрого сохранения индекса на диск и загрузки с диска может потребоваться реорганизация физического представляения префиксного дерева в памяти: перейти от space-spare структуры к хранению в двух массивах (т.н. double-array trie). Либо без реорганизации, с помощью рекурсивной сериализации узлов.
* Построение прямого индекса можно отключить. Тогда удаление из индекса будет работать посредством практически полного сканирования поискового индекса. Это опция оптимальная для ситуаций, когда удаления из файлов не ожидаются.
* В случае, если назначение предполагает индексацию больших и при этом меняющихся файлов (логов и т.п.), необходимо реализовать механизм отслеживания начала изменений в файле, чтобы переидексировать только часть файла. Как вариант, файл можно разбить на секции, вычислить дайджесты от секций и сохранить, например, бинарным деревом. Это позволит за логарифмическое время найти секцию, начиная с которой файл изменился. Однако, работать это будет только если файл меняется путём дописывания "в хвост" и не меняется в начале файла.
