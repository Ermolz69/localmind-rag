using System;

namespace LocalMind.ApiGateway.Domain.Exceptions;

public class GatewayException : Exception
{
    public GatewayException(string message) : base(message)
    {
    }

    public GatewayException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
