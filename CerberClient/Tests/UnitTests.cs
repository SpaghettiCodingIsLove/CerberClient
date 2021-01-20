using CerberClient.Model.Api;
using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;
using Xunit;

namespace CerberClient.Tests
{
    public class UnitTests
    {
        private static RestClient restClient = new RestClient("http://localhost:4000/");

        [Theory]
        [MemberData(nameof(GetLogins))]
        public void TestLogin(AuthenticateRequest model)
        {
            RestRequest request = new RestRequest("Account/authenticate", Method.POST);
            request.RequestFormat = RestSharp.DataFormat.Json;
            request.AddJsonBody(model);
            IRestResponse response = restClient.Execute(request);
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK);
        }

        [Theory]
        [MemberData(nameof(GetTokens))]
        public void ExtendTokens(ExtendTokenRequest token)
        {
            RestRequest request = new RestRequest("Account/refresh-token", Method.POST);
            request.RequestFormat = RestSharp.DataFormat.Json;
            request.AddJsonBody(token);
            IRestResponse response = restClient.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                ExtendTokenResponse responseBody = JsonConvert.DeserializeObject<ExtendTokenResponse>(response.Content);
                Assert.True(responseBody.Success);
            }
            else
            {
                Assert.True(false);
            }
        }

        public static IEnumerable<object[]> GetLogins
        {
            get
            {
                yield return new object[]
                {
                    new AuthenticateRequest
                    {
                        Email = "www@wp.pl",
                        Password = "1234"
                    }
                };
                yield return new object[]
                {
                    new AuthenticateRequest
                    {
                        Email = "wrongMail@wp.pl",
                        Password = "1234"
                    }
                };
                yield return new object[] {
                    new AuthenticateRequest
                    {
                        Email = "www@wp.pl",
                        Password = "wrongPass"
                    }
                };
            }
        }
        public static IEnumerable<object[]> GetTokens
        {
            get
            {
                AuthenticateRequest model = new AuthenticateRequest
                {
                    Email = "www@wp.pl",
                    Password = "1234"
                };
                RestRequest request = new RestRequest("Account/authenticate", Method.POST);
                request.RequestFormat = RestSharp.DataFormat.Json;
                request.AddJsonBody(model);
                IRestResponse response = restClient.Execute(request);
                AuthenticateResponse authenticateResponse = JsonConvert.DeserializeObject<AuthenticateResponse>(response.Content);
                
                yield return new object[]
                {
                    new ExtendTokenRequest
                    {
                        Token = authenticateResponse.RefreshToken,
                        Id = authenticateResponse.Id
                    }
                };
                yield return new object[]
                {
                    new ExtendTokenRequest
                    {
                        Token = authenticateResponse.RefreshToken,
                        Id = 1
                    }
                };
                yield return new object[] {
                    new ExtendTokenRequest
                    {
                        Token = "wrongtoken",
                        Id = authenticateResponse.Id
                    }
                };
                yield return new object[] {
                    new ExtendTokenRequest
                    {
                        Token = "wrongtoken",
                        Id = 7
                    }
                };
            }
        }
    }
}
