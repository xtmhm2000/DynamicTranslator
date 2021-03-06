using System.Text;
using System.Threading.Tasks;

using DynamicTranslator.Application.Google.Configuration;
using DynamicTranslator.Application.Orchestrators.Finders;
using DynamicTranslator.Application.Requests;
using DynamicTranslator.Configuration.Startup;
using DynamicTranslator.Constants;
using DynamicTranslator.Domain.Model;
using DynamicTranslator.Extensions;

using RestSharp;
using RestSharp.Extensions.MonoHttp;

namespace DynamicTranslator.Application.Google.Orchestration
{
    public class GoogleTranslateMeanFinder : AbstractMeanFinder<IGoogleTranslatorConfiguration, GoogleTranslateMeanOrganizer>
    {
        private const string Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
        private const string AcceptEncoding = "gzip, deflate, sdch";
        private const string AcceptLanguage = "en-US,en;q=0.8,tr;q=0.6";
        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.80 Safari/537.36";

        private readonly IApplicationConfiguration _applicationConfiguration;
        private readonly IRestClient _restClient;

        public GoogleTranslateMeanFinder(IApplicationConfiguration applicationConfiguration, IRestClient restClient)
        {
            _applicationConfiguration = applicationConfiguration;
            _restClient = restClient;
        }

        protected override async Task<TranslateResult> Find(TranslateRequest translateRequest)
        {
            string uri = string.Format(
                Configuration.Url,
                _applicationConfiguration.ToLanguage.Extension,
                _applicationConfiguration.ToLanguage.Extension,
                HttpUtility.UrlEncode(translateRequest.CurrentText, Encoding.UTF8));

            IRestResponse response = await _restClient.Manipulate(client =>
                                                      {
                                                          client.BaseUrl = uri.ToUri();
                                                          client.Encoding = Encoding.UTF8;
                                                      })
                                                      .ExecuteGetTaskAsync(
                                                          new RestRequest(Method.GET)
                                                              .AddHeader(Headers.AcceptLanguage, AcceptLanguage)
                                                              .AddHeader(Headers.AcceptEncoding, AcceptEncoding)
                                                              .AddHeader(Headers.UserAgent, UserAgent)
                                                              .AddHeader(Headers.Accept, Accept));

            var mean = new Maybe<string>();

            if (response.Ok())
            {
                mean = await MeanOrganizer.OrganizeMean(response.Content);
            }

            return new TranslateResult(true, mean);
        }
    }
}
