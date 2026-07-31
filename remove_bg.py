import sys
from PIL import Image

def remove_background(image_path, out_path, tolerance=40):
    img = Image.open(image_path).convert("RGBA")
    data = img.load()
    width, height = img.size
    
    # We assume the top-left pixel is the background color
    bg_color = data[0, 0]
    
    # Flood fill from all 4 corners
    visited = set()
    queue = [(0, 0), (width-1, 0), (0, height-1), (width-1, height-1)]
    
    while queue:
        x, y = queue.pop(0)
        if (x, y) in visited:
            continue
        visited.add((x, y))
        
        r, g, b, a = data[x, y]
        if abs(r - bg_color[0]) <= tolerance and abs(g - bg_color[1]) <= tolerance and abs(b - bg_color[2]) <= tolerance:
            data[x, y] = (255, 255, 255, 0)
            
            if x > 0: queue.append((x-1, y))
            if x < width-1: queue.append((x+1, y))
            if y > 0: queue.append((x, y-1))
            if y < height-1: queue.append((x, y+1))
            
    img.save(out_path, "PNG")

if __name__ == "__main__":
    remove_background(sys.argv[1], sys.argv[2])
