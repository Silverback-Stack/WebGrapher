using Requests.Core;
using Requests.Infrastructure.Adapters.HttpClient;
using System;

namespace Requests.Factories
{
    public class RequestsConfig
    {
        public RequestSenderSettings RequestSender { get; set; } = new RequestSenderSettings();

        public TransportProvider Provider { get; set; } = TransportProvider.HttpClient;

        public HttpClientSettings HttpClient { get; set; } = new HttpClientSettings();

    }

}
