import os
import glob
import re

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Match namespace BlockScope\n{ ... } at the end
    pattern = re.compile(r'namespace\s+([A-Za-z0-9_.]+)\s*\{', re.MULTILINE)
    
    match = pattern.search(content)
    if not match:
        return
    
    namespace_name = match.group(1)
    
    # Find where the namespace block starts
    start_idx = match.end()
    
    # Find the corresponding closing brace for the namespace
    # We will just assume the last closing brace in the file belongs to the namespace
    last_brace_idx = content.rfind('}')
    
    if last_brace_idx != -1:
        # Reconstruct the file:
        # Before the match
        # namespace namespace_name;
        # inner content, de-indented by 4 spaces
        # after last brace
        
        prefix = content[:match.start()]
        
        inner = content[start_idx:last_brace_idx]
        suffix = content[last_brace_idx+1:]
        
        # De-indent inner
        lines = inner.split('\n')
        deindented_lines = []
        for line in lines:
            if line.startswith('    '):
                deindented_lines.append(line[4:])
            else:
                deindented_lines.append(line)
        
        new_content = prefix + f'namespace {namespace_name};\n' + '\n'.join(deindented_lines) + suffix
        
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(new_content)

for root, _, files in os.walk('Jellyfin.Plugin.SendToKindle'):
    for f in files:
        if f.endswith('.cs'):
            process_file(os.path.join(root, f))
