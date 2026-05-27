#!/usr/bin/env python3
import os
import sys
import shutil
import argparse

def get_case_variants(word):
    """
    Given a camelCase or PascalCase signature like 'NeonVM', 
    return variants to replace:
    - 'NeonVM'
    - 'neonVM'
    - 'Neon'
    - 'neon'
    """
    if "VM" in word:
        base = word.replace("VM", "")
    else:
        base = word
        
    variants = {
        word: word,                                        # NeonVM
        word[0].lower() + word[1:]: word[0].lower() + word[1:], # neonVM
        base: base,                                        # Neon
        base.lower(): base.lower()                         # neon
    }
    return variants

def process_file_content(filepath, old_sig, new_sig):
    old_variants = get_case_variants(old_sig)
    new_variants = get_case_variants(new_sig)
    
    with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
        
    original_content = content
    
    # Replace from most specific (longest) to least specific (shortest)
    # e.g., NeonVM -> DarksVM, then Neon -> Darks
    replace_pairs = [
        (old_variants[old_sig], new_variants[new_sig]),
        (old_variants[old_sig[0].lower() + old_sig[1:]], new_variants[new_sig[0].lower() + new_sig[1:]]),
    ]
    
    if "VM" in old_sig and "VM" in new_sig:
        old_base = old_sig.replace("VM", "")
        new_base = new_sig.replace("VM", "")
        replace_pairs.append((old_base, new_base))
        replace_pairs.append((old_base.lower(), new_base.lower()))

    for old_str, new_str in replace_pairs:
        content = content.replace(old_str, new_str)
        
    if content != original_content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        return True
    return False

def rename_paths(target_dirs, old_sig, new_sig):
    old_variants = get_case_variants(old_sig)
    new_variants = get_case_variants(new_sig)
    
    replace_pairs = [
        (old_variants[old_sig], new_variants[new_sig]),
        (old_variants[old_sig[0].lower() + old_sig[1:]], new_variants[new_sig[0].lower() + new_sig[1:]])
    ]
    
    if "VM" in old_sig and "VM" in new_sig:
        old_base = old_sig.replace("VM", "")
        new_base = new_sig.replace("VM", "")
        replace_pairs.append((old_base, new_base))
        replace_pairs.append((old_base.lower(), new_base.lower()))

    renamed_count = 0
    for d in target_dirs:
        if not os.path.exists(d):
            continue
            
        for root, dirs, files in os.walk(d, topdown=False):
            # Rename files
            for name in files:
                new_name = name
                for old_str, new_str in replace_pairs:
                    new_name = new_name.replace(old_str, new_str)
                
                if new_name != name:
                    old_path = os.path.join(root, name)
                    new_path = os.path.join(root, new_name)
                    os.rename(old_path, new_path)
                    renamed_count += 1
                    
            # Rename directories
            for name in dirs:
                new_name = name
                for old_str, new_str in replace_pairs:
                    new_name = new_name.replace(old_str, new_str)
                
                if new_name != name:
                    old_path = os.path.join(root, name)
                    new_path = os.path.join(root, new_name)
                    os.rename(old_path, new_path)
                    renamed_count += 1
                    
    return renamed_count

def main():
    parser = argparse.ArgumentParser(description="Change VM signature across the entire project.")
    parser.add_argument("old_sig", help="Current signature (e.g., NeonVM)")
    parser.add_argument("new_sig", help="New signature (e.g., GhostVM)")
    args = parser.parse_args()

    target_dirs = ["KoiVM", "KoiVM.Runtime", "KoiVM.Confuser", "tests", "docs", "ConfuserEx-Plus"]
    extensions = [".cs", ".csproj", ".md", ".sh", ".bat", ".crproj", ".cpp"]

    print(f"[*] Changing signature from '{args.old_sig}' to '{args.new_sig}'...")

    # 1. Replace in files
    replaced_files = 0
    for d in target_dirs:
        if not os.path.exists(d):
            continue
        for root, dirs, files in os.walk(d):
            for f in files:
                if any(f.endswith(ext) for ext in extensions):
                    filepath = os.path.join(root, f)
                    if process_file_content(filepath, args.old_sig, args.new_sig):
                        replaced_files += 1

    print(f"[+] Replaced content in {replaced_files} files.")

    # 2. Rename files and directories
    renamed_count = rename_paths(target_dirs, args.old_sig, args.new_sig)
    print(f"[+] Renamed {renamed_count} files/directories.")
    
    print("[*] Done. Remember to rebuild the project to apply changes!")

if __name__ == "__main__":
    main()
