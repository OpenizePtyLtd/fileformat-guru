using System;
using System.Collections.Generic;

namespace FileFormatAIStudio.Services.Chat
{
    /// <summary>
    /// Details the budgeting evaluation for an individual document attachment.
    /// </summary>
    public sealed class AttachmentBudgetDetail
    {
        public Guid AttachmentId { get; init; }
        public string FileName { get; init; } = string.Empty;
        public int TokenCount { get; init; }
        public bool FitsWithinBudget { get; init; }
    }

    /// <summary>
    /// Represents the context budget evaluation for chat session attachments
    /// against an LLM's maximum token context window.
    /// </summary>
    public sealed class AttachmentBudgetResult
    {
        public int TotalTokens { get; init; }
        public int ContextWindowTokens { get; init; }
        public int ReservedPromptTokens { get; init; }
        public int UsableAttachmentBudgetTokens { get; init; }
        public bool FitsInline { get; init; }
        public bool RequiresEphemeralChunking => !FitsInline;
        public IReadOnlyList<AttachmentBudgetDetail> Details { get; init; } = Array.Empty<AttachmentBudgetDetail>();
    }
}

