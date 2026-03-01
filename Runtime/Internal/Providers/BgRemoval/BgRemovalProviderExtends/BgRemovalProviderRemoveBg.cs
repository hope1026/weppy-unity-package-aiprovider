using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider
{
    internal class BgRemovalProviderRemoveBg : BgRemovalProviderAbstract
    {
        internal const string DEFAULT_SIZE = "auto";

        internal override BgRemovalProviderType ProviderType => BgRemovalProviderType.REMOVE_BG;
        internal bool IsInitialized => _httpClient != null;

        internal BgRemovalProviderRemoveBg(BgRemovalProviderSettings settings_) : base(settings_, RemoveBgApiConfig.API_URL)
        {
        }

        internal override async Task<BgRemovalResponse> RemoveBackgroundAsync(
            BgRemovalRequestPayload requestPayload_,
            string model_,
            CancellationToken cancellationToken_ = default)
        {
            if (!IsInitialized)
            {
                return BgRemovalResponse.FromError("RemoveBgBgRemovalProvider not initialized. Call Initialize() first.");
            }

            if (string.IsNullOrEmpty(requestPayload_.Base64Image))
                return BgRemovalResponse.FromError("Input image is required");

            try
            {
                string size = model_ ?? _settings.DefaultModel ?? DEFAULT_SIZE;
                Dictionary<string, string> headers = RemoveBgApiConfig.GetAuthHeaders(
                    _settings.ApiKey, _settings.CustomHeaders);

                byte[] imageBytes = requestPayload_.GetImageBytes();
                if (imageBytes == null || imageBytes.Length == 0)
                    return BgRemovalResponse.FromError("Failed to decode input image");

                Dictionary<string, object> formData = new Dictionary<string, object>
                {
                    ["image_file"] = imageBytes,
                    ["size"] = size
                };

                HttpResponseResult response = await _httpClient.PostMultipartFormDataAsync(
                    _settings.BaseUrl,
                    formData,
                    headers,
                    cancellationToken_);

                if (!response.IsSuccess)
                {
                    string errorMessage = response.ErrorMessage ?? $"HTTP {response.StatusCode}: {response.Content}";
                    LogRequestError(size, _settings.BaseUrl, formData, errorMessage);
                    return BgRemovalResponse.FromError(errorMessage);
                }

                return ParseResponse(response, size);
            }
            catch (Exception ex)
            {
                LogRequestError(model_ ?? _settings.DefaultModel ?? DEFAULT_SIZE, null, null, ex.Message);
                return BgRemovalResponse.FromError(ex.Message);
            }
        }

        private void LogRequestError(string size_, string url_, Dictionary<string, object> formData_, string errorMessage_)
        {
            string formDataInfo = GetFormDataInfo(formData_);
            AIProviderLogger.LogError($"[RemoveBg] Request failed - Size: {size_}, URL: {url_ ?? "(unknown)"}, FormData: {formDataInfo}, Error: {errorMessage_}");
        }

        private string GetFormDataInfo(Dictionary<string, object> formData_)
        {
            if (formData_ == null)
                return "(null)";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (KeyValuePair<string, object> kvp in formData_)
            {
                if (!first)
                    sb.Append(", ");
                first = false;

                if (kvp.Value is byte[] bytes)
                    sb.Append($"\"{kvp.Key}\": \"(binary data, {bytes.Length} bytes)\"");
                else
                    sb.Append($"\"{kvp.Key}\": \"{kvp.Value}\"");
            }
            sb.Append("}");
            return sb.ToString();
        }

        private BgRemovalResponse ParseResponse(HttpResponseResult response_, string size_)
        {
            BgRemovalResponse result = new BgRemovalResponse
            {
                Model = size_
            };

            if (response_.RawBytes != null && response_.RawBytes.Length > 0)
            {
                result.Base64Image = Convert.ToBase64String(response_.RawBytes);

                Dictionary<string, object> rawResponse = new Dictionary<string, object>
                {
                    ["size"] = size_,
                    ["outputSize"] = response_.RawBytes.Length
                };

                if (response_.Headers != null)
                {
                    if (response_.Headers.ContainsKey("X-Credits-Charged"))
                        rawResponse["creditsCharged"] = response_.Headers["X-Credits-Charged"];
                    if (response_.Headers.ContainsKey("X-RateLimit-Remaining"))
                        rawResponse["rateLimitRemaining"] = response_.Headers["X-RateLimit-Remaining"];
                    if (response_.Headers.ContainsKey("X-RateLimit-Limit"))
                        rawResponse["rateLimitLimit"] = response_.Headers["X-RateLimit-Limit"];
                }

                result.RawResponse = rawResponse;
            }
            else if (!string.IsNullOrEmpty(response_.Content))
            {
                Dictionary<string, object> json = response_.GetJsonContent();
                if (json != null)
                {
                    if (json.ContainsKey("errors"))
                    {
                        object errorsObj = json["errors"];
                        string errorMsg = errorsObj?.ToString() ?? "Unknown error from remove.bg API";
                        return BgRemovalResponse.FromError(errorMsg);
                    }
                }
            }

            if (!result.HasImage)
            {
                return BgRemovalResponse.FromError("No image received from remove.bg API");
            }

            return result;
        }
    }
}
