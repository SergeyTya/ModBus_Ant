using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace modbus_ant.ViewModels;

public class MainWindowViewModel : ObservableRecipient, IRecipient<ConnectionState>
{
    public MainWindowViewModel()
    {
        Messenger.RegisterAll(this);
        Messenger.Send(new ConnectionState(ConnectionState.State.Init));
        

    }

    public void Receive(ConnectionState message)
    {
      //  throw new System.NotImplementedException();
    }
}