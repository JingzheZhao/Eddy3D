import csv
import math
import sys
import os
import re

# Reference velocity used for normalization.
# mag_U  is divided by UREF          (units: m/s  -> dimensionless U/Uref)
# k      is divided by UREF**2       (units: m^2/s^2 -> dimensionless k/Uref^2)
UREF = 5.0

SENTINEL_THRESHOLD = 1e100  # OpenFOAM -DBL_MAX ~ -1.7976931e+307


def _strip_comments(text):
    return re.sub(r'#.*', '', text)


def parse_u_file(u_file_path):
    """Parse an OpenFOAM vector probe file ('U').

    Format (single timestep):
        # header lines starting with '#'
        600 (Ux0 Uy0 Uz0) (Ux1 Uy1 Uz1) ...

    Using regex on parenthesised triplets inherently skips the leading
    timestamp token, avoiding the off-by-one bug of add_mag_u.py.

    Returns a list of normalized magnitudes (|U| / UREF) or NaN for
    sentinel / malformed entries.
    """
    if not os.path.exists(u_file_path):
        return None

    with open(u_file_path, 'r', encoding='utf-8') as f:
        full_text = f.read()

    cleaned = _strip_comments(full_text)
    triplets = re.findall(r'\(([^)]+)\)', cleaned)

    magnitudes = []
    for trip in triplets:
        parts = trip.split()
        if len(parts) != 3:
            magnitudes.append(float('nan'))
            continue
        try:
            u, v, w = float(parts[0]), float(parts[1]), float(parts[2])
            if (math.isnan(u) or math.isinf(u) or abs(u) > SENTINEL_THRESHOLD
                    or math.isnan(v) or math.isinf(v) or abs(v) > SENTINEL_THRESHOLD
                    or math.isnan(w) or math.isinf(w) or abs(w) > SENTINEL_THRESHOLD):
                magnitudes.append(float('nan'))
            else:
                magnitudes.append(math.sqrt(u * u + v * v + w * w) / UREF)
        except Exception:
            magnitudes.append(float('nan'))

    print(f'  -> Parsed {len(magnitudes)} U vectors.')
    return magnitudes


def parse_k_file(k_file_path):
    """Parse an OpenFOAM scalar probe file ('k').

    Format (single timestep):
        # header lines starting with '#'
        600 k0 k1 k2 ...

    The first whitespace-separated token is the timestamp; we drop it.
    k is normalized by UREF**2 so its magnitude range is compatible with
    the mag_U channel (U/Uref) in the training targets.

    Returns a list of normalized k values (k / UREF^2) or NaN for
    sentinel / malformed entries.
    """
    if not os.path.exists(k_file_path):
        return None

    with open(k_file_path, 'r', encoding='utf-8') as f:
        full_text = f.read()

    cleaned = _strip_comments(full_text)
    # Defensive: strip any accidental parens
    cleaned = cleaned.replace('(', ' ').replace(')', ' ')
    tokens = cleaned.split()

    if not tokens:
        return []

    # Drop timestamp (first token).
    tokens = tokens[1:]

    k_norm = UREF * UREF
    ks = []
    for t in tokens:
        try:
            val = float(t)
            if math.isnan(val) or math.isinf(val) or abs(val) > SENTINEL_THRESHOLD:
                ks.append(float('nan'))
            else:
                ks.append(val / k_norm)
        except Exception:
            ks.append(float('nan'))

    print(f'  -> Parsed {len(ks)} k scalars.')
    return ks


def main():
    if len(sys.argv) < 3:
        print('Usage: python add_uk.py <case_name> <case_dir>')
        return

    case_name = sys.argv[1]
    case_dir = sys.argv[2]

    # Discover direction subdirectories (names that look like floats).
    possible_dirs = [
        d for d in os.listdir(case_dir)
        if os.path.isdir(os.path.join(case_dir, d)) and d.replace('.', '', 1).isdigit()
    ]
    directions = sorted(possible_dirs, key=float)

    for direction in directions:
        csv_path = os.path.join(case_dir, 'Dataset', f'{case_name}_{direction}.csv')
        pp_path = os.path.join(case_dir, direction, 'postProcessing', 'ttt')

        if not os.path.exists(csv_path) or not os.path.exists(pp_path):
            continue

        # Latest time directory.
        subdirs = [d for d in os.listdir(pp_path) if os.path.isdir(os.path.join(pp_path, d))]
        numeric_dirs = sorted(
            [d for d in subdirs if d.replace('.', '', 1).isdigit()],
            key=float, reverse=True,
        )
        if not numeric_dirs:
            continue

        time_dir = numeric_dirs[0]
        u_file_path = os.path.join(pp_path, time_dir, 'U')
        k_file_path = os.path.join(pp_path, time_dir, 'k')

        if not os.path.exists(u_file_path) or not os.path.exists(k_file_path):
            print(f'\n--- Skipping {direction} deg: missing U or k file ---')
            continue

        print(f'\n--- Processing {direction} deg ---')
        magnitudes = parse_u_file(u_file_path)
        ks = parse_k_file(k_file_path)
        if not magnitudes or ks is None:
            continue

        if abs(len(magnitudes) - len(ks)) > 0:
            print(f'  WARNING: U/k length mismatch '
                  f'(U={len(magnitudes)}, k={len(ks)}). Using the shorter.')

        # Read CSV.
        try:
            with open(csv_path, 'r', newline='', encoding='utf-8') as f:
                reader = csv.DictReader(f)
                rows = list(reader)
                fieldnames = list(reader.fieldnames)
        except Exception as e:
            print(f'  Error reading CSV: {e}')
            continue

        if 'mag_U' not in fieldnames:
            fieldnames.append('mag_U')
        if 'k' not in fieldnames:
            fieldnames.append('k')

        limit = min(len(rows), len(magnitudes), len(ks))
        new_rows = []
        dropped = 0
        for i in range(limit):
            row = rows[i]
            mag = magnitudes[i]
            k_val = ks[i]
            try:
                sdf = float(row.get('SDF', 0))
            except Exception:
                sdf = 0.0

            mag_nan = math.isnan(mag)
            k_nan = math.isnan(k_val)

            if mag_nan or k_nan:
                # Mirror legacy behavior: inside/near buildings (SDF<10) -> zero;
                # otherwise drop the point.
                if sdf < 10:
                    row['mag_U'] = 0.0 if mag_nan else mag
                    row['k'] = 0.0 if k_nan else k_val
                else:
                    dropped += 1
                    continue
            else:
                row['mag_U'] = mag
                row['k'] = k_val

            new_rows.append(row)

        # Write back.
        try:
            with open(csv_path, 'w', newline='', encoding='utf-8') as f:
                writer = csv.DictWriter(f, fieldnames=fieldnames)
                writer.writeheader()
                writer.writerows(new_rows)
        except Exception as e:
            print(f'  Error writing CSV: {e}')
            continue

        print(f'  -> Wrote {len(new_rows)} rows (dropped {dropped}) to '
              f'{os.path.basename(csv_path)}')


if __name__ == '__main__':
    main()
