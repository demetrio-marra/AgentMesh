using System.ComponentModel.DataAnnotations;
using AgentMeshWeb.Configuration;
using AgentMeshWeb.Models;
using AgentMeshWeb.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AgentMeshWeb.Tests;

public class ChatStateTests
{
    [Fact]
    public async Task StoreLoadsSavesRejectsStaleRevisionAndClears()
    {
        var store = CreateStore();
        var empty = await store.LoadAsync("chat-1", CancellationToken.None);
        var messages = new[] { new ContextMessage(ChatMessageRole.User, DateTime.UnixEpoch, "hello") };
        var saved = await store.SaveAsync("chat-1", empty.Revision, new ChatContextSnapshot(0, messages, 4, 1.25m), CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal(1, saved.Revision);
        Assert.Equal("hello", Assert.Single(saved.Messages).Text);
        Assert.Null(await store.SaveAsync("chat-1", 0, saved, CancellationToken.None));

        var cleared = await store.ClearAsync("chat-1", CancellationToken.None);

        Assert.Equal(2, cleared.Revision);
        Assert.Empty(cleared.Messages);
        Assert.Equal(0, cleared.TokenCount);
        Assert.Equal(0m, cleared.CumulatedCost);
    }

    [Fact]
    public async Task StoreCopiesSavedMessageCollections()
    {
        var store = CreateStore();
        var messages = new List<ContextMessage> { new(ChatMessageRole.User, DateTime.UnixEpoch, "first") };

        var saved = await store.SaveAsync("chat-2", 0, new ChatContextSnapshot(0, messages, 0, 0m), CancellationToken.None);
        messages.Add(new ContextMessage(ChatMessageRole.Assistant, DateTime.UnixEpoch, "later"));

        Assert.Single(saved!.Messages);
    }

    [Fact]
    public async Task StoreHonorsCancellation()
    {
        var store = CreateStore();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.LoadAsync("chat", cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync("chat", 0, ChatContextSnapshot.Empty, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.ClearAsync("chat", cancellation.Token));
    }

    [Fact]
    public void OperationRegistryGuardsAndCompletesOperations()
    {
        var registry = new ChatOperationRegistry();
        var first = registry.Start("chat-1");

        Assert.True(registry.IsCurrent(first));
        Assert.Throws<InvalidOperationException>(() => registry.Start("chat-1"));

        registry.Stop("chat-1");

        Assert.True(first.CancellationToken.IsCancellationRequested);
        Assert.False(registry.IsCurrent(first));

        var second = registry.Start("chat-1");
        registry.Complete(second);

        Assert.False(registry.IsCurrent(second));
        Assert.NotEqual(first.Generation, second.Generation);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(1440, true)]
    [InlineData(0, false)]
    [InlineData(1441, false)]
    public void ChatContextConfigurationValidatesExpirationRange(int minutes, bool expectedValid)
    {
        var configuration = new ChatContextConfiguration { IdleExpirationMinutes = minutes };
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(configuration, new ValidationContext(configuration), results, validateAllProperties: true);

        Assert.Equal(expectedValid, isValid);
    }

    private static InMemoryChatContextStore CreateStore() => new(Options.Create(new ChatContextConfiguration()));
}