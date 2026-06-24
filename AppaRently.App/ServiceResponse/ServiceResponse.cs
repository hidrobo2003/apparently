namespace AppaRently.App.ServiceResponse;

public class ServiceResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
}