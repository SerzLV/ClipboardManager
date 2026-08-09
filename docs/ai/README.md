# Local AI model release manifest

`model-manifest.json` describes the optional Qwen3 4B model offered by ClipVault Studio. The desktop
application accepts a release only when the manifest has a valid ECDSA P-256 signature, is compatible
with the running application version, uses HTTPS, and its downloaded GGUF file passes the declared
SHA-256 verification.

## Publishing a model update

1. Keep the signing key outside this repository.
2. Copy `model-manifest.json` and give the next release a new semantic `version` **and a new
   `fileName`**. The new filename lets ClipVault retain the existing working model until the download
   and verification of the replacement are complete.
3. Update the HTTPS download URL, size, SHA-256, minimum app version, and short release notes.
   Use the exact byte length reported by the release host; the application treats this signed value as
   a hard download limit. Verify both the size and SHA-256 against the official model release before signing.
4. Sign and validate the manifest with the private ECDSA P-256 key, then publish only the signed JSON
   file. Never publish or commit the private key.

The current public manifest URL is:

`https://raw.githubusercontent.com/SerzLV/ClipboardManager/master/docs/ai/model-manifest.json`

ClipVault downloads an available model update only after the user reviews and confirms it in the app.
