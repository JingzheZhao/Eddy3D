import trimesh
from pathlib import Path
import numpy as np
from concurrent.futures import ProcessPoolExecutor, as_completed
import multiprocessing

INPUT = "input.stl"
COMBINED_OUTPUT = "combined_reconstructed_union.stl"
REPORT_OUTPUT = "reconstructed_report.txt"

BOOLEAN_ENGINE = "manifold"
MERGE_DIGITS = 1

# Ignore only truly tiny junk, not open box fragments
MIN_FACES_TO_RECONSTRUCT = 2

# If blocks touch but do not overlap, use small value like 0.01 or 0.1 in model units
BOX_EXPAND = 0.0


def clean(mesh):
    mesh = mesh.copy()

    try:
        mask = mesh.nondegenerate_faces()
        mesh.update_faces(mask)
    except Exception:
        pass

    try:
        mask = mesh.unique_faces()
        mesh.update_faces(mask)
    except Exception:
        pass

    try:
        mesh.remove_unreferenced_vertices()
    except Exception:
        pass

    try:
        mesh.fix_normals()
    except Exception:
        pass

    return mesh


def box_from_bounds(bounds, expand=0.0):
    bmin = bounds[0].astype(float).copy()
    bmax = bounds[1].astype(float).copy()

    bmin -= expand
    bmax += expand

    extents = bmax - bmin

    # Avoid zero-thickness boxes from sheet-like pieces
    # If one dimension is zero, give it a tiny thickness
    min_thickness = 0.001
    extents = np.maximum(extents, min_thickness)

    center = (bmin + bmax) / 2.0

    transform = np.eye(4)
    transform[:3, 3] = center

    box = trimesh.creation.box(extents=extents, transform=transform)
    box.fix_normals()
    return clean(box)


def info(name, mesh):
    try:
        comps_all = len(mesh.split(only_watertight=False))
    except Exception as e:
        comps_all = f"error: {e}"

    try:
        comps_wt = len(mesh.split(only_watertight=True))
    except Exception as e:
        comps_wt = f"error: {e}"

    return (
        f"\n{name}\n"
        f"{'-' * len(name)}\n"
        f"Vertices: {len(mesh.vertices)}\n"
        f"Faces: {len(mesh.faces)}\n"
        f"Watertight: {mesh.is_watertight}\n"
        f"Winding consistent: {mesh.is_winding_consistent}\n"
        f"Components all: {comps_all}\n"
        f"Components watertight only: {comps_wt}\n"
        f"Bounds:\n{mesh.bounds}\n"
    )


def boxes_overlap(bounds_a, bounds_b):
    return (
        (bounds_a[0] <= bounds_b[1]).all()
        and (bounds_b[0] <= bounds_a[1]).all()
    )


def try_union(a, b):
    try:
        candidate = trimesh.boolean.union([a, b], engine=BOOLEAN_ENGINE)
        if candidate is None:
            raise RuntimeError("Boolean returned None")
        candidate = clean(candidate)
        expected = trimesh.util.concatenate([a, b])
        if (
            (candidate.bounds[0] > expected.bounds[0] + 1e-6).any()
            or (candidate.bounds[1] < expected.bounds[1] - 1e-6).any()
        ):
            return clean(trimesh.util.concatenate([a, b]))
        return candidate
    except Exception:
        return clean(trimesh.util.concatenate([a, b]))


def tree_union(parts):
    """Log2(N) depth tree reduction with parallel pair unions."""
    cpu_count = multiprocessing.cpu_count()
    level = parts

    while len(level) > 1:
        pairs = [(level[i], level[i + 1]) for i in range(0, len(level) - 1, 2)]
        remainder = [level[-1]] if len(level) % 2 == 1 else []

        results = []
        with ProcessPoolExecutor(max_workers=cpu_count) as executor:
            futures = {executor.submit(try_union, a, b): i for i, (a, b) in enumerate(pairs)}
            ordered = [None] * len(pairs)
            for future in as_completed(futures):
                ordered[futures[future]] = future.result()

        level = ordered + remainder
        print(f"  Tree level done: {len(level)} parts remaining")

    return clean(level[0])


def pairwise_union(parts):
    """Separate non-overlapping parts (concatenate) from overlapping ones (union)."""
    if len(parts) == 1:
        return parts[0]

    # Sort by centroid X then Y for spatial locality
    parts = sorted(parts, key=lambda m: (m.centroid[0], m.centroid[1]))

    bounds = [m.bounds for m in parts]

    # Build overlap groups via union-find
    parent = list(range(len(parts)))

    def find(x):
        while parent[x] != x:
            parent[x] = parent[parent[x]]
            x = parent[x]
        return x

    def union_find(x, y):
        parent[find(x)] = find(y)

    for i in range(len(parts)):
        for j in range(i + 1, len(parts)):
            if boxes_overlap(bounds[i], bounds[j]):
                union_find(i, j)

    groups: dict[int, list] = {}
    for i, part in enumerate(parts):
        root = find(i)
        groups.setdefault(root, []).append(part)

    overlapping = [g for g in groups.values() if len(g) > 1]
    standalone = [g[0] for g in groups.values() if len(g) == 1]

    print(f"Overlap groups: {len(overlapping)}, standalone (concat only): {len(standalone)}")

    merged = []
    for i, group in enumerate(overlapping):
        print(f"Unioning overlap group {i + 1}/{len(overlapping)} ({len(group)} parts)...")
        merged.append(tree_union(group))

    all_parts = merged + standalone
    if len(all_parts) == 1:
        return all_parts[0]

    return clean(trimesh.util.concatenate(all_parts))


def main():
    if not Path(INPUT).exists():
        raise FileNotFoundError(f"Cannot find {INPUT}")

    report = []

    raw = trimesh.load(INPUT, force="mesh", process=False)

    welded = raw.copy()
    welded.merge_vertices(digits_vertex=MERGE_DIGITS)
    welded = clean(welded)

    print(info("Welded input", welded))
    report.append(info("Welded input", welded))

    pieces = list(welded.split(only_watertight=False))
    print(f"Pieces found: {len(pieces)}")
    report.append(f"\nPieces found: {len(pieces)}\n")

    reconstructed_parts = []

    for i, piece in enumerate(pieces, start=1):
        piece = clean(piece)
        report.append(info(f"Original piece {i}", piece))

        if piece.is_watertight and abs(piece.volume) > 1e-9:
            print(f"Piece {i}: kept watertight volume")
            reconstructed_parts.append(piece)
        elif len(piece.faces) >= MIN_FACES_TO_RECONSTRUCT:
            print(f"Piece {i}: reconstructed as box from bounds")
            box = box_from_bounds(piece.bounds, expand=BOX_EXPAND)
            reconstructed_parts.append(box)
            report.append(info(f"Reconstructed box {i}", box))
        else:
            print(f"Piece {i}: skipped tiny fragment")

    combined = pairwise_union(reconstructed_parts)
    combined.export(COMBINED_OUTPUT)

    print(f"\nSaved combined: {COMBINED_OUTPUT}")
    print(info("Combined reconstructed union", combined))
    report.append(info("Combined reconstructed union", combined))

    with open(REPORT_OUTPUT, "w", encoding="utf-8") as f:
        f.write("\n".join(report))

    print(f"Saved report: {REPORT_OUTPUT}")


if __name__ == "__main__":
    multiprocessing.freeze_support()
    main()
