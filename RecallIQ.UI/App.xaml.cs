using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using RecallIQ.AI;
using RecallIQ.Core.Interfaces;
using RecallIQ.Indexing;
using RecallIQ.Indexing.Parsers;
using RecallIQ.Search;
using RecallIQ.Storage;
using RecallIQ.UI.Services;
using RecallIQ.UI.ViewModels;
using Serilog;

namespace RecallIQ.UI;

public partial class App : Application
{
    private static IServiceProvider _serviceProvider = null!;
    public static Window? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        ConfigureServices();

        var settingsService = GetService<ISettingsService>();
        await settingsService.LoadAsync();

        var settings = settingsService.CurrentSettings;

        var storage = GetService<IStorageService>();
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RecallIQ", settings.DatabasePath);
        await storage.InitializeAsync(dbPath);

        var embeddingService = GetService<IEmbeddingService>();
        var modelPath = Path.Combine(AppContext.BaseDirectory, settings.AiModelPath);
        await embeddingService.InitializeAsync(modelPath);

        var ocrService = GetService<IOcrService>();
        var tessPath = Path.Combine(AppContext.BaseDirectory, settings.TesseractDataPath);
        await ocrService.InitializeAsync(tessPath);

        MainWindow = new MainWindow();

        if (MainWindow.Content is FrameworkElement root)
        {
            root.RequestedTheme = settings.IsDarkMode
                ? ElementTheme.Dark
                : ElementTheme.Light;
        }

        MainWindow.Activate();

        if (settings.IsIndexingEnabled && settings.WatchedFolders.Count > 0)
        {
            var fileWatcher = GetService<IFileWatcherService>();
            foreach (var folder in settings.WatchedFolders)
                fileWatcher.WatchFolder(folder);

            _ = Task.Run(async () =>
            {
                var indexer = GetService<IIndexingService>();
                await indexer.StartIndexingAsync(settings.WatchedFolders);
            });
        }
    }

    private static void ConfigureServices()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RecallIQ", "logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(logDir, "recalliq-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: true);
        });

        services.AddSingleton<IStorageService, SqliteStorageService>();
        services.AddSingleton<IEmbeddingService, OnnxEmbeddingService>();
        services.AddSingleton<IOcrService, TesseractOcrService>();
        services.AddSingleton<ITextChunker, TextChunker>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IFileWatcherService, FileWatcherService>();
        services.AddSingleton<NavigationService>();

        services.AddSingleton<IDocumentParser, PdfDocumentParser>();
        services.AddSingleton<IDocumentParser, DocxDocumentParser>();
        services.AddSingleton<IDocumentParser, TextDocumentParser>();
        services.AddSingleton<IDocumentParser, MarkdownDocumentParser>();
        services.AddSingleton<IDocumentParser, ImageDocumentParser>();
        services.AddSingleton<DocumentParserFactory>();

        services.AddSingleton<IIndexingService, IndexingService>();
        services.AddSingleton<ISearchService, VectorSearchService>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddTransient<DocumentsViewModel>();
        services.AddTransient<ActivityViewModel>();
        services.AddTransient<SettingsViewModel>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public static T GetService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled exception");
        Log.CloseAndFlush();
        e.Handled = true;
    }
}
