using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using modbus_ant.utils;
using modbus_ant.ViewModels;
using modbus_ant.Views;
using Serilog;
using Splat;
using Splat.Serilog;

namespace modbus_ant;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
           
            
            Locator.CurrentMutable.RegisterConstant(new LogProvider(), typeof(ILogProvider));

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Observers(events => events.Do(evt =>{
                    Locator.Current.GetService<ILogProvider>()?.Post(
                        $"   {evt.Timestamp.Hour}:{evt.Timestamp.Minute}:{evt.Timestamp.Second}.{evt.Timestamp.Millisecond} : [ {evt.Level} ]  {evt.MessageTemplate.Text} \n"
                    );
                }).Subscribe())
                .CreateLogger();

            Locator.CurrentMutable.UseSerilogFullLogger();
            
            Connector connector = new();
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}