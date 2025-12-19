import openpyxl
import math
import sys
import os
import re

def parse_u_file(u_file_path):
    '''
    Parses an OpenFOAM U file and returns a list of vector magnitudes.
    '''
    magnitudes = []
    # Probe coords in header: # Probe 0 (1.5 2.5 10)
    probe_coords = []
    probe_header_pattern = re.compile(r'#\s*Probe\s+\d+\s*\(\s*([-\d\.eE]+)\s+([-\d\.eE]+)\s+([-\d\.eE]+)\s*\)')
    
    if not os.path.exists(u_file_path):
        return None, None

    with open(u_file_path, 'r') as f:
        lines = f.readlines()
        
    data_lines = []
    
    for line in lines:
        line = line.strip()
        if not line: continue
        
        # Parse Probe Headers
        if line.startswith('#'):
            hmatch = probe_header_pattern.search(line)
            if hmatch:
                px = float(hmatch.group(1))
                py = float(hmatch.group(2))
                probe_coords.append((px, py))
            continue
        
        data_lines.append(line)

    full_text = ' '.join(data_lines)
    cleaned_text = full_text.replace('(', ' ').replace(')', ' ')
    tokens = cleaned_text.split()
    
    count = len(tokens) // 3
    print(f'  -> Parsed {count} vectors from U file.')
    
    for i in range(0, len(tokens), 3):
        if i + 2 >= len(tokens): break
        u = float(tokens[i])
        v = float(tokens[i+1])
        w = float(tokens[i+2])
        try:
            if math.isnan(u) or math.isnan(v) or math.isnan(w) or \
               math.isinf(u) or math.isinf(v) or math.isinf(w) or \
               abs(u) > 1e100:
                mag = float('nan')
            else:
                mag = math.sqrt(u*u + v*v + w*w) / 5.0
        except:
            mag = float('nan')
        magnitudes.append(mag)

    return magnitudes, probe_coords

def main():
    if len(sys.argv) < 3:
        print('Usage: python add_mag_u.py <path_to_dataset> <case_dir>')
        return

    dataset_path = sys.argv[1]
    case_dir = sys.argv[2]
    
    # ---------------------------------------------------------
    # Setup Result Path
    # ---------------------------------------------------------
    dirname, filename = os.path.split(dataset_path)
    base, ext = os.path.splitext(filename)
    result_filename = f'{base}_result{ext}'
    result_path = os.path.join(dirname, result_filename)
    
    target_path = dataset_path
    if os.path.exists(result_path):
        print(f'Found existing result file. Editing: {result_filename}')
        target_path = result_path
    
    # ---------------------------------------------------------
    # 1. LOAD WORKBOOK (ONCE)
    # ---------------------------------------------------------
    print(f'Loading Workbook: {target_path} ...')
    try:
        wb = openpyxl.load_workbook(target_path)
    except Exception as e:
        print(f'Error loading workbook: {e}')
        return

    directions = ['0', '45', '90', '135', '180', '225', '270', '315']
    changes_made = False

    # ---------------------------------------------------------
    # 2. ITERATE DIRECTIONS
    # ---------------------------------------------------------
    for direction in directions:
        search_path = os.path.join(case_dir, direction, 'postProcessing', 'ttt')
        
        # Check if Direction Logic Exists
        if not os.path.exists(search_path):
             # Silent skip or verbose? Silent is better for batch log clutter, 
             # but we'll print if found.
             continue
             
        # Find Latest Time
        try:
            subdirs = [d for d in os.listdir(search_path) if os.path.isdir(os.path.join(search_path, d))]
            numeric_dirs = []
            for d in subdirs:
                try: numeric_dirs.append((float(d), d))
                except: pass
            
            if not numeric_dirs: continue
            
            numeric_dirs.sort(key=lambda x: x[0], reverse=True)
            latest_time = numeric_dirs[0][1]
            u_file_path = os.path.join(search_path, latest_time, 'U')
            
            if not os.path.exists(u_file_path): continue
            
            print(f'\n--- Processing {direction} deg (Time: {latest_time}) ---')
            
            # PARSE U FILE
            magnitudes, probe_coords = parse_u_file(u_file_path)
            if magnitudes is None or len(magnitudes) == 0: continue
            
            # SELECT SHEET
            if direction not in wb.sheetnames:
                print(f'  Sheet \'{direction}\' missing. Attempting to copy \'0\'...')
                if '0' in wb.sheetnames:
                    ws_source = wb['0']
                    ws = wb.copy_worksheet(ws_source)
                    ws.title = direction
                else:
                    print('  Error: Sheet \'0\' missing. Cannot create sheet.')
                    continue
            else:
                ws = wb[direction]
            
            # PROCESS SHEET DATA
            headers = []
            for cell in ws[1]: headers.append(cell.value)
            
            try:
                x_idx = headers.index('X_coords')
                y_idx = headers.index('Y_coords')
                sdf_idx = headers.index('SDF')
            except ValueError:
                print('  Error: Missing required columns (X_coords, Y_coords, SDF).')
                continue

            if 'mag_U' in headers:
                mag_u_idx = headers.index('mag_U')
            else:
                mag_u_idx = len(headers)
                ws.cell(row=1, column=mag_u_idx+1, value='mag_U')

            # Read Rows
            rows = list(ws.iter_rows(min_row=2, values_only=True))
            limit = min(len(rows), len(magnitudes))
            
            new_data = [] 
            mismatches = 0
            
            for i in range(limit):
                row_vals = list(rows[i])
                
                # Check NaNs based on SDF
                mag = magnitudes[i]
                sdf = float(row_vals[sdf_idx]) if row_vals[sdf_idx] is not None else 0
                
                if math.isnan(mag):
                    if sdf < 10: mag = 0.0
                    else: continue # Skip row (Remove)
                
                # Update mag_U
                while len(row_vals) <= mag_u_idx: row_vals.append(None)
                row_vals[mag_u_idx] = mag
                new_data.append(row_vals)
                
            # Write Back
            ws.delete_rows(2, amount=ws.max_row)
            for r_idx, row_data in enumerate(new_data):
                for c_idx, val in enumerate(row_data):
                    ws.cell(row=r_idx+2, column=c_idx+1, value=val)
                    
            print(f'  -> Updated {len(new_data)} rows.')
            changes_made = True
            
        except Exception as e:
            print(f'  Error processing {direction}: {e}')

    # ---------------------------------------------------------
    # 3. SAVE WORKBOOK (ONCE)
    # ---------------------------------------------------------
    if changes_made:
        print('\nSaving Workbook...')
        wb.save(result_path)
        print(f'Saved to: {result_path}')
    else:
        print('\nNo changes made.')

if __name__ == '__main__':
    main()
