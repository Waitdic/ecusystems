using System;

namespace EcuCommunication.Protocols;

public interface IDiagProtocol
{
    bool Connected { get; }     

    TimeSpan Time { get; }

    event EventHandler OnConnect;
    event EventHandler OnDisconnect;
    event EventHandler OnDiagUpdate;
        
    DiagData GetDiagData();
}