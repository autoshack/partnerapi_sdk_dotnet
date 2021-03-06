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

namespace Walmart.Sdk.Base.Http
{
    class OAuthHttpHandler : Handler
    {
        private ICacheProvider _cacheProvider;
        private IAccessTokenFactory _accessTokenFactory;
        private string accessTokenCacheKey = "access_token";
        private int retriesCount = 0;
        private const int MAX_REFRESH_TOKEN_RETRY_COUNT = 3;
        public OAuthHttpHandler(IHttpConfig apiConfig, ICacheProvider cacheProvider) : base(apiConfig)
        {
            //TODO: Change retry policy?
            RetryPolicy = new Retry.LuckyMePolicy();
            _cacheProvider = cacheProvider;
            _accessTokenFactory = new AccessTokenFactory(this.Fetcher);
        }

        protected override async Task<IResponse> ExecuteAsync(IRequest request)
        {
            request.Config.AccessToken = await GetAccessToken(request.Config);
            try
            {
                return await RetryPolicy.GetResponse(Fetcher, request);
            }
            catch (InvalidAccessTokenException ex)
            {
                if (retriesCount < MAX_REFRESH_TOKEN_RETRY_COUNT)
                {
                    retriesCount++;
                    await request.RecreateHttpRequest();
                    await RefreshAccessToken(request.Config);
                    return await ExecuteAsync(request);
                }
                else
                {
                    throw NoRetriesLeftException.Factory(retriesCount,
                        new System.Exception($"Could not refresh the access token after {retriesCount} times"));
                }
               
            }
        }

        private async Task<string> RefreshAccessToken(IRequestConfig config)
        {
            await this._cacheProvider.Remove(accessTokenCacheKey);
            return await GetAccessToken(config);
        }

        private async Task<string> GetAccessToken(IRequestConfig config)
        {
            var accessToken = await this._cacheProvider.Get(accessTokenCacheKey);

            if (accessToken is null)
            {
                accessToken = await _accessTokenFactory.RetrieveAccessToken(config);
                await _cacheProvider.Set(accessTokenCacheKey, accessToken);
            }

            return accessToken.ToString();
        }
    }
}