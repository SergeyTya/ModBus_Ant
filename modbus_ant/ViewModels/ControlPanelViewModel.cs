using System.IO.Ports;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using modbus_ant.utils;
using Serilog;
using Splat;

namespace modbus_ant.ViewModels;

public partial class ControlPanelViewModel: ObservableRecipient, IEnableLogger
{
    public ControlPanelViewModel()
    {
        Messenger.RegisterAll(this);
        RTUOptions = SerialPort.GetPortNames();
    }
    
    [ObservableProperty]
    private string[]? _hwOptions = ["RTU", "TCP"];
    
    [ObservableProperty]
    private string[]? _RTUOptions;
    
    [ObservableProperty]
    private int _hwOptionSelected = 0;
    
    [ObservableProperty]
    private int _RTUOptionSelected = 0;
    
    [ObservableProperty]
    private uint _iD = 1;
    
    [ObservableProperty]
    private int[]? _rTUSpeeds = [9600, 38400, 115200, 230400, 460800, 921600];
    
    [ObservableProperty]
    private int _rTUSpeedSelected = 0;
    
    [ObservableProperty]
    private bool _isConnectRequested = false;
    
    partial void OnIsConnectRequestedChanged(bool value)
    {
        Messenger.Send(value
            ? new ConnectionState(ConnectionState.State.Connected)
            : new ConnectionState(ConnectionState.State.Disconnected));
    }

    [RelayCommand]
    void RelayCommand_SlButtonClick()
    {
        var inst = IModbusTransport.GetInstance();
        if (inst is null) return;
        inst.SlaveMode = !inst.SlaveMode;
        this.Log().Info($"Slave mode state {inst.SlaveMode}");
    }

    [RelayCommand]
    void RelayCommand_ConnectButtonClick()
    {
        
        IModbusTransport.CloseInstance();

       if (IsConnectRequested)
       {
           var initStructure = new ICAN.TransportInitStruct();
           switch (HwOptionSelected)
           {
               case 0: //RTU
                   if (RTUOptions[RTUOptionSelected] == null) return;
                   initStructure.TransportType = ICAN.TransportTypes.SerialPort;
                   initStructure.ComName = RTUOptions[RTUOptionSelected];
                   initStructure.Baudrate = (uint) RTUSpeeds[RTUSpeedSelected];
                   initStructure.DevId = ID;
                   initStructure.PollIntervalMs = 100;
                   IModbusTransport.CreateInstance(initStructure);
                   Messenger.Send(new ConnectionState(ConnectionState.State.Connected));
                   break;
               case 1:
                   
                   this.Log().Error($"MODBUS TCP NOT IMPLEMENTED");
                   IsConnectRequested = false;
                   break;
           }
           if(IModbusTransport.GetInstance() is { IsOpen: true }) { this.Log().Info("Connected");}
           else
           {
               this.Log().Error($"Connection Error");
               IsConnectRequested = false;
           }
       }else{
           Messenger.Send(new ConnectionState(ConnectionState.State.Disconnected));
           this.Log().Info($"Connection state {IsConnectRequested}");
           
       }
    }
    
}