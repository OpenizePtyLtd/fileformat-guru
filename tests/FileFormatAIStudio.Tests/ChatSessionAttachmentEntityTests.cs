using System;
using System.Linq;
using System.Threading.Tasks;
using FileFormatAIStudio.Data;
using FileFormatAIStudio.Data.Entities;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FileFormatAIStudio.Tests
{
    public class ChatSessionAttachmentEntityTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        public ChatSessionAttachmentEntityTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = new AppDbContext(_options);
            context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        [Fact]
        public async Task AddAttachment_PersistsInDatabaseWithAllProperties()
        {
            var session = new ChatSessionEntity
            {
                Id = Guid.NewGuid(),
                Title = "Document Q&A"
            };

            var attachment = new ChatSessionAttachmentEntity
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                FileName = "QuarterlyReport.pdf",
                FilePath = @"C:\Docs\QuarterlyReport.pdf",
                FileType = ".pdf",
                DocumentCategory = "Pdf",
                FileSize = 1048576,
                Status = "Ready",
                ParserEngineUsed = "AsposePdfParser",
                RawExtractedText = "Financial results for Q3 show 25% growth.",
                TokenCount = 120,
                ChunkCount = 1,
                AttachedAt = DateTime.UtcNow
            };

            using (var context = new AppDbContext(_options))
            {
                context.Sessions.Add(session);
                context.ChatSessionAttachments.Add(attachment);
                await context.SaveChangesAsync();
            }

            using (var verifyContext = new AppDbContext(_options))
            {
                var saved = await verifyContext.ChatSessionAttachments
                    .Include(a => a.Session)
                    .FirstOrDefaultAsync(a => a.Id == attachment.Id);

                saved.Should().NotBeNull();
                saved!.FileName.Should().Be("QuarterlyReport.pdf");
                saved.FilePath.Should().Be(@"C:\Docs\QuarterlyReport.pdf");
                saved.FileType.Should().Be(".pdf");
                saved.DocumentCategory.Should().Be("Pdf");
                saved.FileSize.Should().Be(1048576);
                saved.Status.Should().Be("Ready");
                saved.ParserEngineUsed.Should().Be("AsposePdfParser");
                saved.RawExtractedText.Should().Be("Financial results for Q3 show 25% growth.");
                saved.TokenCount.Should().Be(120);
                saved.ChunkCount.Should().Be(1);
                saved.Session.Should().NotBeNull();
                saved.Session.Id.Should().Be(session.Id);
            }
        }

        [Fact]
        public async Task DeletingChatSession_CascadeDeletesAttachments()
        {
            var session = new ChatSessionEntity
            {
                Id = Guid.NewGuid(),
                Title = "Session to Delete"
            };

            var attachment1 = new ChatSessionAttachmentEntity
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                FileName = "doc1.docx",
                FilePath = @"C:\doc1.docx",
                FileType = ".docx",
                DocumentCategory = "Word",
                Status = "Ready"
            };

            var attachment2 = new ChatSessionAttachmentEntity
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                FileName = "doc2.xlsx",
                FilePath = @"C:\doc2.xlsx",
                FileType = ".xlsx",
                DocumentCategory = "Excel",
                Status = "Ready"
            };

            using (var context = new AppDbContext(_options))
            {
                context.Sessions.Add(session);
                context.ChatSessionAttachments.AddRange(attachment1, attachment2);
                await context.SaveChangesAsync();
            }

            // Verify they exist
            using (var context = new AppDbContext(_options))
            {
                var count = await context.ChatSessionAttachments.CountAsync(a => a.SessionId == session.Id);
                count.Should().Be(2);

                // Delete session
                var sessionToDelete = await context.Sessions.FindAsync(session.Id);
                context.Sessions.Remove(sessionToDelete!);
                await context.SaveChangesAsync();
            }

            // Verify attachments are deleted
            using (var context = new AppDbContext(_options))
            {
                var count = await context.ChatSessionAttachments.CountAsync(a => a.SessionId == session.Id);
                count.Should().Be(0);
            }
        }
    }
}

