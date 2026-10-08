# Vendor and container formats

DEVI Validate reads expected hashes from text it can parse without guessing. A format is included only when the layout is public and a wrong parse would be obvious in tests.

## Included

- GNU `md5sum`, `sha1sum`, and `sha256sum` output, including the binary-mode `*` marker
- BSD `ALGO (name) = hash` lines
- A file that contains only one hash, or one `ALGO: hash` line, compared with a single evidence file
- CSV and TSV with a path column and a hash column
- A DEVI Validate hash manifest or verification record, accepted only when its record hash matches
- FTK Imager text logs that contain `MD5 checksum:`, `SHA1 checksum:`, or `SHA256 checksum:` lines

The FTK Imager checksum is the hash of the acquired data. Compare it with a raw image of that data. It is not the hash of an E01 or L01 container. This version refuses `.E01`, `.L01`, and later segments of those containers, and it refuses a log that lists more than one image segment.

## Not included

These stay behind `IVendorReportParser`. The application does not load parser assemblies from disk.

| Format | Why it is not parsed yet |
| --- | --- |
| Cellebrite UFDR, XML, and PDF reports | Proprietary package. There is no stable public field guide to implement without guessing. |
| Magnet AXIOM case and portable-case reports | Proprietary. |
| E01 / EWF embedded hashes | The Expert Witness Compression Format is documented by the libewf project, but a partial reader can report the wrong value. A reader belongs here only after it is checked against known images. |
| EnCase Ex01 / L01 containers, X-Ways containers, and other vendor packages | Same constraint: no reliable public parse in this version. |

Per-file hash lists exported as CSV or as `sha256sum` text already work through the generic parsers. Point the tool at that export.

## Adding a parser

1. Implement `DeviValidate.Core.Expected.Vendor.IVendorReportParser`.
2. Register the instance from `VendorParserRegistry.RegisterBuiltIns`.
3. Add tests that use synthetic text generated in the test project.
4. Document the format here, including what the hash covers.

`CanParse` must be specific. Built-in detectors run first, then registered parsers, then the sum-file parser.
