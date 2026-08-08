namespace SS14.ChangelogTool.Tests.TestInfrastructure;

public class MockHttpMessageHandler(HttpResponseMessage responseMessage) : HttpMessageHandler
{
    public int Called { private set; get; }

    public List<string> Requests { get; } = new();

    public List<string> Urls { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var content = await request.Content?.ReadAsStringAsync(cancellationToken)!;
        Requests.Add(content);
        Urls.Add(request.RequestUri?.ToString()!);
        Called++;
        return responseMessage;
    }
}