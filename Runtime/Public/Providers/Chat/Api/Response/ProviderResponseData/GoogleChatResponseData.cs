using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Google-specific response metadata for chat responses.
    /// </summary>
    public class GoogleChatResponseData
    {
        /// <summary>
        /// Safety rating details returned by Google.
        /// </summary>
        public class SafetyRating
        {
            public string Category { get; set; }
            public string Probability { get; set; }
            public bool Blocked { get; set; }

            /// <summary>
            /// Creates an empty safety rating.
            /// </summary>
            public SafetyRating()
            {
            }

            /// <summary>
            /// Creates a safety rating with category and probability.
            /// </summary>
            /// <param name="category_">Safety category.</param>
            /// <param name="probability_">Probability level.</param>
            /// <param name="blocked_">Whether content was blocked.</param>
            public SafetyRating(string category_, string probability_, bool blocked_ = false)
            {
                Category = category_;
                Probability = probability_;
                Blocked = blocked_;
            }
        }

        /// <summary>
        /// Prompt feedback information from Google.
        /// </summary>
        public class PromptFeedbackData
        {
            public string BlockReason { get; set; }
            public List<SafetyRating> SafetyRatings { get; set; }

            /// <summary>
            /// Creates an empty prompt feedback instance.
            /// </summary>
            public PromptFeedbackData()
            {
            }

            /// <summary>
            /// Creates prompt feedback with block reason and safety ratings.
            /// </summary>
            /// <param name="blockReason_">Block reason.</param>
            /// <param name="safetyRatings_">Safety ratings list.</param>
            public PromptFeedbackData(string blockReason_, List<SafetyRating> safetyRatings_ = null)
            {
                BlockReason = blockReason_;
                SafetyRatings = safetyRatings_;
            }
        }

        public List<SafetyRating> SafetyRatings { get; set; }
        public PromptFeedbackData PromptFeedback { get; set; }
    }
}
