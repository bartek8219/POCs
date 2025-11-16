namespace MessagingPOC.Abstractions;

public interface IRequestReplyClient : IDisposable
{
    /// <summary>
    /// Wysyła request i czeka na odpowiedź (request-reply pattern)
    /// </summary>
    Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        string requestTopic, 
        TRequest request, 
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}