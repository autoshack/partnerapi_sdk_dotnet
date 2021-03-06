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
    class AccessTokenFactory : IAccessTokenFactory
    {
        private IFetcher _fetcher;

        public AccessTokenFactory(IFetcher fetcher)
        {
            _fetcher = fetcher;
        }

        public async Task<string> RetrieveAccessToken(IRequestConfig config)
        {
            IRequestConfig tokenRequestConfig = new BaseConfig
            {
                Credentials = new Credentials()
                {
                    Id=config.Credentials.Id,
                    Secret = config.Credentials.Secret
                },
                ContentType = ContentTypeFormat.FORM_URLENCODED,
                ApiFormat = ApiFormat.JSON,
                ServiceName = config.ServiceName,
                AuthType = AuthenticationType.OAuth
            };

            var request = new OAuthRequest(tokenRequestConfig)
            {
                EndpointUri = "/v3/token",
                Method = HttpMethod.Post,
            };

            var grantType = "client_credentials";

            request.AddPayload($"grant_type={grantType}");

            var response = await _fetcher.ExecuteAsync(request);
            var serializerFactory = new SerializerFactory();
            var serializer = serializerFactory.GetSerializer(request.Config.ApiFormat);
            var responsePayload = await response.GetPayloadAsString();
            var accessToken = serializer.Deserialize<AccessToken>(responsePayload);

            return accessToken.Token;
        }
    }

    internal interface IAccessTokenFactory
    {
        Task<string> RetrieveAccessToken(IRequestConfig config);
    }
}