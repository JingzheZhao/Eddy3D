import io
import traceback

import numpy as np
import trimesh

try:
    import pymeshfix
    HAS_PYMESHFIX = True
except ImportError:
    HAS_PYMESHFIX = False


def pymeshfix_arrays(fix):
    """Extract (vertices, faces) from a pymeshfix.MeshFix instance across API versions."""
    if hasattr(fix, "return_arrays"):
        return fix.return_arrays()
    if hasattr(fix, "v") and hasattr(fix, "f"):
        return fix.v, fix.f
    mesh = getattr(fix, "mesh", None)
    if mesh is not None:
        verts = np.asarray(mesh.points)
        faces_raw = np.asarray(mesh.faces).reshape(-1, 4)[:, 1:]
        return verts, faces_raw
    raise AttributeError("Cannot extract repaired mesh from pymeshfix.MeshFix")
from fastapi import FastAPI, File, HTTPException, UploadFile, Request
from fastapi.responses import Response, JSONResponse

from combine_mesh import clean, pairwise_union, MERGE_DIGITS, MIN_FACES_TO_RECONSTRUCT, BOX_EXPAND, box_from_bounds

app = FastAPI(title="MetaBlock", description="STL mesh combiner API for CFD preprocessing")


@app.exception_handler(Exception)
async def unhandled_exception_handler(request: Request, exc: Exception):
    tb = traceback.format_exc()
    print(tb, flush=True)
    return JSONResponse(status_code=500, content={"detail": f"{type(exc).__name__}: {exc}", "traceback": tb})


@app.get("/health")
def health():
    return {"status": "ok"}


@app.post("/combine", response_class=Response)
async def combine(file: UploadFile = File(...), mode: str = "auto"):
    """
    mode:
      - "auto" (default): try whole-mesh pymeshfix first, fall back to per-piece reconstruct
      - "complex": whole-mesh pymeshfix only (best for CAD parts)
      - "urban": per-piece reconstruct only (best for many independent buildings)
    """
    if not file.filename.endswith(".stl"):
        raise HTTPException(status_code=400, detail="Only STL files are supported")

    data = await file.read()

    try:
        raw = trimesh.load(io.BytesIO(data), file_type="stl", force="mesh", process=False)
    except Exception as e:
        raise HTTPException(status_code=422, detail=f"Failed to parse STL: {e}")

    welded = raw.copy()
    welded.merge_vertices(digits_vertex=MERGE_DIGITS)
    welded = clean(welded)

    def _stl_response(mesh_obj, stats_dict):
        import json as _json
        buf = io.BytesIO()
        mesh_obj.export(buf, file_type="stl")
        buf.seek(0)
        return Response(
            content=buf.read(),
            media_type="application/octet-stream",
            headers={
                "Content-Disposition": "attachment; filename=combined.stl",
                "X-MetaBlock-Stats": _json.dumps(stats_dict),
            },
        )

    def run_urban(welded_mesh):
        pieces = list(welded_mesh.split(only_watertight=False))
        s = {
            "input_faces": len(welded_mesh.faces),
            "input_vertices": len(welded_mesh.vertices),
            "pieces_total": len(pieces),
            "watertight_original": 0,
            "repaired_trimesh": 0,
            "repaired_pymeshfix": 0,
            "preserved_nonwatertight": 0,
            "skipped_tiny": 0,
            "pymeshfix_available": HAS_PYMESHFIX,
        }
        parts = []
        for piece in pieces:
            piece = clean(piece)
            original_watertight = piece.is_watertight
            if original_watertight:
                s["watertight_original"] += 1
            if not piece.is_watertight:
                try:
                    piece.fill_holes()
                    piece = clean(piece)
                    if piece.is_watertight and not original_watertight:
                        s["repaired_trimesh"] += 1
                except Exception:
                    pass
            if not piece.is_watertight and HAS_PYMESHFIX and len(piece.faces) >= MIN_FACES_TO_RECONSTRUCT:
                try:
                    fix = pymeshfix.MeshFix(np.asarray(piece.vertices), np.asarray(piece.faces))
                    fix.repair(joincomp=True, remove_smallest_components=False)
                    v_out, f_out = pymeshfix_arrays(fix)
                    if len(f_out) > 0:
                        piece = clean(trimesh.Trimesh(vertices=v_out, faces=f_out, process=False))
                        if piece.is_watertight:
                            s["repaired_pymeshfix"] += 1
                except Exception:
                    pass
            if piece.is_watertight and abs(piece.volume) > 1e-9:
                parts.append(piece)
            elif len(piece.faces) >= MIN_FACES_TO_RECONSTRUCT:
                s["preserved_nonwatertight"] += 1
                parts.append(piece)
            else:
                s["skipped_tiny"] += 1
        if not parts:
            return None, s
        combined = pairwise_union(parts)
        s["output_faces"] = len(combined.faces)
        s["output_vertices"] = len(combined.vertices)
        s["output_watertight"] = bool(combined.is_watertight)
        s["strategy"] = "urban (per-piece + union)"
        return combined, s

    def run_complex(welded_mesh):
        if not HAS_PYMESHFIX:
            return None, {"strategy": "complex", "error": "pymeshfix not available"}
        print(f"[complex] input: {len(welded_mesh.vertices)} verts, {len(welded_mesh.faces)} faces, watertight={welded_mesh.is_watertight}", flush=True)
        whole = None
        for jc in (False, True):
            try:
                fix = pymeshfix.MeshFix(np.asarray(welded_mesh.vertices), np.asarray(welded_mesh.faces))
                fix.repair(joincomp=jc, remove_smallest_components=False)
                v_out, f_out = pymeshfix_arrays(fix)
                print(f"[complex] joincomp={jc}: {len(v_out)} verts, {len(f_out)} faces", flush=True)
                if len(f_out) == 0:
                    continue
                cand = clean(trimesh.Trimesh(vertices=v_out, faces=f_out, process=False))
                if cand.is_watertight and abs(cand.volume) > 1e-9:
                    whole = cand
                    break
            except Exception as e:
                print(f"[complex] failed at joincomp={jc}: {e}", flush=True)
        if whole is None:
            return None, {"strategy": "complex", "error": "no watertight repair"}
        s = {
            "strategy": "whole-mesh pymeshfix",
            "input_faces": len(welded_mesh.faces),
            "output_faces": len(whole.faces),
            "output_vertices": len(whole.vertices),
            "output_watertight": True,
            "detail_ratio": round(len(whole.faces) / max(1, len(welded_mesh.faces)), 3),
        }
        return whole, s

    if mode == "urban":
        result, stats = run_urban(welded)
        if result is None:
            raise HTTPException(status_code=422, detail="Urban mode found no valid geometry.")
        stats["mode"] = "urban"
        return _stl_response(result, stats)

    if mode == "complex":
        result, stats = run_complex(welded)
        if result is None:
            raise HTTPException(status_code=422, detail="Complex mode could not produce a watertight result.")
        stats["mode"] = "complex"
        return _stl_response(result, stats)

    # auto: try urban first; accept if output is watertight, otherwise fall back to complex
    urban_result, urban_stats = run_urban(welded)
    if urban_result is not None and urban_stats.get("output_watertight", False):
        urban_stats["mode"] = "auto -> urban"
        return _stl_response(urban_result, urban_stats)

    print(f"[auto] urban output not watertight, trying complex...", flush=True)
    complex_result, complex_stats = run_complex(welded)
    if complex_result is not None:
        complex_stats["mode"] = "auto -> complex"
        complex_stats["urban_attempt"] = urban_stats
        return _stl_response(complex_result, complex_stats)

    if urban_result is not None:
        urban_stats["mode"] = "auto -> urban (complex also failed)"
        return _stl_response(urban_result, urban_stats)

    raise HTTPException(status_code=422, detail="Auto mode could not produce a result with either strategy.")


if __name__ == "__main__":
    import uvicorn
    uvicorn.run("api:app", host="0.0.0.0", port=8000, reload=False)
