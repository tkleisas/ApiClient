using System.Collections.Generic;

namespace ApiClient.Core.Model;

/// <summary>The kind of payload carried by a request body.</summary>
public enum BodyType
{
    /// <summary>No request body is sent.</summary>
    None,

    /// <summary>A raw textual payload (JSON, XML, plain text, ...) described by <see cref="RequestBody.MediaType"/>.</summary>
    Raw,

    /// <summary>An <c>application/x-www-form-urlencoded</c> payload built from <see cref="RequestBody.Form"/>.</summary>
    FormUrlEncoded,
}

/// <summary>
/// Describes the payload sent with a request. The active shape is selected by
/// <see cref="Type"/>; unrelated fields are simply ignored (e.g. <see cref="Text"/>
/// is meaningless when <see cref="Type"/> is <see cref="BodyType.FormUrlEncoded"/>).
/// </summary>
public record RequestBody
{
    /// <summary>Which kind of body this is. Defaults to <see cref="BodyType.None"/>.</summary>
    public BodyType Type { get; init; } = BodyType.None;

    /// <summary>
    /// The content type for a <see cref="BodyType.Raw"/> body, e.g. <c>application/json</c>.
    /// Used to set the <c>Content-Type</c> header unless one is set explicitly.
    /// </summary>
    public string? MediaType { get; init; }

    /// <summary>The raw text payload for a <see cref="BodyType.Raw"/> body. May contain <c>{{variables}}</c>.</summary>
    public string? Text { get; init; }

    /// <summary>The fields for a <see cref="BodyType.FormUrlEncoded"/> body.</summary>
    public IReadOnlyList<KeyValueItem> Form { get; init; } = [];
}
