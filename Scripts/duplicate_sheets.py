import openpyxl
import sys
import os
import math

def duplicate_sheets(file_path):
    if not os.path.exists(file_path):
        print(f"Error: File not found: {file_path}")
        return

    try:
        print(f"Loading workbook (openpyxl): {file_path}")
        # Load workbook
        wb = openpyxl.load_workbook(file_path)
    except Exception as e:
        print(f"Error reading Excel file: {e}")
        return

    if '0' not in wb.sheetnames:
        print("Error: Sheet '0' not found. Cannot duplicate.")
        return

    ws_0 = wb['0']
    
    # 1. Identify Column Indices for dir_sin and dir_cos
    # Assuming headers are in row 1
    header_row = next(ws_0.iter_rows(min_row=1, max_row=1, values_only=True))
    
    sin_idx = -1
    cos_idx = -1
    
    for i, h in enumerate(header_row):
        if h == 'dir_sin': sin_idx = i
        if h == 'dir_cos': cos_idx = i
        
    if sin_idx == -1 or cos_idx == -1:
        print("Warning: Could not find 'dir_sin' or 'dir_cos' columns. Updates will be skipped.")
    else:
        print(f"Found direction columns at indices: {sin_idx}, {cos_idx}")

    target_directions = ['45', '90', '135', '180', '225', '270', '315']
    count = 0
    
    for direction in target_directions:
        ws_target = None
        
        if direction not in wb.sheetnames:
            print(f"Creating sheet '{direction}' from '0'...")
            # Copy Sheet
            ws_target = wb.copy_worksheet(ws_0)
            ws_target.title = direction
            count += 1
        else:
            print(f"Sheet '{direction}' exists. Updating direction columns...")
            ws_target = wb[direction]
            
        # Update Direction Columns (Always enforce correctness)
        if ws_target and sin_idx != -1 and cos_idx != -1:
            try:
                w_dir_val = float(direction)
                ang_rad = w_dir_val * math.pi / 180.0
                
                # Logic: -sin, -cos
                d_sin = -math.sin(ang_rad)
                d_cos = -math.cos(ang_rad)
                
                # Clamp
                d_sin = max(-1.0, min(1.0, d_sin))
                d_cos = max(-1.0, min(1.0, d_cos))
                
                # Round to 3 decimals
                d_sin = round(d_sin, 3)
                d_cos = round(d_cos, 3)
                
                # Handle clean 0, 1, -1 for aesthetics
                if d_sin == 0.0: d_sin = 0
                if d_cos == 0.0: d_cos = 0
                if d_sin == 1.0: d_sin = 1
                if d_cos == 1.0: d_cos = 1
                if d_sin == -1.0: d_sin = -1
                if d_cos == -1.0: d_cos = -1
                
                # Iterate rows starting from row 2
                for row in ws_target.iter_rows(min_row=2):
                    row[sin_idx].value = d_sin
                    row[cos_idx].value = d_cos
                    
                print(f"  -> Updated direction columns for {direction} deg.")
                
            except ValueError:
                print(f"Warning: Could not parse direction {direction}")

    if count == 0:
        print("All target sheets already exist.")
        return

    try:
        print(f"Saving updates to {file_path}...")
        wb.save(file_path)
        print("Successfully updated file.")
    except Exception as e:
        print(f"Error saving file: {e}")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python duplicate_sheets.py <path_to_excel_file>")
    else:
        duplicate_sheets(sys.argv[1])
