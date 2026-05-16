namespace EcuCommunication;

public class ECUError
{
    public string Description { get; }
    public ushort Code { get; }
    public byte Status { get; }

    internal ECUError(string description, ushort code, byte status)
    {
        Description = description;
        Code = code;
        Status = status;
    }

    #region Overrides of Object
        
    public override string ToString()
    {
        return $"P{Code.ToString("X4")} - {Description} [{Status.ToString("X2")}]";
    }

    #endregion
}