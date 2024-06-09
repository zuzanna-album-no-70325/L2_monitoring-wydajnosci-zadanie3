using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace zadanie_3_zuzanna_lukiewska
{
    public partial class MainWindow
    {
        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _ramCounter;
        private PerformanceCounter? _diskCounter;
        private PerformanceCounter? _threadCounter;
        private readonly DispatcherTimer _refreshTimer;
        private readonly Configuration _config;

        public MainWindow()
        {
            InitializeComponent();

            // Wczytanie konfiguracji z pliku lub ustawienie domyślnej konfiguracji
            _config = FetchConfiguration();

            Console.WriteLine($"Loaded configuration: CpuThreshold: {_config.CpuThreshold}, RamThreshold: {_config.RamThreshold}, DiskThreshold: {_config.DiskThreshold}, ThreadThreshold: {_config.ThreadThreshold}, LogFilePath: {_config.LogFilePath}, RefreshInterval: {_config.RefreshInterval}");

            // Inicjalizacja liczników wydajności
            SetupPerformanceCounters();

            // Konfiguracja i uruchomienie timera
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_config.RefreshInterval ?? 1)
            };
            _refreshTimer.Tick += OnTimerTick;
            _refreshTimer.Start();
        }

        // Metoda inicjalizująca liczniki wydajności
        private void SetupPerformanceCounters()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            _diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            _threadCounter = new PerformanceCounter("Process", "Thread Count", "_Total");
        }

        // Metoda wywoływana na każde tyknięcie timera
        private void OnTimerTick(object? sender, EventArgs e)
        {
            // Aktualizacja wartości liczników wydajności
            UpdateCounters();

            // Sprawdzenie czy któryś z liczników przekroczył progi z konfiguracji
            if (_cpuCounter?.NextValue() <= _config.CpuThreshold &&
                _ramCounter?.NextValue() >= _config.RamThreshold &&
                _diskCounter?.NextValue() <= _config.DiskThreshold &&
                _threadCounter?.NextValue() <= _config.ThreadThreshold)
                return;

            // Zapis logu do pliku
            using var logWriter = new StreamWriter(_config.LogFilePath ?? "log.txt", true);
            var logEntry = $"{DateTime.Now}: {cpuLabel.Content}, {ramLabel.Content}, {diskLabel.Content}, {threadLabel.Content}";
            logWriter.WriteLine(logEntry);

            // Zapis logu do dziennika zdarzeń systemowych
            using var appLog = new EventLog("Application");
            appLog.Source = "My Application";
            appLog.WriteEntry(logEntry, EventLogEntryType.Information);
        }

        // Metoda aktualizująca wartości liczników wydajności
        private void UpdateCounters()
        {
            cpuLabel.Content = $"CPU Usage: {_cpuCounter?.NextValue() ?? 0}%";
            ramLabel.Content = $"RAM Available: {_ramCounter?.NextValue() ?? 0}MB";
            diskLabel.Content = $"Disk Usage: {_diskCounter?.NextValue() ?? 0}%";
            threadLabel.Content = $"Thread Count: {_threadCounter?.NextValue() ?? 0}";
        }

        // Metoda wczytująca konfigurację z pliku XML
        private static Configuration FetchConfiguration()
        {
            try
            {
                using var reader = new StreamReader("config.xml");
                var serializer = new XmlSerializer(typeof(Configuration));
                var config = (Configuration?)serializer.Deserialize(reader);
                if (config != null)
                {
                    Console.WriteLine($"Loaded configuration from file: {config}");
                    return config;
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Configuration file not found. Using default settings.");
            }

            // Wczytanie domyślnej konfiguracji w przypadku braku pliku konfiguracyjnego
            var defaultConfig = CreateDefaultConfiguration();
            Console.WriteLine($"Using default configuration: {defaultConfig}");
            return defaultConfig;
        }

        // Metoda tworząca domyślną konfigurację
        private static Configuration CreateDefaultConfiguration()
        {
            return new Configuration
            {
                CpuThreshold = 10.0f,
                RamThreshold = 5000.0f,
                DiskThreshold = 10.0f,
                ThreadThreshold = 1000.0f,
                LogFilePath = "log.txt",
                RefreshInterval = 1
            };
        }
    }

    // Klasa reprezentująca konfigurację aplikacji
    public class Configuration
    {
        public float? CpuThreshold { get; init; }
        public float? RamThreshold { get; init; }
        public float? DiskThreshold { get; init; }
        public float? ThreadThreshold { get; init; }
        public string? LogFilePath { get; init; }
        public int? RefreshInterval { get; init; }
    }
}
