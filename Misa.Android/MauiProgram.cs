namespace Misa.Android;

public static class MauiProgram
{
    private static void Log(string msg) => global::Android.Util.Log.Debug("MisaApp", msg);

    public static MauiApp CreateMauiApp()
    {
        Log("CreateMauiApp start");
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        var app = builder.Build();
        Log("CreateMauiApp done");
        return app;
    }
}
