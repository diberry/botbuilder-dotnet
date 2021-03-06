﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace Microsoft.Bot.Connector
{
    /// <summary>
    /// Service client to handle requests to the Bot Framework API service.
    /// </summary>
    public class OAuthClient : ServiceClient<OAuthClient>
    {
        private readonly ConnectorClient _client;
        private readonly string _uri;

        /// <summary>
        /// The default endpoint that is used for API requests.
        /// </summary>
        public static string OAuthEndpoint { get; set; } = AuthenticationConstants.OAuthUrl;

        /// <summary>
        /// Initializes an new instance of the <see cref="OAuthClient"/> class.
        /// </summary>
        /// <param name="client">The Bot Connector REST client to use.</param>
        /// <param name="uri">The URL to use to get a token.</param>
        public OAuthClient(ConnectorClient client, string uri)
        {
            if (!(Uri.TryCreate(uri, UriKind.Absolute, out var uriResult) && uriResult.Scheme == Uri.UriSchemeHttps))
                throw new ArgumentException("Please supply a valid https uri");
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _uri = uri;
        }

        /// <summary>
        /// Gets a user token for a given user and connection.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        /// <param name="connectionName">Name of the auth connection to use.</param>
        /// <param name="magicCode">The user entered code to validate.</param>
        /// <param name="customHeaders"></param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the task completes successfully, the <see cref="TokenResponse"/> contains the user token.</remarks>
        public async Task<TokenResponse> GetUserTokenAsync(string userId, string connectionName, string magicCode, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }
            if (string.IsNullOrWhiteSpace(connectionName))
            {
                throw new ArgumentNullException(nameof(connectionName));
            }

            // Tracing
            bool shouldTrace = ServiceClientTracing.IsEnabled;
            string invocationId = null;
            if (shouldTrace)
            {
                invocationId = ServiceClientTracing.NextInvocationId.ToString();
                Dictionary<string, object> tracingParameters = new Dictionary<string, object>();
                tracingParameters.Add("userId", userId);
                tracingParameters.Add("connectionName", connectionName);
                tracingParameters.Add("magicCode", magicCode);
                tracingParameters.Add("cancellationToken", cancellationToken);
                ServiceClientTracing.Enter(invocationId, this, "GetUserTokenAsync", tracingParameters);
            }
            // Construct URL
            var tokenUrl = new Uri(new Uri(_uri + (_uri.EndsWith("/") ? "" : "/")), "api/usertoken/GetToken?userId={userId}&connectionName={connectionName}{magicCodeParam}").ToString();
            tokenUrl = tokenUrl.Replace("{connectionName}", Uri.EscapeDataString(connectionName));
            tokenUrl = tokenUrl.Replace("{userId}", Uri.EscapeDataString(userId));
            if (!string.IsNullOrEmpty(magicCode))
            {
                tokenUrl = tokenUrl.Replace("{magicCodeParam}", $"&code={Uri.EscapeDataString(magicCode)}");
            }
            else
            {
                tokenUrl = tokenUrl.Replace("{magicCodeParam}", String.Empty);
            }

            // Create HTTP transport objects
            var httpRequest = new HttpRequestMessage();
            HttpResponseMessage httpResponse = null;
            httpRequest.Method = new HttpMethod("GET");
            httpRequest.RequestUri = new Uri(tokenUrl);

            // add botframework api service url to the list of trusted service url's for these app credentials.
            MicrosoftAppCredentials.TrustServiceUrl(tokenUrl);

            // Set Credentials
            if (_client.Credentials != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _client.Credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();

            if (shouldTrace)
            {
                ServiceClientTracing.SendRequest(invocationId, httpRequest);
            }
            httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            if (shouldTrace)
            {
                ServiceClientTracing.ReceiveResponse(invocationId, httpResponse);
            }
            HttpStatusCode statusCode = httpResponse.StatusCode;
            cancellationToken.ThrowIfCancellationRequested();
            if (statusCode == HttpStatusCode.OK)
            {
                string responseContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    var tokenResponse = Rest.Serialization.SafeJsonConvert.DeserializeObject<TokenResponse>(responseContent);
                    return tokenResponse;
                }
                catch (JsonException)
                {
                    // ignore json exception and return null
                    httpRequest.Dispose();
                    if (httpResponse != null)
                    {
                        httpResponse.Dispose();
                    }
                    return null;
                }
            }
            else if (statusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Signs the user out of a connection.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        /// <param name="connectionName">Name of the auth connection to sign out of.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the task completes successfully, the response indicates whether the call to
        /// sign the user out was successful.</remarks>
        public async Task<bool> SignOutUserAsync(string userId, string connectionName = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            bool shouldTrace = ServiceClientTracing.IsEnabled;
            string invocationId = null;
            if (shouldTrace)
            {
                invocationId = ServiceClientTracing.NextInvocationId.ToString();
                Dictionary<string, object> tracingParameters = new Dictionary<string, object>();
                tracingParameters.Add("userId", userId);
                tracingParameters.Add("connectionName", connectionName);
                tracingParameters.Add("cancellationToken", cancellationToken);
                ServiceClientTracing.Enter(invocationId, this, "SignOutUserAsync", tracingParameters);
            }

            // Construct URL
            var tokenUrl = new Uri(new Uri(_uri + (_uri.EndsWith("/") ? "" : "/")), "api/usertoken/SignOut?&userId={userId}{connectionNameParam}").ToString();
            tokenUrl = tokenUrl.Replace("{connectionNameParam}", string.IsNullOrEmpty(connectionName) ? 
                String.Empty : 
                $"&connectionName={Uri.EscapeDataString(connectionName)}");
            tokenUrl = tokenUrl.Replace("{userId}", Uri.EscapeDataString(userId));
            
            // Create HTTP transport objects
            var httpRequest = new HttpRequestMessage();
            HttpResponseMessage httpResponse = null;
            httpRequest.Method = new HttpMethod("DELETE");
            httpRequest.RequestUri = new Uri(tokenUrl);

            // Set Credentials
            if (_client.Credentials != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _client.Credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();

            if (shouldTrace)
            {
                ServiceClientTracing.SendRequest(invocationId, httpRequest);
            }
            httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            if (shouldTrace)
            {
                ServiceClientTracing.ReceiveResponse(invocationId, httpResponse);
            }

            HttpStatusCode _statusCode = httpResponse.StatusCode;
            cancellationToken.ThrowIfCancellationRequested();
            if (_statusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Get the raw signin link to be sent to the user for signin for a connection name.
        /// </summary>
        /// <param name="state">A serialized and encoded parameter of a TokenExchangeState parameter.</param>
        /// <param name="finalRedirect">The endpoint URL for the final page of a succesful login attempt.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the task completes successfully and the call to the OAuth client is successful,
        /// the result contains the signin link.</remarks>
        public async Task<string> GetSignInLinkAsync(string state, string finalRedirect = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(state))
            {
                throw new ArgumentNullException(nameof(state));
            }

            bool shouldTrace = ServiceClientTracing.IsEnabled;
            string invocationId = null;
            if (shouldTrace)
            {
                invocationId = ServiceClientTracing.NextInvocationId.ToString();
                Dictionary<string, object> tracingParameters = new Dictionary<string, object>();
                tracingParameters.Add("state", state);
                tracingParameters.Add("finalRedirect", finalRedirect);
                tracingParameters.Add("cancellationToken", cancellationToken);
                ServiceClientTracing.Enter(invocationId, this, "GetSignInLinkAsync", tracingParameters);
            }

            // Construct URL
            var tokenUrl = new Uri(new Uri(_uri + (_uri.EndsWith("/") ? "" : "/")), "api/botsignin/getsigninurl?&state={state}{finalRedirectParam}").ToString();
            tokenUrl = tokenUrl.Replace("{state}", state);
            tokenUrl = tokenUrl.Replace("{finalRedirectParam}", string.IsNullOrEmpty(finalRedirect) ?
                String.Empty :
                $"&finalRedirect={Uri.EscapeDataString(finalRedirect)}");

            // add botframework api service url to the list of trusted service url's for these app credentials.
            MicrosoftAppCredentials.TrustServiceUrl(tokenUrl);

            // Create HTTP transport objects
            var httpRequest = new HttpRequestMessage();
            HttpResponseMessage httpResponse = null;
            httpRequest.Method = new HttpMethod("GET");
            httpRequest.RequestUri = new Uri(tokenUrl);

            // Set Credentials
            if (_client.Credentials != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _client.Credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();

            if (shouldTrace)
            {
                ServiceClientTracing.SendRequest(invocationId, httpRequest);
            }
            httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            if (shouldTrace)
            {
                ServiceClientTracing.ReceiveResponse(invocationId, httpResponse);
            }

            HttpStatusCode statusCode = httpResponse.StatusCode;
            cancellationToken.ThrowIfCancellationRequested();
            if (statusCode == HttpStatusCode.OK)
            {
                var link = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                return link;
            }
            return String.Empty;
        }

        /// <summary>
        /// Get the status of tokens for connections for this bot for a particular user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="includeFilter">A comma seperated list of connections to include. If null, then all connections are returned</param>
        /// <param name="customHeaders"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<TokenStatus[]> GetTokenStatusAsync(string userId, string includeFilter = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            // Tracing
            bool shouldTrace = ServiceClientTracing.IsEnabled;
            string invocationId = null;
            if (shouldTrace)
            {
                invocationId = ServiceClientTracing.NextInvocationId.ToString();
                Dictionary<string, object> tracingParameters = new Dictionary<string, object>();
                tracingParameters.Add("userId", userId);
                tracingParameters.Add("includeFilter", includeFilter);
                tracingParameters.Add("cancellationToken", cancellationToken);
                ServiceClientTracing.Enter(invocationId, this, "GetTokenStatusAsync", tracingParameters);
            }

            // Construct URL
            var tokenUrl = new Uri(new Uri(_uri + (_uri.EndsWith("/") ? "" : "/")), "api/usertoken/gettokenstatus?userId={userId}{includeFilterParam}").ToString();
            tokenUrl = tokenUrl.Replace("{userId}", Uri.EscapeDataString(userId));
            if (!string.IsNullOrEmpty(includeFilter))
            {
                tokenUrl = tokenUrl.Replace("{includeFilterParam}", $"&include={Uri.EscapeDataString(includeFilter)}");
            }
            else
            {
                tokenUrl = tokenUrl.Replace("{includeFilterParam}", String.Empty);
            }
            
            // Create HTTP transport objects
            var httpRequest = new HttpRequestMessage();
            HttpResponseMessage httpResponse = null;
            httpRequest.Method = new HttpMethod("GET");
            httpRequest.RequestUri = new Uri(tokenUrl);
            
            // Set Credentials
            if (_client.Credentials != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _client.Credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();

            if (shouldTrace)
            {
                ServiceClientTracing.SendRequest(invocationId, httpRequest);
            }

            httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            if (shouldTrace)
            {
                ServiceClientTracing.ReceiveResponse(invocationId, httpResponse);
            }

            HttpStatusCode statusCode = httpResponse.StatusCode;
            cancellationToken.ThrowIfCancellationRequested();
            if (statusCode == HttpStatusCode.OK)
            {
                string responseContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    var statuses = Rest.Serialization.SafeJsonConvert.DeserializeObject<TokenStatus[]>(responseContent);
                    return statuses;
                }
                catch (JsonException)
                {
                    // ignore json exception and return null
                    httpRequest.Dispose();
                    if (httpResponse != null)
                    {
                        httpResponse.Dispose();
                    }
                    return null;
                }
            }
            else if (statusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Send a dummy OAuth card when the bot is being used on the Emulator for testing without fetching a real token.
        /// </summary>
        /// <param name="emulateOAuthCards">Indicates whether the Emulator should emulate the OAuth card.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task SendEmulateOAuthCardsAsync(bool emulateOAuthCards)
        {
            bool shouldTrace = ServiceClientTracing.IsEnabled;
            string invocationId = null;
            if (shouldTrace)
            {
                invocationId = ServiceClientTracing.NextInvocationId.ToString();
                Dictionary<string, object> tracingParameters = new Dictionary<string, object>();
                tracingParameters.Add("emulateOAuthCards", emulateOAuthCards);
                ServiceClientTracing.Enter(invocationId, this, "SendEmulateOAuthCards", tracingParameters);
            }

            var cancellationToken = default(CancellationToken);
            // Construct URL
            var tokenUrl = new Uri(new Uri(_uri + (_uri.EndsWith("/") ? "" : "/")), "api/usertoken/emulateOAuthCards?emulate={emulate}").ToString();
            tokenUrl = tokenUrl.Replace("{emulate}", emulateOAuthCards.ToString());

            // Create HTTP transport objects
            var httpRequest = new HttpRequestMessage();
            HttpResponseMessage httpResponse = null;
            httpRequest.Method = new HttpMethod("POST");
            httpRequest.RequestUri = new Uri(tokenUrl);
            
            // add botframework api service url to the list of trusted service url's for these app credentials.
            MicrosoftAppCredentials.TrustServiceUrl(tokenUrl);

            // Set Credentials
            if (_client.Credentials != null)
            {
                await _client.Credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            }

            if (shouldTrace)
            {
                ServiceClientTracing.SendRequest(invocationId, httpRequest);
            }
            httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            if (shouldTrace)
            {
                ServiceClientTracing.ReceiveResponse(invocationId, httpResponse);
            }
        }
    }
}
