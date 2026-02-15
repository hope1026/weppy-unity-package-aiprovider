using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider.Chat
{
    internal class HttpClientWrapper : IDisposable
    {
        private readonly HttpClient _client;
        private bool _disposed;

        public HttpClientWrapper(int timeoutSeconds_ = 60)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds_)
            };
        }

        public async Task<HttpResponseResult> PostJsonAsync(
            string url_,
            Dictionary<string, object> body_,
            Dictionary<string, string> headers_ = null,
            CancellationToken cancellationToken_ = default)
        {
            string jsonBody = JsonHelper.Serialize(body_);
            return await PostAsync(url_, jsonBody, "application/json", headers_, cancellationToken_);
        }

        public async Task<HttpResponseResult> PostJsonForBytesAsync(
            string url_,
            Dictionary<string, object> body_,
            Dictionary<string, string> headers_ = null,
            CancellationToken cancellationToken_ = default)
        {
            try
            {
                string jsonBody = JsonHelper.Serialize(body_);

                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url_))
                {
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    ApplyHeaders(request, headers_);

                    using (HttpResponseMessage response = await _client.SendAsync(request, cancellationToken_))
                    {
                        byte[] rawBytes = await response.Content.ReadAsByteArrayAsync();
                        string contentType = response.Content.Headers.ContentType?.MediaType ?? "";

                        HttpResponseResult result = new HttpResponseResult
                        {
                            IsSuccess = response.IsSuccessStatusCode,
                            StatusCode = (int)response.StatusCode,
                            RawBytes = rawBytes,
                            Headers = GetResponseHeaders(response)
                        };

                        if (contentType.Contains("application/json") || contentType.Contains("text/"))
                        {
                            result.Content = Encoding.UTF8.GetString(rawBytes);
                        }

                        return result;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return new HttpResponseResult
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    ErrorMessage = "Request timed out or was cancelled"
                };
            }
            catch (Exception ex)
            {
                return new HttpResponseResult
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<HttpResponseResult> PostAsync(
            string url_,
            string body_,
            string contentType_,
            Dictionary<string, string> headers_ = null,
            CancellationToken cancellationToken_ = default)
        {
            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url_))
                {
                    request.Content = new StringContent(body_, Encoding.UTF8, contentType_);

                    ApplyHeaders(request, headers_);

                    using (HttpResponseMessage response = await _client.SendAsync(request, cancellationToken_))
                    {
                        string content = await response.Content.ReadAsStringAsync();

                        return new HttpResponseResult
                        {
                            IsSuccess = response.IsSuccessStatusCode,
                            StatusCode = (int)response.StatusCode,
                            Content = content,
                            Headers = GetResponseHeaders(response)
                        };
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return new HttpResponseResult
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    ErrorMessage = "Request timed out or was cancelled"
                };
            }
            catch (Exception ex)
            {
                return new HttpResponseResult
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<HttpResponseResult> GetAsync(
            string url_,
            Dictionary<string, string> headers_ = null,
            CancellationToken cancellationToken_ = default)
        {
            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url_))
                {
                    ApplyHeaders(request, headers_);

                    using (HttpResponseMessage response = await _client.SendAsync(request, cancellationToken_))
                    {
                        string content = await response.Content.ReadAsStringAsync();

                        return new HttpResponseResult
                        {
                            IsSuccess = response.IsSuccessStatusCode,
                            StatusCode = (int)response.StatusCode,
                            Content = content,
                            Headers = GetResponseHeaders(response)
                        };
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return new HttpResponseResult
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    ErrorMessage = "Request timed out or was cancelled"
                };
            }
            catch (Exception ex)
            {
                return new HttpResponseResult
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task PostStreamWithCallbackAsync(
            string url_,
            Dictionary<string, object> body_,
            Action<string> onLineReceived_,
            Dictionary<string, string> headers_ = null,
            CancellationToken cancellationToken_ = default)
        {
            if (onLineReceived_ == null)
                return;

            string jsonBody = JsonHelper.Serialize(body_);

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url_))
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                ApplyHeaders(request, headers_);

                using (HttpResponseMessage response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken_))
                {
                    Stream stream = await response.Content.ReadAsStreamAsync();
                    try
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            while (!reader.EndOfStream && !cancellationToken_.IsCancellationRequested)
                            {
                                string line = await reader.ReadLineAsync();
                                if (!string.IsNullOrEmpty(line))
                                    onLineReceived_(line);
                            }
                        }
                    }
                    finally
                    {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
                        await stream.DisposeAsync();
#else
                        stream.Dispose();
#endif
                    }
                }
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        public async IAsyncEnumerable<string> PostStreamAsync(
            string url_,
            Dictionary<string, object> body_,
            Dictionary<string, string> headers_ = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken_ = default)
        {
            string jsonBody = JsonHelper.Serialize(body_);

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url_))
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                ApplyHeaders(request, headers_);

                using (HttpResponseMessage response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken_))
                {
                    await using (Stream stream = await response.Content.ReadAsStreamAsync())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream && !cancellationToken_.IsCancellationRequested)
                        {
                            string line = await reader.ReadLineAsync();
                            if (!string.IsNullOrEmpty(line))
                                yield return line;
                        }
                    }
                }
            }
        }
#endif

        public async Task<HttpResponseResult> PostBytesForBytesAsync(
            string url_,
            byte[] body_,
            string contentType_,
            Dictionary<string, string> headers_ = null,
            CancellationToken cancellationToken_ = default)
        {
            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url_))
                {
                    request.Content = new ByteArrayContent(body_);
                    request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType_);
                    ApplyHeaders(request, headers_);

                    using (HttpResponseMessage response = await _client.SendAsync(request, cancellationToken_))
                    {
                        byte[] rawBytes = await response.Content.ReadAsByteArrayAsync();
                        string responseContentType = response.Content.Headers.ContentType?.MediaType ?? "";

                        HttpResponseResult result = new HttpResponseResult
                        {
                            IsSuccess = response.IsSuccessStatusCode,
                            StatusCode = (int)response.StatusCode,
                            RawBytes = rawBytes,
                            Headers = GetResponseHeaders(response)
                        };

                        if (responseContentType.Contains("application/json") || responseContentType.Contains("text/"))
                        {
                            result.Content = Encoding.UTF8.GetString(rawBytes);
                        }

                        return result;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return new HttpResponseResult
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    ErrorMessage = "Request timed out or was cancelled"
                };
            }
            catch (Exception ex)
            {
                return new HttpResponseResult
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<byte[]> DownloadBytesAsync(
            string url_,
            Dictionary<string, string> headers_ = null,
            CancellationToken cancellationToken_ = default)
        {
            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url_))
                {
                    ApplyHeaders(request, headers_);

                    using (HttpResponseMessage response = await _client.SendAsync(request, cancellationToken_))
                    {
                        if (response.IsSuccessStatusCode)
                            return await response.Content.ReadAsByteArrayAsync();

                        return null;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<HttpResponseResult> PostMultipartFormDataAsync(
            string url_,
            Dictionary<string, object> formData_,
            Dictionary<string, string> headers_ = null,
            CancellationToken cancellationToken_ = default)
        {
            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url_))
                using (MultipartFormDataContent content = new MultipartFormDataContent())
                {
                    if (formData_ != null)
                    {
                        foreach (KeyValuePair<string, object> field in formData_)
                        {
                            if (field.Value is byte[] bytes)
                            {
                                ByteArrayContent byteContent = new ByteArrayContent(bytes);
                                byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                                content.Add(byteContent, field.Key, "file");
                            }
                            else if (field.Value != null)
                            {
                                content.Add(new StringContent(field.Value.ToString()), field.Key);
                            }
                        }
                    }

                    request.Content = content;
                    ApplyHeaders(request, headers_);

                    using (HttpResponseMessage response = await _client.SendAsync(request, cancellationToken_))
                    {
                        byte[] rawBytes = await response.Content.ReadAsByteArrayAsync();
                        string responseContentType = response.Content.Headers.ContentType?.MediaType ?? "";

                        HttpResponseResult result = new HttpResponseResult
                        {
                            IsSuccess = response.IsSuccessStatusCode,
                            StatusCode = (int)response.StatusCode,
                            RawBytes = rawBytes,
                            Headers = GetResponseHeaders(response)
                        };

                        if (responseContentType.Contains("application/json") || responseContentType.Contains("text/"))
                        {
                            result.Content = Encoding.UTF8.GetString(rawBytes);
                        }

                        return result;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return new HttpResponseResult
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    ErrorMessage = "Request timed out or was cancelled"
                };
            }
            catch (Exception ex)
            {
                return new HttpResponseResult
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    ErrorMessage = ex.Message
                };
            }
        }

        private void ApplyHeaders(HttpRequestMessage request_, Dictionary<string, string> headers_)
        {
            if (headers_ == null)
                return;

            foreach (KeyValuePair<string, string> header in headers_)
                request_.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        private Dictionary<string, string> GetResponseHeaders(HttpResponseMessage response_)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();

            foreach (KeyValuePair<string, IEnumerable<string>> header in response_.Headers)
                headers[header.Key] = string.Join(", ", header.Value);

            if (response_.Content?.Headers != null)
            {
                foreach (KeyValuePair<string, IEnumerable<string>> header in response_.Content.Headers)
                    headers[header.Key] = string.Join(", ", header.Value);
            }

            return headers;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _client?.Dispose();
            _disposed = true;
        }
    }
}
