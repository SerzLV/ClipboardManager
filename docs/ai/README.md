# Local AI model release manifest

`model-manifest.json` describes the optional Qwen3 4B model offered by ClipVault Studio. The desktop
application accepts a release only when the manifest has a valid ECDSA P-256 signature, is compatible
with the running application version, uses HTTPS, and its downloaded GGUF file passes the declared
SHA-256 verification.

## Publishing a model update

1. Keep the signing key outside the repository.
2. Give each release a new semantic `version` and a new `.gguf` `fileName`. ClipVault keeps the
   existing verified model until the replacement has downloaded and passed verification.
3. Update the HTTPS download URL, size, SHA-256, minimum app version, and short release notes.
4. Sign the manifest with the private ECDSA P-256 key, validate it locally, and publish only the signed
   JSON file. Never publish the private key.

ClipVault downloads an available model update only after the user reviews and confirms it in the app.
