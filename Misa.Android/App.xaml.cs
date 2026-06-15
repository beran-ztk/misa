namespace Misa.Android;

public partial class App : Application
{
    private static void Log(string msg) => global::Android.Util.Log.Debug("MisaApp", msg);

    public App()
    {
        Log("App ctor start");
        InitializeComponent();
        Log("App ctor done");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Log("CreateWindow called");
        return new Window(new MainPage());
    }
}
