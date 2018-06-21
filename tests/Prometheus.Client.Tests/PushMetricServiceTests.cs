using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Prometheus.Client.Collectors;
using Xunit;

namespace Prometheus.Client.Tests
{
    public class MetricPushServiceTest : BaseMetricPushServiceTest
    {
        private MockedMetricPushService _pushService;
        private ICollectorRegistry _metrics;
        private MetricFactory _metricFactory;
        private string _result;
        public override void Act()
        {
            _pushService.PushAsync(_metrics.CollectAll(), "http://localhost:9091",
                "pushgateway", Environment.MachineName, null).GetAwaiter().GetResult();
        }

        public override void Arrange()
        {
            _metrics = new CollectorRegistry();
            _metricFactory = new MetricFactory(_metrics);
            var counter = _metricFactory.CreateCounter("test_counter", "just a simple test counter", "Color", "Size");
            counter.Labels("White", "XXS").Inc();
            counter.Labels("Black", "XXL").Inc();

            _pushService = new MockedMetricPushService();
            _pushService.Handler.SetMessageHandler((r) =>
            {
                var streamContent = r.Content as StreamContent;
                _result = streamContent.ReadAsStringAsync().GetAwaiter().GetResult();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });
        }

        [Fact]
        public void should_send_metrics_and_get_success()
        {
            var memoryStream = new MemoryStream();
            ScrapeHandler.ProcessScrapeRequest(_metrics.CollectAll(), null, memoryStream);
            var expectedResult = Encoding.UTF8.GetString(memoryStream.ToArray());
            Assert.Equal(expectedResult, _result);
        }
    }

    public abstract class BaseMetricPushServiceTest
    {
        protected BaseMetricPushServiceTest()
        {
            Arrange();
            Act();
        }

        public abstract void Act();
        public abstract void Arrange();
    }

    public class MockedMetricPushService : MetricPushService
    {
        public MockedMetricPushService() : base() { }
        protected override HttpMessageHandler MessageHandler => Handler;
        public MessageHandler Handler { get; } = new MessageHandler();
    }

    public class MessageHandler : HttpMessageHandler
    {
        private Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;
        public void SetMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return await _handler(request);
        }
    }
}
