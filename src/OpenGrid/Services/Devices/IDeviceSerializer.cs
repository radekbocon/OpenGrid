namespace OpenGrid.Models;

public interface IDeviceSerializer
{
    string ToJson(IDevice device);
    IDevice? FromJson(string json);
}