# Storage format

Collections are **plain files in a folder you own**. There is no database and no
proprietary container: a collection is a directory you can put under version control,
zip up, or share as-is. This is the feature that makes requests easy to store and share.

## Collection layout on disk (planned)

```
my-collection/
├── collection.json           # collection manifest (name, description, schema version)
├── environments/
│   ├── local.json            # non-secret environment variables (safe to commit)
│   └── local.secrets.json    # secrets — GIT-IGNORED, never shared
├── Users/                    # a folder = a folder node in the request tree
│   ├── get-user.req.json
│   └── create-user.req.json
└── health.req.json
```

- **Folders map to directories.** The request tree mirrors the filesystem, so moving a
  request is just moving a file.
- **One request per file**, suffixed `.req.json`. The file name derives from the request
  name; the authoritative name is still inside the file.
- **Secrets live apart.** Environment values that are secret go in a `*.secrets.json`
  file that is git-ignored by default, so sharing a collection never leaks tokens.
  Requests reference them via `{{variables}}` (see below) rather than inlining them.

> Status: the request file format below is **implemented**. The collection manifest,
> folder loader, and environment files are **planned** (see [roadmap.md](roadmap.md)).

## Request file format (implemented)

Each request is a JSON document. Conventions:

- **camelCase** property names.
- **Enums as strings** (e.g. `"Raw"`, `"Bearer"`).
- **Indented** output for readable diffs.
- **Null optionals omitted** to keep files clean.
- **Unknown properties are ignored on read** — a file written by a newer app version
  still loads in an older one (fields it doesn't understand are dropped, not fatal).
- A top-level **`version`** integer identifies the schema; it is bumped only on
  breaking changes so old files can be migrated rather than rejected.

### Example

```json
{
  "version": 1,
  "name": "Get user",
  "method": "GET",
  "url": "{{baseUrl}}/users/{{id}}",
  "headers": [
    { "name": "Accept", "value": "application/json" },
    { "name": "X-Trace", "value": "1", "enabled": false }
  ],
  "query": [
    { "name": "verbose", "value": "true" }
  ],
  "body": {
    "type": "Raw",
    "mediaType": "application/json",
    "text": "{\"a\":1}"
  },
  "auth": {
    "type": "Bearer",
    "token": "{{token}}"
  },
  "description": "Fetches a user by id"
}
```

### Field reference

| Field | Type | Notes |
|-------|------|-------|
| `version` | int | Schema version. Currently `1`. |
| `name` | string | **Required.** Display name; basis for the file name. |
| `method` | string | HTTP method. Defaults to `GET`. A string, so custom methods are allowed. |
| `url` | string | **Required.** May contain `{{variables}}`. |
| `headers` | array of [key/value](#keyvalue-entries) | Ordered. Disabled entries are kept but not sent. |
| `query` | array of [key/value](#keyvalue-entries) | Ordered query string parameters. |
| `body` | [body object](#body) | Defaults to `{ "type": "None" }`. |
| `auth` | [auth object](#auth) | Defaults to `{ "type": "None" }`. |
| `description` | string? | Optional; omitted when null. |

#### Key/value entries

Used by `headers`, `query`, and form bodies.

| Field | Type | Notes |
|-------|------|-------|
| `name` | string | The key. May contain `{{variables}}`. |
| `value` | string | The value. May contain `{{variables}}`. |
| `enabled` | bool | Defaults to `true`. When `false`, kept for reference but not sent. |
| `description` | string? | Optional note; never sent over the wire. |

#### Body

| `type` | Active fields |
|--------|---------------|
| `None` | (no body sent) |
| `Raw` | `mediaType` (e.g. `application/json`), `text` |
| `FormUrlEncoded` | `form` (array of key/value entries) |

#### Auth

| `type` | Active fields |
|--------|---------------|
| `None` | — |
| `Bearer` | `token` |
| `Basic` | `username`, `password` |
| `ApiKey` | `apiKeyName`, `apiKeyValue`, `apiKeyLocation` (`Header` or `Query`) |

All auth string fields may contain `{{variables}}`, so secrets are sourced from a
(git-ignored) environment rather than committed inline.

## Variables

Any value may contain `{{name}}` tokens, resolved against the active environment
before a request is sent (`ApiClient.Core.Variables.VariableResolver`):

- Known tokens are substituted; **unknown tokens are left untouched** (`{{missing}}`
  stays literally `{{missing}}`) so missing values are visible rather than silent.
- Whitespace inside the braces is ignored: `{{ token }}` == `{{token}}`.
- The resolver can also report which referenced names were unresolved, for UI hints.

## Versioning policy

- Additive changes (new optional fields) do **not** bump `version` — old apps ignore
  unknown fields, new apps treat missing fields as defaults.
- Breaking changes bump `version`; a migration step upgrades older files on load.
