# Devi.Updates

Signed update-feed client shared by DEVI Validate, DEVI Decrypt, and DEVI Registry.

- One ECDSA P-256 public key (`UpdateTrust.PublicKeySpkiBase64`). The matching private key is offline and is not in this repository.
- Per-product feeds at `https://downloads.deviops.app/downloads/<product-id>/updates.json`.
- The HTTP user agent is `DEVI-<Product>/<version>` so the download host can recognize the app when it fetches its own installer and zip.
