from html.parser import HTMLParser
import sys

class TagChecker(HTMLParser):
    def __init__(self):
        super().__init__()
        self.stack = []
        self.void_elements = {'area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'param', 'source', 'track', 'wbr'}

    def handle_starttag(self, tag, attrs):
        if tag not in self.void_elements:
            self.stack.append(tag)

    def handle_endtag(self, tag):
        if not self.stack:
            print(f"Error: closing tag </{tag}> with no open tags.")
            return
        if self.stack[-1] == tag:
            self.stack.pop()
        else:
            print(f"Error: Mismatched tag. Expected </{self.stack[-1]}>, got </{tag}>.")
            self.stack.pop()

with open(sys.argv[1], 'r', encoding='utf-8') as f:
    content = f.read()

# simple strip of @code block and @if
import re
content = re.sub(r'@code\s*\{.*\}', '', content, flags=re.DOTALL)
content = re.sub(r'@[a-zA-Z0-9_\.]+', '', content)
content = re.sub(r'@\(.*?\)', '', content)
content = re.sub(r'@\{.*?\}', '', content, flags=re.DOTALL)
content = re.sub(r'@if\s*\(.*?\)\s*\{', '<div>', content)
content = re.sub(r'@foreach\s*\(.*?\)\s*\{', '<div>', content)
content = re.sub(r'@else\s*\{', '<div>', content)
content = re.sub(r'\}', '</div>', content)

checker = TagChecker()
checker.feed(content)
if checker.stack:
    print("Unclosed tags:", checker.stack)
else:
    print("All tags match.")
