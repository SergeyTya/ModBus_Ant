using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using modbus_ant.utils;
using Splat;

namespace modbus_ant.ViewModels;

public partial class LogPanelViewModel: ObservableRecipient, IRecipient<ConnectionState>, IEnableLogger
{
    private IDisposable? _subscription;
    
    [ObservableProperty]
    private string _textAll = "";

    public LogPanelViewModel()
    {
        Messenger.RegisterAll(this);
    }
    
    public void Receive(ConnectionState message)
    {
        if (message.state == ConnectionState.State.Init)
        {
            _subscription?.Dispose();
            var logProvider = Locator.Current.GetService<ILogProvider>();
            _subscription = logProvider?.GetObservable.Subscribe(
                (s) =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if(s.Contains("[ Information ]")) s = s.Replace("[ Information ]", "");
                        var tmp = new LogItem { Text = s, TextColor = new SolidColorBrush(Colors.Black) };
                        if (s.Contains("Error")) tmp.TextColor.Color = Colors.Red;
                        if (s.Contains("Warning")) tmp.TextColor.Color = Colors.Yellow;
                        
                        TextAll = $"{tmp.Text}{TextAll}";

                        if (TextAll.Length > 3000) Clear();
                    });
                });
        }
        
    }
    
    public partial class LogItem : ObservableObject {
        [ObservableProperty]
        string _text;

        [ObservableProperty]
        private SolidColorBrush _textColor;
    }
    
    [RelayCommand]
    public void Clear()
    {
        TextAll = "";
    }
}