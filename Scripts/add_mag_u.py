import csv
import math
import sys
import os
import re

def parse_u_file(u_file_path):
    magnitudes = []
    if not os.path.exists(u_file_path):
        return None

    with open(u_file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    # Filter out comment lines, use only the last time step
    data_lines = [l for l in lines if not l.strip().startswith('#') and l.strip()]
    if not data_lines:
        return None
    last_line = data_lines[-1]

    cleaned = last_line.replace('(', ' ').replace(')', ' ')
    tokens = cleaned.split()

    # Probes format: "time (u v w) (u v w) ..." — skip the leading time value
    if tokens:
        tokens = tokens[1:]

    count = len(tokens) // 3
    print(f'  -> Parsed {count} vectors.')

    for i in range(0, count * 3, 3):
        try:
            u, v, w = float(tokens[i]), float(tokens[i+1]), float(tokens[i+2])
            # Check all three components for OpenFOAM's VGREAT sentinel (-1.797e308)
            # and other invalid values
            if any(math.isnan(x) or math.isinf(x) or abs(x) > 1e100 for x in (u, v, w)):
                mag = float('nan')
            else:
                mag = math.sqrt(u*u + v*v + w*w) / 5.0
                if math.isnan(mag) or math.isinf(mag):
                    mag = float('nan')
        except:
            mag = float('nan')
        magnitudes.append(mag)
    return magnitudes

def main():
    if len(sys.argv) < 3:
        print('Usage: python add_mag_u.py <case_name> <case_dir>')
        return

    case_name = sys.argv[1]
    case_dir = sys.argv[2]

    # Discover directions based on subdirectories in case_dir
    possible_dirs = [d for d in os.listdir(case_dir) if os.path.isdir(os.path.join(case_dir, d)) and d.replace('.','',1).isdigit()]
    directions = sorted(possible_dirs, key=float)

    for direction in directions:
        csv_path = os.path.join(case_dir, 'Dataset', f'{case_name}_{direction}.csv')
        u_search_path = os.path.join(case_dir, direction, 'postProcessing', 'ttt')

        if not os.path.exists(csv_path) or not os.path.exists(u_search_path):
            continue

        # Find Latest Time
        subdirs = [d for d in os.listdir(u_search_path) if os.path.isdir(os.path.join(u_search_path, d))]
        numeric_dirs = sorted([d for d in subdirs if d.replace('.','',1).isdigit()], key=float, reverse=True)
        if not numeric_dirs: continue

        u_file_path = os.path.join(u_search_path, numeric_dirs[0], 'U')
        if not os.path.exists(u_file_path): continue

        print(f'\n--- Processing {direction} deg ---')
        magnitudes = parse_u_file(u_file_path)
        if not magnitudes: continue

        # Process CSV
        try:
            with open(csv_path, 'r', newline='', encoding='utf-8') as f:
                reader = csv.DictReader(f)
                rows = list(reader)
                fieldnames = reader.fieldnames

            if 'mag_U' not in fieldnames:
                fieldnames.append('mag_U')

            new_rows = []
            limit = min(len(rows), len(magnitudes))

            for i in range(limit):
                row = rows[i]
                mag = magnitudes[i]
                sdf = float(row.get('SDF', 0))

                if math.isnan(mag):
                    if sdf < 10: row['mag_U'] = 0.0
                    else: row['mag_U'] = 'NaN'
                else:
                    row['mag_U'] = mag
                new_rows.append(row)

            # Write back to the SAME file
            with open(csv_path, 'w', newline='', encoding='utf-8') as f:
                writer = csv.DictWriter(f, fieldnames=fieldnames)
                writer.writeheader()
                writer.writerows(new_rows)

            print(f'  -> Results written to: {os.path.basename(csv_path)}')
        except Exception as e:
            print(f'  Error: {e}')

if __name__ == '__main__':
    main()
