using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FileFormatAIStudio.Data.Entities;

namespace FileFormatAIStudio.Services.Chat
{
    /// <summary>
    /// Service for managing document attachments within chat sessions,
    /// handling dynamic multi-format text extraction, token estimation, and context budgeting.
    /// </summary>
    public interface IChatAttachmentService
    {
        /// <summary>
        /// Attaches and extracts text from a document for the specified chat session.
        /// </summary>
        Task<ChatSessionAttachmentEntity> AttachDocumentAsync(
            Guid sessionId,
            string sourceFilePath,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves all attachments belonging to a chat session.
        /// </summary>
        Task<IReadOnlyList<ChatSessionAttachmentEntity>> GetSessionAttachmentsAsync(
            Guid sessionId,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves a single attachment by ID.
        /// </summary>
        Task<ChatSessionAttachmentEntity?> GetAttachmentAsync(
            Guid attachmentId,
            CancellationToken ct = default);

        /// <summary>
        /// Detaches and deletes a document attachment and its local stored file.
        /// </summary>
        Task<bool> DetachDocumentAsync(
            Guid attachmentId,
            CancellationToken ct = default);

        /// <summary>
        /// Assesses the token budget of all ready attachments in a session against the target model context window.
        /// </summary>
        Task<AttachmentBudgetResult> AssessBudgetAsync(
            Guid sessionId,
            int modelContextWindowTokens,
            int reservedPromptTokens = 2000,
            CancellationToken ct = default);

        /// <summary>
        /// Evaluates the token budget for an in-memory set of attachments.
        /// </summary>
        AttachmentBudgetResult EvaluateBudget(
            IReadOnlyList<ChatSessionAttachmentEntity> attachments,
            int modelContextWindowTokens,
            int reservedPromptTokens = 2000);
    }
}

