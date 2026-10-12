namespace ChillerCoolingSystem_CCS_.Monitoring
{
    public sealed record PlcTagValue(string TagId, double? Value, bool IsGood, DateTime? Timestamp = null, string? Error = null);

    public sealed class PlcConnectionException : Exception
    {
        public PlcConnectionException(string message, Exception? inner = null) : base(message, inner)
        {
        }
    }
}
