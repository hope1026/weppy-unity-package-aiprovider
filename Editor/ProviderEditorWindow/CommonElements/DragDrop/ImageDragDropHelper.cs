using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public static class ImageDragDropHelper
    {
        public static void SetupFileDragAndDrop(
            VisualElement dropArea_,
            Action<string> onFileDropped_,
            string dragOverClass_ = "drag-over")
        {
            if (dropArea_ == null)
                return;

            dropArea_.RegisterCallback<DragEnterEvent>(evt =>
            {
                if (HasImageFileInDrag())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    dropArea_.AddToClassList(dragOverClass_);
                }
            });

            dropArea_.RegisterCallback<DragLeaveEvent>(evt =>
            {
                dropArea_.RemoveFromClassList(dragOverClass_);
            });

            dropArea_.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (HasImageFileInDrag())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }
            });

            dropArea_.RegisterCallback<DragPerformEvent>(evt =>
            {
                dropArea_.RemoveFromClassList(dragOverClass_);

                if (HasTexture2DInDrag())
                {
                    DragAndDrop.AcceptDrag();
                    return;
                }

                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    foreach (string path in DragAndDrop.paths)
                    {
                        if (ImageHelper.IsImageFilePath(path) || IsSupportedDocumentFile(path))
                        {
                            onFileDropped_?.Invoke(path);
                        }
                    }
                }

                DragAndDrop.AcceptDrag();
            });
        }

        public static void SetupFileDragAndDropSingle(
            VisualElement dropArea_,
            Action<string> onFileDropped_,
            string dragOverClass_ = "drag-over")
        {
            if (dropArea_ == null)
                return;

            dropArea_.RegisterCallback<DragEnterEvent>(evt =>
            {
                if (HasImageFileInDrag())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    dropArea_.AddToClassList(dragOverClass_);
                }
            });

            dropArea_.RegisterCallback<DragLeaveEvent>(evt =>
            {
                dropArea_.RemoveFromClassList(dragOverClass_);
            });

            dropArea_.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (HasImageFileInDrag())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }
            });

            dropArea_.RegisterCallback<DragPerformEvent>(evt =>
            {
                dropArea_.RemoveFromClassList(dragOverClass_);

                if (HasTexture2DInDrag())
                {
                    DragAndDrop.AcceptDrag();
                    return;
                }

                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    foreach (string path in DragAndDrop.paths)
                    {
                        if (ImageHelper.IsImageFilePath(path))
                        {
                            onFileDropped_?.Invoke(path);
                            break;
                        }
                    }
                }

                DragAndDrop.AcceptDrag();
            });
        }

        public static void SetupTextureDragAndDrop(
            VisualElement dropArea_,
            Action<Texture2D> onTextureDropped_,
            string dragOverClass_ = "drag-over")
        {
            if (dropArea_ == null)
                return;

            dropArea_.RegisterCallback<DragEnterEvent>(evt =>
            {
                if (HasTexture2DInDrag())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    dropArea_.AddToClassList(dragOverClass_);
                }
            });

            dropArea_.RegisterCallback<DragLeaveEvent>(evt =>
            {
                dropArea_.RemoveFromClassList(dragOverClass_);
            });

            dropArea_.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (HasTexture2DInDrag())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }
            });

            dropArea_.RegisterCallback<DragPerformEvent>(evt =>
            {
                dropArea_.RemoveFromClassList(dragOverClass_);

                foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                {
                    if (obj is Texture2D texture)
                    {
                        onTextureDropped_?.Invoke(texture);
                    }
                }

                DragAndDrop.AcceptDrag();
            });
        }

        public static void SetupTextureDragAndDropSingle(
            VisualElement dropArea_,
            Action<Texture2D> onTextureDropped_,
            string dragOverClass_ = "drag-over")
        {
            if (dropArea_ == null)
                return;

            dropArea_.RegisterCallback<DragEnterEvent>(evt =>
            {
                if (HasTexture2DInDrag())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    dropArea_.AddToClassList(dragOverClass_);
                }
            });

            dropArea_.RegisterCallback<DragLeaveEvent>(evt =>
            {
                dropArea_.RemoveFromClassList(dragOverClass_);
            });

            dropArea_.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (HasTexture2DInDrag())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }
            });

            dropArea_.RegisterCallback<DragPerformEvent>(evt =>
            {
                dropArea_.RemoveFromClassList(dragOverClass_);

                foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                {
                    if (obj is Texture2D texture)
                    {
                        onTextureDropped_?.Invoke(texture);
                        break;
                    }
                }

                DragAndDrop.AcceptDrag();
            });
        }

        public static bool HasImageFileInDrag()
        {
            if (DragAndDrop.paths == null || DragAndDrop.paths.Length == 0)
                return false;

            foreach (string path in DragAndDrop.paths)
            {
                if (ImageHelper.IsSupportedImageExtension(Path.GetExtension(path)))
                    return true;
                if (IsSupportedDocumentFile(path))
                    return true;
            }

            return false;
        }

        public static bool IsSupportedDocumentFile(string path_)
        {
            if (string.IsNullOrEmpty(path_) || !File.Exists(path_))
                return false;

            string extension = Path.GetExtension(path_).ToLowerInvariant();
            return extension == ".pdf" ||
                   extension == ".txt" ||
                   extension == ".md" ||
                   extension == ".json" ||
                   extension == ".xml" ||
                   extension == ".csv" ||
                   extension == ".yaml" ||
                   extension == ".yml" ||
                   extension == ".cs" ||
                   extension == ".js" ||
                   extension == ".ts" ||
                   extension == ".py" ||
                   extension == ".html" ||
                   extension == ".css" ||
                   extension == ".sql" ||
                   extension == ".sh" ||
                   extension == ".bat" ||
                   extension == ".log";
        }

        public static bool HasTexture2DInDrag()
        {
            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0)
                return false;

            foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
            {
                if (obj is Texture2D)
                    return true;
            }

            return false;
        }
    }
}
