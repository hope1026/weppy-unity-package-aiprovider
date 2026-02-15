using System.Collections.Generic;

namespace Weppy.AIProvider.Chat
{
    /// <summary>
    /// Google-specific request options for chat payloads.
    /// </summary>
    public class GoogleChatRequestOptions
    {
        /// <summary>
        /// Safety settings entry for Google chat requests.
        /// </summary>
        public class SafetySetting
        {
            public string Category { get; set; }
            public string Threshold { get; set; }

            /// <summary>
            /// Creates an empty safety setting.
            /// </summary>
            public SafetySetting()
            {
            }

            /// <summary>
            /// Creates a safety setting with category and threshold.
            /// </summary>
            /// <param name="category_">Safety category.</param>
            /// <param name="threshold_">Threshold value.</param>
            public SafetySetting(string category_, string threshold_)
            {
                Category = category_;
                Threshold = threshold_;
            }
        }

        public int? TopK { get; set; }
        public int? CandidateCount { get; set; }
        public List<SafetySetting> SafetySettings { get; set; }
    }
}
