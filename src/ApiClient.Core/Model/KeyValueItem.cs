namespace ApiClient.Core.Model;

/// <summary>
/// A single, orderable, toggleable key/value entry. Used for HTTP headers, query
/// string parameters, and form fields. The list order is significant and preserved
/// on disk, and <see cref="Enabled"/> allows an entry to be kept for reference
/// without being sent.
/// </summary>
/// <param name="Name">The key (e.g. header or parameter name). May contain <c>{{variables}}</c>.</param>
/// <param name="Value">The value. May contain <c>{{variables}}</c>.</param>
/// <param name="Enabled">Whether the entry is included when the request is sent. Defaults to <c>true</c>.</param>
/// <param name="Description">Optional human-readable note; never sent over the wire.</param>
public record KeyValueItem(string Name, string Value, bool Enabled = true, string? Description = null);
