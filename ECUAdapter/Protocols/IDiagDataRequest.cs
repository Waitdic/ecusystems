namespace EcuCommunication.Protocols
{
    internal interface IDiagDataRequest
    {
        DiagData DiagData { get; }
        bool IsOnline { get; }
    }
}
