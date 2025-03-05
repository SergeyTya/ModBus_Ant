using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using modbus_ant.utils;
using Splat;

namespace modbus_ant.ViewModels;

public record ConnectionState(ConnectionState.State state) {
    public enum State { 
        Init,
        Connected,
        Disconnected,
        Reset
    }
};

public class Connector : ObservableRecipient
{
    public Connector()
    {
        Messenger.RegisterAll(this);
    }

    // public void Receive(ConnectionState message)
    // {
    //     switch (message.state)
    //     {
    //         case ConnectionState.State.Init:
    //             break;
    //         case ConnectionState.State.Connected:
    //             ICAN.CloseCurrent();
    //             ICAN.Create(new ICAN.TransportInitStruct(), ICAN.CANType.ModbusRTU);
    //             break;
    //         case ConnectionState.State.Disconnected:
    //             ICAN.CloseCurrent();
    //             break;
    //         case ConnectionState.State.Reset:
    //             break;
    //         default:
    //             break;
    //     }
    // }
}
