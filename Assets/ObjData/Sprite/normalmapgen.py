from PIL import Image
import numpy as np
import cv2
import sys
import os

# Check if an image path is provided via drag-and-drop or command-line argument
if len(sys.argv) > 1:
    image_path = sys.argv[1]
else:
    raise ValueError("No image file provided. Drag and drop an image onto the script.")    pip install pillow numpy opencv-python

# Validate the file path
if not os.path.isfile(image_path):
    raise FileNotFoundError(f"The file '{image_path}' does not exist.")

# Load the image
image = Image.open(image_path).convert("L")  # Convert to grayscale

# Convert to NumPy array
gray = np.array(image)

# Generate normal map using Sobel filter
sobelx = cv2.Sobel(gray, cv2.CV_32F, 1, 0, ksize=3)
sobely = cv2.Sobel(gray, cv2.CV_32F, 0, 1, ksize=3)

# Calculate the normal map
height, width = gray.shape
normal_map = np.zeros((height, width, 3), dtype=np.float32)
normal_map[..., 0] = sobelx / 255.0  # X
normal_map[..., 1] = sobely / 255.0  # Y
normal_map[..., 2] = 1.0  # Z
norm = np.linalg.norm(normal_map, axis=2)
normal_map /= norm[..., np.newaxis]
normal_map = (normal_map + 1.0) * 0.5  # Normalize to [0, 1] for image
normal_map_img = (normal_map * 255).astype(np.uint8)

# Save the normal map image
normal_map_path = os.path.splitext(image_path)[0] + "_normal_map.png"
Image.fromarray(normal_map_img).save(normal_map_path)

print(f"Normal map saved to: {normal_map_path}")

