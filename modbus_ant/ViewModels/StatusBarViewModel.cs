using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using modbus_ant.utils;

namespace modbus_ant.ViewModels;

public partial class StatusBarViewModel: ObservableRecipient, IRecipient<ConnectionState>
{
    [ObservableProperty] private string _text = "Disconnected";

    public StatusBarViewModel()
    {
        Messenger.RegisterAll(this);
    }

    public void Receive(ConnectionState message)
    {
        var tmp = IModbusTransport.GetInstance();
        Text = tmp == null ? "" 
            : $" {tmp.InitStructure.ComName}:{tmp.InitStructure.Baudrate} Addr: {tmp.InitStructure.DevId} ";
        
        switch (message.state)
        {
            case ConnectionState.State.Connected:
                Text = $"Connected [{Text}]";
                break;
            case ConnectionState.State.Init:
            case ConnectionState.State.Disconnected:
                Text = $"Disconnected [{Text}]";
                break;
            default:
                break;
        }
    }
}