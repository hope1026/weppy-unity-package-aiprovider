using UnityEngine;

namespace Weppy.AIProvider.Editor
{
    public enum AttachedFileType
    {
        IMAGE,
        PDF,
        TEXT
    }

    public class AttachedFileData
    {
        public AttachedFileType FileType { get; set; }
        public string Base64Data { get; set; }
        public string MediaType { get; set; }
        public string FileName { get; set; }
        public Texture2D Thumbnail { get; set; }
        public Texture2D SourceTexture { get; set; }
        public string TextContent { get; set; }
        public long FileSizeBytes { get; set; }

        public AttachedFileData()
        {
        }

        public AttachedFileData(AttachedFileType fileType_, string base64Data_, string mediaType_, string fileName_)
        {
            FileType = fileType_;
            Base64Data = base64Data_;
            MediaType = mediaType_;
            FileName = fileName_;
        }

        public AttachedFileData(AttachedFileType fileType_, string base64Data_, string mediaType_, string fileName_, Texture2D thumbnail_)
            : this(fileType_, base64Data_, mediaType_, fileName_)
        {
            Thumbnail = thumbnail_;
        }

        public static AttachedFileData FromImage(string base64Data_, string mediaType_, string fileName_, Texture2D thumbnail_ = null)
        {
            return new AttachedFileData(AttachedFileType.IMAGE, base64Data_, mediaType_, fileName_, thumbnail_);
        }

        public static AttachedFileData FromPdf(string base64Data_, string fileName_, long fileSizeBytes_ = 0)
        {
            return new AttachedFileData(AttachedFileType.PDF, base64Data_, "application/pdf", fileName_)
            {
                FileSizeBytes = fileSizeBytes_
            };
        }

        public static AttachedFileData FromText(string textContent_, string fileName_, string mediaType_ = "text/plain")
        {
            return new AttachedFileData(AttachedFileType.TEXT, null, mediaType_, fileName_)
            {
                TextContent = textContent_,
                FileSizeBytes = System.Text.Encoding.UTF8.GetByteCount(textContent_)
            };
        }

        public static AttachedFileData FromAttachedImageData(AttachedImageData imageData_)
        {
            return new AttachedFileData(AttachedFileType.IMAGE, imageData_.Base64Data, imageData_.MediaType, imageData_.FileName, imageData_.Thumbnail)
            {
                SourceTexture = imageData_.SourceTexture
            };
        }

        public bool IsImage => FileType == AttachedFileType.IMAGE;
        public bool IsPdf => FileType == AttachedFileType.PDF;
        public bool IsText => FileType == AttachedFileType.TEXT;
        public bool IsDocument => FileType == AttachedFileType.PDF || FileType == AttachedFileType.TEXT;

        public string GetDisplaySize()
        {
            if (FileSizeBytes <= 0)
                return "";

            if (FileSizeBytes < 1024)
                return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024)
                return $"{FileSizeBytes / 1024.0:F1} KB";
            return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
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
