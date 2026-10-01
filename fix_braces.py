import os
import re

def fix_braces(file_path):
    with open(file_path, 'r') as f:
        content = f.read()

    # Find single-line if statements without braces and add braces
    # This regex is a bit naive but should catch most `if (cond) return/throw...`
    pattern = re.compile(r'^[ \t]*if\s*\((.*?)\)\s*\n[ \t]+(return|throw|continue|break)[^\n]+;$', re.MULTILINE)
    
    def repl(m):
        full_match = m.group(0)
        lines = full_match.split('\n')
        if_line = lines[0]
        body_line = lines[1]
        
        indent = len(if_line) - len(if_line.lstrip())
        indent_str = ' ' * indent
        
        return f"{if_line}\n{indent_str}{{\n{body_line}\n{indent_str}}}"

    # We might need to run it multiple times if there are nested ones, but unlikely here.
    new_content = pattern.sub(repl, content)
    
    with open(file_path, 'w') as f:
        f.write(new_content)

fix_braces('Jellyfin.Plugin.SendToKindle/Api/SendToKindleController.cs')
fix_braces('Jellyfin.Plugin.SendToKindle/Services/KindleExtractionService.cs')
