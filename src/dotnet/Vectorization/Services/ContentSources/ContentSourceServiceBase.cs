﻿using FoundationaLLM.Common.Constants;
using FoundationaLLM.Vectorization.DataFormats.Office;
using FoundationaLLM.Vectorization.DataFormats.PDF;
using FoundationaLLM.Vectorization.Exceptions;

namespace FoundationaLLM.Vectorization.Services.ContentSources
{
    /// <summary>
    /// Provides common functionalities for all content sources.
    /// </summary>
    public class ContentSourceServiceBase
    {
        /// <summary>
        /// Validates a multipart unique content identifier.
        /// </summary>
        /// <param name="multipartId">The multipart identifier to validate.</param>
        /// <param name="partsCount">The required number of parts in the multipart identifier.</param>
        /// <exception cref="VectorizationException"></exception>
        public static void ValidateMultipartId(List<string> multipartId, int partsCount)
        {
            if (multipartId == null
                || multipartId.Count != partsCount
                || multipartId.Any(t => string.IsNullOrWhiteSpace(t)))
                throw new VectorizationException("Invalid multipart identifier.");
        }

        /// <summary>
        /// Reads the binary content of a specified file from the storage.
        /// </summary>
        /// <param name="fileName">The file name of the file being extracted.</param>
        /// <param name="binaryContent">The binary data of the file being extracted.</param>
        /// <returns>The string content of the file.</returns>
        /// <exception cref="VectorizationException"></exception>
        public static async Task<string> ExtractTextFromFileAsync(string fileName, BinaryData binaryContent)
        {
            await Task.CompletedTask;

            var fileExtension = Path.GetExtension(fileName);

            return fileExtension.ToLower() switch
            {
                FileExtensions.Text => binaryContent.ToString(),
                FileExtensions.Markdown => binaryContent.ToString(),
                FileExtensions.JSON => binaryContent.ToString(),
                FileExtensions.Word => DOCXTextExtractor.GetText(binaryContent),
                FileExtensions.Excel => new XLSXTextExtractor().GetText(binaryContent),
                FileExtensions.PowerPoint => PPTXTextExtractor.GetText(binaryContent),
                FileExtensions.PDF => PDFTextExtractor.GetText(binaryContent),
                _ => throw new VectorizationException($"The file type for {fileName} is not supported."),
            };
        }
    }
}