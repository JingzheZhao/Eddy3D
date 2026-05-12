# MetaBlock

STL mesh combiner — takes a multi-part, non-watertight STL and outputs a single clean solid.

## How it works

1. **Weld** — merges duplicate vertices to stitch open seams
2. **Split** — separates the mesh into individual connected components
3. **Reconstruct** — keeps watertight pieces as-is; replaces open/broken pieces with tight bounding boxes
4. **AABB overlap detection** — checks every pair of bounding boxes; pieces whose boxes don't intersect will never need a boolean union
5. **Union-find grouping** — clusters overlapping pieces into connected groups using union-find; non-overlapping pieces (e.g. most buildings in an urban model) are collected as standalones and skip boolean entirely
6. **Parallel tree union** — each overlap group is resolved with a log₂(N) depth tree reduction; pairs at each level run in parallel across all CPU cores via `ProcessPoolExecutor`
7. **Concatenate** — standalone pieces and union results are concatenated into the final mesh; boolean fallback is used if any union fails or shrinks bounds

### Union pipeline (urban scale)

```
pieces: [A, B, C, D, E, F, ...]
         │
         ▼
 AABB overlap check
         │
         ├── overlapping groups → tree union (parallel)
         │        round 1:  (A∪B)  (C∪D)  (E∪F)
         │        round 2:  (AB∪CD)  (EF∪...)
         │        round 3:  final group solid
         │
         └── standalones (no overlap) → concatenate directly
         │
         ▼
    final combined mesh
```

For a 1000-building urban model where ~90% of buildings don't overlap, this reduces boolean operations from ~1000 sequential calls to ~50 parallel ones.

## Usage

### Standalone

```bash
uv run combine_mesh.py
```

Place your model as `input.stl` in the project root before running.

### API (Grasshopper / Eddy3D)

Start the API server:

```bash
uv run api.py
```

The server listens on `http://localhost:8000` by default.

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check |
| `/combine` | POST | Upload an STL, receive the combined solid STL |

Then in Grasshopper, use the `MetaBlockComponent` (see `grasshopper/MetaBlockComponent.cs`) — wire in your mesh, set the API URL, and toggle **Run** to true.

## Output

| File | Description |
|------|-------------|
| `combined_reconstructed_union.stl` | Final combined solid |
| `reconstructed_report.txt` | Per-piece stats: vertices, faces, watertight status, bounds |

## Configuration

Edit the constants at the top of `combine_mesh.py`:

| Variable | Default | Description |
|----------|---------|-------------|
| `BOOLEAN_ENGINE` | `"manifold"` | Boolean backend (`"manifold"` or `"blender"`) |
| `MERGE_DIGITS` | `1` | Vertex weld precision (decimal places — `1` = 10 cm tolerance for meter-unit CFD models) |
| `MIN_FACES_TO_RECONSTRUCT` | `2` | Minimum faces for a piece to be reconstructed as a box |
| `BOX_EXPAND` | `0.0` | Expand bounding boxes by this amount (use `0.01`–`0.1` if parts touch but don't overlap) |

## Requirements

Dependencies are declared in `pyproject.toml` and resolved automatically by `uv run`:

- [`trimesh`](https://trimesh.org/) — mesh loading, cleaning, splitting, and concatenation
- [`manifold3d`](https://github.com/elalish/manifold) — fast, robust boolean union engine
- [`numpy`](https://numpy.org/) — geometry math
