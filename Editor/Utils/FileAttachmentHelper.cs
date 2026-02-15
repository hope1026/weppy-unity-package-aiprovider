using System;
using System.IO;
using UnityEngine;

namespace Weppy.AIProvider.Chat.Editor
{
    public static class FileAttachmentHelper
    {
        private static readonly string[] SUPPORTED_IMAGE_EXTENSIONS = { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
        private static readonly string[] SUPPORTED_PDF_EXTENSIONS = { ".pdf" };
        private static readonly string[] SUPPORTED_TEXT_EXTENSIONS = { ".txt", ".md", ".json", ".xml", ".csv", ".yaml", ".yml", ".cs", ".js", ".ts", ".py", ".html", ".css", ".sql", ".sh", ".bat", ".log", ".ini", ".cfg", ".conf" };

        public const long MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;
        public const string FILE_DIALOG_FILTER = "Supported Files (*.png;*.jpg;*.jpeg;*.gif;*.webp;*.pdf;*.txt;*.md;*.json;*.xml;*.csv;*.cs;*.js;*.ts;*.py)|*.png;*.jpg;*.jpeg;*.gif;*.webp;*.pdf;*.txt;*.md;*.json;*.xml;*.csv;*.cs;*.js;*.ts;*.py|All Files (*.*)|*.*";
        public const string FILE_DIALOG_EXTENSIONS = "png,jpg,jpeg,gif,webp,pdf,txt,md,json,xml,csv,cs,js,ts,py,html,css,sql,sh,bat,log,yaml,yml,ini,cfg,conf";

        public static bool IsSupportedFile(string filePath_)
        {
            if (string.IsNullOrEmpty(filePath_) || !File.Exists(filePath_))
                return false;

            string extension = Path.GetExtension(filePath_).ToLowerInvariant();
            return IsImageExtension(extension) || IsPdfExtension(extension) || IsTextExtension(extension);
        }

        public static bool IsImageExtension(string extension_)
        {
            string ext = NormalizeExtension(extension_);
            foreach (string supported in SUPPORTED_IMAGE_EXTENSIONS)
            {
                if (ext == supported)
                    return true;
            }
            return false;
        }

        public static bool IsPdfExtension(string extension_)
        {
            string ext = NormalizeExtension(extension_);
            foreach (string supported in SUPPORTED_PDF_EXTENSIONS)
            {
                if (ext == supported)
                    return true;
            }
            return false;
        }

        public static bool IsTextExtension(string extension_)
        {
            string ext = NormalizeExtension(extension_);
            foreach (string supported in SUPPORTED_TEXT_EXTENSIONS)
            {
                if (ext == supported)
                    return true;
            }
            return false;
        }

        public static AttachedFileType GetFileType(string filePath_)
        {
            string extension = Path.GetExtension(filePath_).ToLowerInvariant();

            if (IsImageExtension(extension))
                return AttachedFileType.IMAGE;
            if (IsPdfExtension(extension))
                return AttachedFileType.PDF;
            if (IsTextExtension(extension))
                return AttachedFileType.TEXT;

            throw new NotSupportedException($"File type not supported: {extension}");
        }

        public static string GetMediaType(string extension_)
        {
            string ext = NormalizeExtension(extension_);

            return ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".md" => "text/markdown",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".csv" => "text/csv",
                ".yaml" => "text/yaml",
                ".yml" => "text/yaml",
                ".cs" => "text/x-csharp",
                ".js" => "text/javascript",
                ".ts" => "text/typescript",
                ".py" => "text/x-python",
                ".html" => "text/html",
                ".css" => "text/css",
                ".sql" => "text/x-sql",
                ".sh" => "text/x-shellscript",
                ".bat" => "text/x-batch",
                ".log" => "text/plain",
                ".ini" => "text/plain",
                ".cfg" => "text/plain",
                ".conf" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        public static AttachedFileData LoadFile(string filePath_)
        {
            if (string.IsNullOrEmpty(filePath_))
                throw new ArgumentNullException(nameof(filePath_));

            if (!File.Exists(filePath_))
                throw new FileNotFoundException("File not found", filePath_);

            FileInfo fileInfo = new FileInfo(filePath_);
            if (fileInfo.Length > MAX_FILE_SIZE_BYTES)
                throw new InvalidOperationException($"File size exceeds maximum allowed size of {MAX_FILE_SIZE_BYTES / (1024 * 1024)}MB");

            string extension = Path.GetExtension(filePath_).ToLowerInvariant();
            string fileName = Path.GetFileName(filePath_);
            string mediaType = GetMediaType(extension);
            AttachedFileType fileType = GetFileType(filePath_);

            switch (fileType)
            {
                case AttachedFileType.IMAGE:
                    return LoadImageFile(filePath_, fileName, mediaType, fileInfo.Length);

                case AttachedFileType.PDF:
                    return LoadPdfFile(filePath_, fileName, fileInfo.Length);

                case AttachedFileType.TEXT:
                    return LoadTextFile(filePath_, fileName, mediaType, fileInfo.Length);

                default:
                    throw new NotSupportedException($"File type not supported: {extension}");
            }
        }

        private static AttachedFileData LoadImageFile(string filePath_, string fileName_, string mediaType_, long fileSize_)
        {
            Texture2D texture = ImageHelper.LoadTextureFromFile(filePath_);
            string base64Data = ImageHelper.EncodeToBase64(texture, mediaType_ == "image/png");
            Texture2D thumbnail = ImageHelper.CreateThumbnail(texture, 50, 50);

            UnityEngine.Object.DestroyImmediate(texture);

            return new AttachedFileData(AttachedFileType.IMAGE, base64Data, mediaType_, fileName_, thumbnail)
            {
                FileSizeBytes = fileSize_
            };
        }

        private static AttachedFileData LoadPdfFile(string filePath_, string fileName_, long fileSize_)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath_);
            string base64Data = Convert.ToBase64String(fileBytes);

            return AttachedFileData.FromPdf(base64Data, fileName_, fileSize_);
        }

        private static AttachedFileData LoadTextFile(string filePath_, string fileName_, string mediaType_, long fileSize_)
        {
            string textContent = File.ReadAllText(filePath_);

            return new AttachedFileData(AttachedFileType.TEXT, null, mediaType_, fileName_)
            {
                TextContent = textContent,
                FileSizeBytes = fileSize_
            };
        }

        public static Texture2D GetFileTypeIcon(AttachedFileType fileType_)
        {
            return null;
        }

        public static string GetFileTypeDisplayName(AttachedFileType fileType_)
        {
            return fileType_ switch
            {
                AttachedFileType.IMAGE => "Image",
                AttachedFileType.PDF => "PDF",
                AttachedFileType.TEXT => "Text",
                _ => "File"
            };
        }

        public static bool IsProviderSupported(ChatProviderType providerType_, AttachedFileType fileType_)
        {
            switch (providerType_)
            {
                case ChatProviderType.OPEN_AI:
                    return fileType_ == AttachedFileType.IMAGE;

                case ChatProviderType.GOOGLE:
                case ChatProviderType.ANTHROPIC:
                    return true;

                default:
                    return fileType_ == AttachedFileType.IMAGE;
            }
        }

        public static string GetUnsupportedFileTypeMessage(ChatProviderType providerType_, AttachedFileType fileType_)
        {
            if (providerType_ == ChatProviderType.OPEN_AI && fileType_ != AttachedFileType.IMAGE)
            {
                return "OpenAI only supports image attachments. PDF and text files are not supported.";
            }

            return null;
        }

        private static string NormalizeExtension(string extension_)
        {
            if (string.IsNullOrEmpty(extension_))
                return "";

            string ext = extension_.ToLowerInvariant();
            if (!ext.StartsWith("."))
                ext = "." + ext;

            return ext;
        }
    }
}
