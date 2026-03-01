using UnityEngine;

namespace Weppy.AIProvider.Editor
{
    public class AttachedImageData
    {
        public string Base64Data { get; set; }
        public string MediaType { get; set; }
        public string FileName { get; set; }
        public Texture2D Thumbnail { get; set; }
        public Texture2D SourceTexture { get; set; }

        public AttachedImageData() { }

        public AttachedImageData(string base64Data_, string mediaType_, string fileName_, Texture2D thumbnail_ = null)
        {
            Base64Data = base64Data_;
            MediaType = mediaType_;
            FileName = fileName_;
            Thumbnail = thumbnail_;
        }

        public void DisposeThumbnail()
        {
            if (Thumbnail != null)
            {
                Object.DestroyImmediate(Thumbnail);
                Thumbnail = null;
            }
        }
    }
}
