﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Walmart.Sdk.Base.Http.Exception;
using Walmart.Sdk.Base.Http.Fetcher;
using Walmart.Sdk.Base.Primitive;
using Walmart.Sdk.Base.Primitive.Config;
using Walmart.Sdk.Base.Serialization;

namespace Walmart.Sdk.Base.Http
{
    class AccessTokenFactory:IAccessTokenFactory
    {
        private IFetcher _fetcher;
        public AccessTokenFactory(IFetcher fetcher)
        {
            _fetcher = fetcher;
        }
        public async Task<string> RetrieveAccessToken(Credentials credentials)
        {
            IRequestConfig config = new BaseConfig(credentials.ClientId, credentials.ClientSecret)
            {
                ContentType = ContentTypeFormat.FORM_URLENCODED,
                ApiFormat = ApiFormat.JSON

            };

            var request = new Request(config)
            {
                EndpointUri = "/v3/token",
                Method = HttpMethod.Post,

            };

            var grantType = "client_credentials";

            request.AddPayload($"grant_type={grantType}");

            try
            {
                var response = await _fetcher.ExecuteAsync(request);

                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    // 503 Service Unavailable
                    throw new GatewayException("Service is unavailable, gateway connection error");
                }

                if (response.StatusCode == (HttpStatusCode)429)
                {
                    // 429 Too many requests
                    throw new ThrottleException("HTTP request was throttled");
                }

                var serializerFactory = new SerializerFactory();
                var serializer = serializerFactory.GetSerializer(request.Config.ApiFormat);
                var responsePayload = await response.GetPayloadAsString();
                var accessToken = serializer.Deserialize<AccessToken>(responsePayload);
                return accessToken.Token;
            }
            catch (System.Exception ex)
            {
                // unable to connect to API because of network/timeout
                throw new ConnectionException("An error occured while trying to retrieve token", ex);
            }
        }


    }

    internal interface IAccessTokenFactory
    {
        Task<string> RetrieveAccessToken(Credentials credentials);
    }
}