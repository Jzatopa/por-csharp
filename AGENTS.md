# Source port working rules

Preserve source semantics and explicit signed/unsigned widths, array windows, record-value copies and serialized layouts. Pin upstream comparisons to revision `7616920a9178fa32efd45939b9360e95933bbf49`.

Keep original game media read-only, use disposable state for native tests, preserve existing work and do not publish original archives or personal saves. Do not replace unsupported platform services with success-returning stubs. Run `bash scripts/test-portable.sh` for source changes and appropriate native replays for host changes. Document the distinction between source disposition, behavioral tests and live/full-game validation.

Review UPSTREAM.md before publication; do not invent a license grant.
