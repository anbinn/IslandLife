# Sprout Lands Source Registry

Version: T0.1
Owner: PM
Document type: Asset / source record (not a PM governance document)

This document records the provenance and license facts of third-party source
packages imported into the IslandLife Unity project. It records what a package
**is**. It does not decide what any asset **means** or whether it may be used
in production.

---

## 1. PACKAGE

| Field | Value |
|---|---|
| Project Source ID | `SPROUT_LANDS_SORRY_PACK` |
| Author | Cup Nooble |
| Original package | `Sprout Sorry pack.zip` |
| Original local acquisition/archive path | `F:\IslandLife\Projects\Sprout Sorry pack.zip` |
| SHA-256 | `A1BF8BAE5528B27E32EC5E0D1C18E898B3FC2B6D20F89F70E393FAC2757EBE0C` |
| Audit | `IL-ASSET-004A` |
| Package status | `AUTHOR_SOURCE_IMPORTED` |
| Production status | `NOT_PRODUCTION_APPROVED` |
| Commercial release status | `UNRESOLVED / NON-COMMERCIAL TERMS IN INCLUDED README` |
| Author source root | `Assets/Art/Environment/SproutLands/Source/SorryPack/` |

### File totals

| Type | Count |
|---|---|
| Total files | 130 |
| PNG | 100 |
| WAV | 22 |
| GIF | 4 |
| ASEPRITE | 3 |
| TXT | 1 |

Directory count: 20 folders below the package root.
Total uncompressed source size: 19,135,120 bytes.

---

## 2. LICENSE FACTS FROM INCLUDED README

Source of these statements: `Source/SorryPack/read_me.txt` (author-supplied,
2,643 bytes), reproduced verbatim in the source tree.

- Modification of the assets is permitted.
- Use in any kind of non-commercial project is permitted.
- Anything to do with NFTs is not allowed.
- AI training is not allowed.
- Redistribution of the asset pack itself, or resale on other platforms, is not
  allowed, even if slightly modified.
- Redistribution of projects made with these assets is allowed.
- Open-source projects are allowed, with a required note that some or all assets
  are made by Cup Nooble, together with the licensing terms.
- Credit is required: Cup Nooble.
- The author invites contact to arrange different licensing terms
  (Discord: `cup_nooble`).

No commercial license is asserted by this document.
Commercial release is **not** cleared.

---

## 3. SOURCE PRESERVATION RULE

- `SorryPack` is **AUTHOR SOURCE** provenance.
- Author folder names and file names are preserved exactly, including casing,
  spacing, spelling and typos.
- Source presence does **not** imply production approval.
- Production assets must be separately reviewed for semantics, slicing, import
  settings, object completeness, animation grouping, season/biome role, and
  Unity validation before use.
- 16x16 is **not** a blanket object-slicing rule.
- Existing Basic production assets are frozen in their current physical paths
  until a separately approved migration task exists.

### Author hierarchy as imported

```
Assets/Art/Environment/SproutLands/Source/SorryPack/
├── Audio/
├── Early Access/
│   ├── Dungeon Pack/
│   │   ├── enemies/
│   │   └── tiles/
│   ├── Ocean Pack/
│   ├── Plant update 2/
│   │   ├── Bee/
│   │   ├── frog/
│   │   ├── Furniture/
│   │   ├── Ground tilesets/
│   │   └── piknik/
│   ├── Sprout winter/
│   └── Village pack/
│       └── houses/
│           ├── Grey brick house/
│           ├── small house/
│           └── small hut/
│               └── light/
├── Tests/
└── read_me.txt
```

`Early Access`, `Dungeon Pack`, `Ocean Pack`, `Sprout winter`, `Village pack`,
`Plant update 2`, `Ground tilesets` and `piknik` are **author** folder names.
They are source provenance only. They are not IslandLife production
classifications, and they are not season or biome categories.

### Duplicate content retained

`present animation aseprite file.png` and `present red.png` are byte-identical
inside the author package. **Both are kept.** The source layer preserves author
package identity and is not deduplicated.

The Sorry Pack `read_me.txt` and the Basic pack `read_me.txt` differ. They are
kept separate and are not merged.

### Audio and Aseprite source handling

- The 22 WAV files are preserved as source package contents only. They are not
  normalized, converted, trimmed, loop-edited, classified for gameplay use, or
  assigned to scenes. No AudioMixer asset exists for this package.
- The 3 `.aseprite` files are preserved exactly as author source files. They are
  not edited, exported, decoded into production frames, or used to replace their
  PNG counterparts. Their internal frame and layer structure remains unresolved
  until a future dedicated card.

---

## 4. EARLY ACCESS / PACKAGE STABILITY

- The package contains an author folder named `Early Access`.
- Source provenance must therefore remain traceable to this exact ZIP and
  SHA-256 hash.
- Future package updates must be treated as a **new source-version intake**,
  compared against this hash, and must not silently overwrite this imported
  snapshot.

No speculation is recorded here about future author changes.

---

## 5. INTAKE RECORD

| Field | Value |
|---|---|
| Intake task | `IL-ASSET-004B` |
| Imported branch | `IL-WORLD-003A` |
| Imported at commit | `4f91045fabd3495853e102f49149db9b67d17977` (task starting HEAD) |
| Author files verified byte-identical to archive | 130 / 130 |
| Redundant outer wrapper directory | not created |
| Existing Basic assets modified | none |
| Sprite slicing performed | none |
| Production assets created | none |

Note: Git cannot represent empty directories. The author package contains one
empty folder, `Early Access/Village pack/houses/small hut/light/`, which contains
zero files. It exists in the imported working tree but is not represented in the
commit. No placeholder file was invented for it.